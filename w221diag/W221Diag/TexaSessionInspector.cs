using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace W221Diag;

public static class TexaSessionInspector
{
    public sealed record VehicleMetadata(
        string? ProductVersion,
        string? Brand,
        string? Model,
        string? Engine,
        string? EngineCode,
        string? Vin);

    public sealed record DtcEntry(
        string Code,
        string? Status,
        string? Detail);

    public sealed record EcuScanResult(
        string ShortName,
        int? Index,
        string? DataSetId,
        string? Cp5,
        string? ReadState,
        string? ErrorState,
        string? Variant,
        IReadOnlyList<DtcEntry> Dtcs);

    public sealed record SessionFileInfo(
        string FileName,
        long Size,
        string Format,
        bool LooksEncrypted,
        string Sha256,
        string Notes);

    public sealed record SessionInspection(
        string Folder,
        string? SessionName,
        VehicleMetadata? Vehicle,
        IReadOnlyList<EcuScanResult> EcuResults,
        IReadOnlyList<SessionFileInfo> Files)
    {
        public int DtcCount => EcuResults.Sum(x => x.Dtcs.Count);
    }

    public static SessionInspection InspectFolder(string folder)
    {
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException(folder);

        VehicleMetadata? vehicle = null;
        string? sessionName = null;
        var ecuResults = new List<EcuScanResult>();
        var files = new List<SessionFileInfo>();

        foreach (var path in Directory.EnumerateFiles(folder).OrderBy(Path.GetFileName))
        {
            var bytes = File.ReadAllBytes(path);
            string name = Path.GetFileName(path);
            string format = "Unknown";
            bool encrypted = false;
            string notes = string.Empty;

            if (name.Equals("data.xml", StringComparison.OrdinalIgnoreCase))
            {
                format = "TEXA session metadata XML";
                try
                {
                    vehicle = ParseDataXml(bytes);
                    notes = "Vehicle/session metadata parsed.";
                }
                catch (Exception ex)
                {
                    notes = "XML present but could not be parsed: " + ex.Message;
                }
            }
            else if (name.Equals("rgeFB.xml", StringComparison.OrdinalIgnoreCase))
            {
                format = "TEXA ECU scan result XML";
                try
                {
                    ecuResults.Clear();
                    ecuResults.AddRange(ParseRgeFbXml(bytes));
                    notes = $"Parsed {ecuResults.Count} ECU entries and {ecuResults.Sum(x => x.Dtcs.Count)} DTC entries.";
                }
                catch (Exception ex)
                {
                    notes = "ECU result XML present but could not be parsed: " + ex.Message;
                }
            }
            else if (name.Equals("autodia.ini", StringComparison.OrdinalIgnoreCase))
            {
                format = "TEXA AutoDia session configuration";
                try
                {
                    sessionName = ParseSessionName(bytes);
                    notes = sessionName is null ? "Configuration parsed." : $"Session name: {sessionName}.";
                }
                catch (Exception ex)
                {
                    notes = "Configuration present but could not be parsed: " + ex.Message;
                }
            }
            else if (StartsWithAscii(bytes, "Salted__"))
            {
                format = "OpenSSL salted encrypted container";
                encrypted = true;
                notes = "OpenSSL-compatible Salted__ header detected; password/key is not stored in the file header.";
            }
            else if (StartsWithAscii(bytes, "TXEF"))
            {
                format = "TEXA TXEF container";
                encrypted = true;
                notes = "TXEF header detected. Payload is proprietary/high-entropy and is kept opaque until its encoding is understood.";
            }
            else if (StartsWithAscii(bytes, "#### BIN VERSION"))
            {
                format = "TEXA diagnostic BIN";
                encrypted = true;
                notes = ParseBinHeader(bytes) ?? "TEXA BIN header detected; payload appears encoded/encrypted.";
            }
            else if (Path.GetExtension(name).Equals(".pxml", StringComparison.OrdinalIgnoreCase) ||
                     Path.GetExtension(name).Equals(".xml", StringComparison.OrdinalIgnoreCase))
            {
                format = "XML-like session file";
            }
            else if (Path.GetExtension(name).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                format = "JSON session file";
            }

            files.Add(new SessionFileInfo(
                name,
                bytes.LongLength,
                format,
                encrypted,
                Convert.ToHexString(SHA256.HashData(bytes)),
                notes));
        }

        return new SessionInspection(folder, sessionName, vehicle, ecuResults, files);
    }

