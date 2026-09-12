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

    public sealed record SessionFileInfo(
        string FileName,
        long Size,
        string Format,
        bool LooksEncrypted,
        string Sha256,
        string Notes);

    public sealed record SessionInspection(
        string Folder,
        VehicleMetadata? Vehicle,
        IReadOnlyList<SessionFileInfo> Files);

    public static SessionInspection InspectFolder(string folder)
    {
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException(folder);

        VehicleMetadata? vehicle = null;
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
                notes = "TXEF header detected. Payload is high-entropy/proprietary and is treated as opaque until the TEXA format/key derivation is known.";
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

        return new SessionInspection(folder, vehicle, files);
    }

    public static VehicleMetadata ParseDataXml(byte[] bytes)
    {
        // IDC6 data.xml observed in backups is UTF-16 with BOM.
        string text;
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            text = Encoding.Unicode.GetString(bytes);
        else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            text = Encoding.BigEndianUnicode.GetString(bytes);
        else
            text = Encoding.UTF8.GetString(bytes);

        text = text.TrimStart('\uFEFF', '\u0000');
        var document = XDocument.Parse(text);
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
