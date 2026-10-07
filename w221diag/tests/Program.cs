using System.Buffers.Binary;
using W221Diag;

var tests = new (string Name, Action Run)[]
{
    ("TCP endpoints and target filter", () => {
        var flows = Analyze(Join(Section(), Interface(1), Packet(Tcp())), "192.168.0.10");
        Check(flows.Count == 1 && flows[0].SourcePort == 50000 && flows[0].DestinationPort == 1234 && flows[0].Packets == 1, "Wrong TCP endpoints");
        Check(Analyze(Join(Section(), Interface(1), Packet(Tcp())), "192.168.0.99").Count == 0, "Target filter leaked unrelated traffic");
    }),
    ("UDP endpoints", () => {
        var frame = Tcp(); frame[23] = 17; frame[38] = 0; frame[39] = 20;
        var flows = Analyze(Join(Section(), Interface(1), Packet(frame)));
        Check(flows.Count == 1 && flows[0].Protocol == "UDP" && flows[0].DestinationPort == 1234, "UDP ports lost");
    }),
    ("Big endian section", () => {
        var blocks = new[] {Section(), Interface(1), Packet(Tcp())};
        foreach (var block in blocks)
        {
            foreach (int offset in new[] {0,4,block.Length - 4}) Array.Reverse(block,offset,4);
        }
        Array.Reverse(blocks[0],8,4); Array.Reverse(blocks[0],12,2); Array.Reverse(blocks[0],14,2);
        Array.Reverse(blocks[1],8,2); Array.Reverse(blocks[1],12,4);
        for (int offset=8;offset<=24;offset+=4) Array.Reverse(blocks[2],offset,4);
        var flows = Analyze(Join(blocks));
        Check(flows.Count == 1 && flows[0].SourcePort == 50000, "Big endian capture not decoded");
    }),
    ("Interface IDs restart in each section", () => {
        var flows = Analyze(Join(Section(), Interface(101), Section(), Interface(1), Packet(Tcp())));
        Check(flows.Count == 1, "Second section reused first section link type");
    }),
    ("Non-initial fragments are not TCP ports", () => {
        var frame = Tcp(); frame[20] = 0; frame[21] = 1;
        var flows = Analyze(Join(Section(), Interface(1), Packet(frame)));
        Check(flows.Count == 1 && flows[0].SourcePort == 0 && flows[0].DestinationPort == 0, "Fragment payload interpreted as TCP ports");
    }),
    ("Truncated blocks are rejected", () => {
        ExpectInvalid(Join(Section(), Interface(1), Packet(Tcp()))[..^1]);
    }),
    ("Mismatched block trailer is rejected", () => {
        var data = Join(Section(), Interface(1), Packet(Tcp())); data[^4] = 0;
        ExpectInvalid(data);
    }),
    ("Unknown interfaces do not become Ethernet", () => {
        Check(Analyze(Join(Section(), Packet(Tcp()))).Count == 0, "Unknown interface parsed as Ethernet");
    }),
    ("Invalid IP filters are rejected", () => {
        try { Analyze(Section(), "not-an-ip"); throw new Exception("Invalid filter accepted"); }
        catch (ArgumentException) { }
    })
};
int failed = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception ex) { failed++; Console.WriteLine($"FAIL {test.Name}: {ex.Message}"); }
}
return failed == 0 ? 0 : 1;

static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
static void ExpectInvalid(byte[] data)
{
    try { Analyze(data); throw new Exception("Damaged capture accepted"); }
    catch (InvalidDataException) { }
}
static IReadOnlyList<CaptureAnalyzer.FlowSummary> Analyze(byte[] data, string? target = null)
{
    string path = Path.GetTempFileName();
    try { File.WriteAllBytes(path, data); return CaptureAnalyzer.Analyze(path, target); }
    finally { File.Delete(path); }
}
static byte[] Join(params byte[][] parts) => parts.SelectMany(p => p).ToArray();
static byte[] Block(uint type, byte[] body)
{
    var bytes = new byte[12 + body.Length];
    BinaryPrimitives.WriteUInt32LittleEndian(bytes, type);
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), (uint)bytes.Length);
    body.CopyTo(bytes, 8);
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(bytes.Length - 4), (uint)bytes.Length);
    return bytes;
}
static byte[] Section() => Block(0x0A0D0D0A, new byte[] {0x4D,0x3C,0x2B,0x1A,1,0,0,0,255,255,255,255,255,255,255,255});
static byte[] Interface(ushort type)
{
    var body = new byte[8]; BinaryPrimitives.WriteUInt16LittleEndian(body, type);
    BinaryPrimitives.WriteUInt32LittleEndian(body.AsSpan(4), 65535); return Block(1, body);
}
static byte[] Packet(byte[] frame)
{
    var body = new byte[20 + ((frame.Length + 3) & ~3)];
    BinaryPrimitives.WriteUInt32LittleEndian(body.AsSpan(12), (uint)frame.Length);
    BinaryPrimitives.WriteUInt32LittleEndian(body.AsSpan(16), (uint)frame.Length);
    frame.CopyTo(body, 20); return Block(6, body);
}
static byte[] Tcp()
{
    var frame = new byte[54]; frame[12]=8; frame[14]=0x45; frame[17]=40; frame[23]=6;
    new byte[]{192,168,0,2}.CopyTo(frame,26); new byte[]{192,168,0,10}.CopyTo(frame,30);
    frame[34]=0xC3; frame[35]=0x50; frame[36]=4; frame[37]=0xD2; frame[46]=0x50;
    return frame;
}
