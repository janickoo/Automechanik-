using System.Buffers.Binary;
using System.Net;

namespace W221Diag;

public static class CaptureAnalyzer
{
    public sealed record FlowSummary(
        string Protocol,
        string Source,
        int SourcePort,
        string Destination,
        int DestinationPort,
        int Packets,
        long Bytes)
    {
        public string SourceEndpoint => SourcePort > 0 ? $"{Source}:{SourcePort}" : Source;
        public string DestinationEndpoint => DestinationPort > 0 ? $"{Destination}:{DestinationPort}" : Destination;
    }

    private sealed class MutableFlow
    {
        public required string Protocol { get; init; }
        public required string Source { get; init; }
        public int SourcePort { get; init; }
        public required string Destination { get; init; }
        public int DestinationPort { get; init; }
        public int Packets { get; set; }
        public long Bytes { get; set; }
    }

    public static IReadOnlyList<FlowSummary> Analyze(string path, string? targetIp = null)
    {
        if (!string.IsNullOrWhiteSpace(targetIp))
        {
            if (!IPAddress.TryParse(targetIp.Trim(), out var address) ||
                address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                throw new ArgumentException("Filter musí byť platná IPv4 adresa.", nameof(targetIp));
            targetIp = address.ToString();
        }
        byte[] data = File.ReadAllBytes(path);
        if (data.Length < 28 || BinaryPrimitives.ReadUInt32LittleEndian(data) != 0x0A0D0D0A)
            throw new InvalidDataException("Súbor nie je platný PCAPNG záznam.");
        var flows = new Dictionary<string, MutableFlow>(StringComparer.Ordinal);
        var linkTypes = new Dictionary<uint, ushort>();

        int offset = 0;
        bool littleEndian = true;
        uint interfaceIndex = 0;

        while (offset < data.Length)
        {
            if (data.Length - offset < 12)
                throw new InvalidDataException("Neúplná hlavička PCAPNG bloku.");
            uint type = ReadUInt32(data, offset, littleEndian);

            // Section Header Block has a byte-order magic at offset + 8.
            if (type == 0x0A0D0D0A && offset + 12 <= data.Length)
            {
                uint bomAsLittle = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 8, 4));
                if (bomAsLittle == 0x1A2B3C4D)
                    littleEndian = true;
                else if (bomAsLittle == 0x4D3C2B1A)
                    littleEndian = false;
                else
                    throw new InvalidDataException("Neplatné poradie bajtov PCAPNG sekcie.");
                linkTypes.Clear();
                interfaceIndex = 0;
            }

            uint blockLength = ReadUInt32(data, offset + 4, littleEndian);
            if (blockLength < 12 || blockLength > int.MaxValue || blockLength > data.Length - offset || blockLength % 4 != 0)
                throw new InvalidDataException("Neúplný alebo neplatný PCAPNG blok.");
            if (ReadUInt32(data, offset + (int)blockLength - 4, littleEndian) != blockLength)
                throw new InvalidDataException("Dĺžka PCAPNG bloku nesúhlasí s koncovou hodnotou.");
            if (type == 0x0A0D0D0A && blockLength < 28 ||
                type == 1 && blockLength < 20 || type == 6 && blockLength < 32)
                throw new InvalidDataException("PCAPNG blok je kratší ako povinná hlavička.");

            if (type == 0x00000001 && blockLength >= 20) // Interface Description Block
            {
                ushort linkType = ReadUInt16(data, offset + 8, littleEndian);
                linkTypes[interfaceIndex++] = linkType;
            }
            else if (type == 0x00000006 && blockLength >= 32) // Enhanced Packet Block
            {
                uint capturedLength = ReadUInt32(data, offset + 20, littleEndian);
                uint iface = ReadUInt32(data, offset + 8, littleEndian);
                int packetOffset = offset + 28;

                if (capturedLength > blockLength - 32)
                    throw new InvalidDataException("Dĺžka paketu presahuje PCAPNG blok.");
                if (linkTypes.TryGetValue(iface, out var linkType))
                    AnalyzePacket(data.AsSpan(packetOffset, (int)capturedLength), linkType, targetIp, flows);
            }

            offset += (int)blockLength;
        }

        return flows.Values
            .OrderByDescending(f => f.Packets)
            .ThenByDescending(f => f.Bytes)
            .Select(f => new FlowSummary(f.Protocol, f.Source, f.SourcePort, f.Destination, f.DestinationPort, f.Packets, f.Bytes))
            .ToList();
    }

    private static void AnalyzePacket(
        ReadOnlySpan<byte> packet,
        ushort linkType,
        string? targetIp,
        Dictionary<string, MutableFlow> flows)
    {
        // Windows pktmon PCAPNG normally exports Ethernet frames (DLT_EN10MB = 1).
        if (linkType != 1 || packet.Length < 14)
            return;

        ushort etherType = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(12, 2));
        int ipOffset = 14;

        // One 802.1Q VLAN tag.
        if (etherType == 0x8100 && packet.Length >= 18)
        {
            etherType = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(16, 2));
            ipOffset = 18;
        }

        if (etherType != 0x0800 || packet.Length < ipOffset + 20)
            return;

        int version = packet[ipOffset] >> 4;
        int ihl = (packet[ipOffset] & 0x0F) * 4;
        if (version != 4 || ihl < 20 || packet.Length < ipOffset + ihl)
            return;

        int totalLength = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(ipOffset + 2, 2));
        if (totalLength < ihl)
            return;
        packet = packet[..Math.Min(packet.Length, ipOffset + totalLength)];
        bool nonInitialFragment = (BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(ipOffset + 6, 2)) & 0x1FFF) != 0;
        byte protocol = packet[ipOffset + 9];
        string source = new IPAddress(packet.Slice(ipOffset + 12, 4)).ToString();
        string destination = new IPAddress(packet.Slice(ipOffset + 16, 4)).ToString();

        if (!string.IsNullOrWhiteSpace(targetIp) &&
            !source.Equals(targetIp, StringComparison.OrdinalIgnoreCase) &&
            !destination.Equals(targetIp, StringComparison.OrdinalIgnoreCase))
            return;

        int transportOffset = ipOffset + ihl;
        int sourcePort = 0;
        int destinationPort = 0;
        string protocolName;

        if (!nonInitialFragment && protocol == 6 && packet.Length >= transportOffset + 20)
        {
            protocolName = "TCP";
            sourcePort = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(transportOffset, 2));
            destinationPort = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(transportOffset + 2, 2));
        }
        else if (!nonInitialFragment && protocol == 17 && packet.Length >= transportOffset + 8)
        {
            protocolName = "UDP";
            sourcePort = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(transportOffset, 2));
            destinationPort = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(transportOffset + 2, 2));
        }
        else
        {
            protocolName = $"IP/{protocol}";
        }

        string key = $"{protocolName}|{source}|{sourcePort}|{destination}|{destinationPort}";
        if (!flows.TryGetValue(key, out var flow))
        {
            flow = new MutableFlow
            {
                Protocol = protocolName,
                Source = source,
                SourcePort = sourcePort,
                Destination = destination,
                DestinationPort = destinationPort
            };
            flows.Add(key, flow);
        }

        flow.Packets++;
        flow.Bytes += packet.Length;
    }

    private static uint ReadUInt32(byte[] data, int offset, bool littleEndian) =>
        littleEndian
            ? BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4))
            : BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));

    private static ushort ReadUInt16(byte[] data, int offset, bool littleEndian) =>
        littleEndian
            ? BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2))
            : BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, 2));
}