    public static VehicleMetadata ParseDataXml(byte[] bytes)
    {
        var document = XDocument.Parse(DecodeText(bytes));
        var root = document.Root;
        var vehicle = root?.Element("vehicles");

        return new VehicleMetadata(
            root?.Attribute("product-version")?.Value,
            vehicle?.Attribute("brand")?.Value,
            vehicle?.Attribute("model")?.Value,
            vehicle?.Attribute("motor")?.Value,
            vehicle?.Attribute("motorcode")?.Value,
            vehicle?.Attribute("vin")?.Value);
    }

    public static IReadOnlyList<EcuScanResult> ParseRgeFbXml(byte[] bytes)
    {
        var document = XDocument.Parse(DecodeText(bytes));
        var results = new List<EcuScanResult>();

        foreach (var rw in document.Root?.Elements("RW") ?? Enumerable.Empty<XElement>())
        {
            var pn = rw.Element("PN");
            if (pn is null)
                continue;

            string[] parts = pn.Value.Split(';');
            string shortName = parts.ElementAtOrDefault(0) ?? string.Empty;
            int? index = int.TryParse(parts.ElementAtOrDefault(1), out int parsedIndex) ? parsedIndex : null;
            string? dataSetId = parts.ElementAtOrDefault(2);
            string? cp5 = pn.Attribute("cp5")?.Value;
            string? rd = rw.Element("RD")?.Value;
            string? ea = rw.Element("EA")?.Value;
            var rcs = rw.Element("RCS");
            string? variant = rcs?.Attribute("VA")?.Value;

            var dtcs = (rcs?.Elements("RC") ?? Enumerable.Empty<XElement>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => new DtcEntry(
                    x.Value.Trim(),
                    x.Attribute("ST")?.Value,
                    x.Attribute("D")?.Value))
                .ToList();

            results.Add(new EcuScanResult(
                shortName,
                index,
                dataSetId,
                cp5,
                rd,
                ea,
                variant,
                dtcs));
        }

        return results;
    }

    public static string? ParseSessionName(byte[] bytes)
    {
        string text = DecodeText(bytes);
        foreach (string rawLine in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            string line = rawLine.Trim();
            if (line.StartsWith("ds.name=", StringComparison.OrdinalIgnoreCase))
                return line["ds.name=".Length..].Trim();
        }
        return null;
    }

    private static string DecodeText(byte[] bytes)
    {
        string text;
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            text = Encoding.Unicode.GetString(bytes);
        else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            text = Encoding.BigEndianUnicode.GetString(bytes);
        else
            text = Encoding.UTF8.GetString(bytes);

        return text.TrimStart('\uFEFF', '\u0000');
    }

    private static string? ParseBinHeader(byte[] bytes)
    {
        int length = Math.Min(bytes.Length, 160);
        string header = Encoding.ASCII.GetString(bytes, 0, length);
        var match = Regex.Match(
            header,
            @"#### BIN VERSION \[(?<version>[^\]]+)\]; Date: (?<date>[^ ]+) Time:(?<time>[^ ]+) ####",
            RegexOptions.CultureInvariant);

        if (!match.Success)
            return null;

        return $"BIN version {match.Groups["version"].Value}; date {match.Groups["date"].Value}; time {match.Groups["time"].Value}. Payload after the header is treated as opaque.";
    }

    private static bool StartsWithAscii(byte[] bytes, string value)
    {
        byte[] prefix = Encoding.ASCII.GetBytes(value);
        return bytes.Length >= prefix.Length && bytes.AsSpan(0, prefix.Length).SequenceEqual(prefix);
    }
}
