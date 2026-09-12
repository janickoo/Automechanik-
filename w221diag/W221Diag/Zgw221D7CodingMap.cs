namespace W221Diag;

/// <summary>
/// Read-only recovery knowledge for the ZGW221 D7 variant (Diagnostic-ID 000D).
///
/// This file deliberately separates values confirmed by historical diagnostics from
/// strong inferences and unresolved fields.  It is NOT a ready-to-write coding string.
/// Unknown bits must be preserved/read from the ECU when possible, or resolved from
/// authoritative vehicle data before any write operation is enabled.
/// </summary>
public static class Zgw221D7CodingMap
{
    public const ushort ExpectedDiagnosticId = 0x000D;
    public const string VariantName = "D7_Muster";
    public const int GlobalCodingLength = 24;
    public const int InternalCodingLength = 1;

    // Recovery file identity established offline from the matching CBF/CFF set.
    public const string ExpectedCbfName = "zgw221.cbf";
    public const string ExpectedCffName = "2214481227_001.CFF";
    public const string ExpectedSoftwarePartNumber = "A2214481227";
    public const string ExpectedSoftwareVersion = "07.34.01";

    public enum EvidenceLevel
    {
        Confirmed,
        StrongInference,
        Unresolved
    }

    public sealed record CodingField(
        string Qualifier,
        int BitOffset,
        int BitLength,
        ulong? Value,
        EvidenceLevel Evidence,
        string Note);

    /// <summary>
    /// Fields parsed from VCD_Globale_Variantencodierung for D7_Muster.
    /// Values below describe the target vehicle only where evidence is available.
    /// </summary>
    public static IReadOnlyList<CodingField> TargetFacts { get; } = new List<CodingField>
    {
        // Guard-specific fields.
        new("Sonderfahrzeug_Z07_Notoeffnung_inaktiv", 8, 1, 1, EvidenceLevel.StrongInference,
            "Vehicle is model 221.176 S 600 Guard / B6-B7 (Z07). Exact original coding dump has not been found."),
        new("Sonderfahrzeug_Z11Z12Z13Z14_Automatischer_Hochlauf_Fensterheber_gesperrt", 11, 1, 1, EvidenceLevel.StrongInference,
            "Guard-specific window lifter configuration. Consistent with Z07 Guard production, but exact original bit is not backed up."),

        // Historical 2024 topology evidence.
        new("889_Keyless_Go_SA", 16, 1, 1, EvidenceLevel.Confirmed,
            "Historical pre-fault topology shows KG present and communicating."),
        new("614618615616_Xenon_Scheinwerfer_SA", 20, 1, 1, EvidenceLevel.Confirmed,
            "Historical pre-fault topology shows XALWA-L/XALWA-R xenon control units."),

        // DTR is present, but the old 219 and newer 233 coding choices must not be guessed.
        new("219_Abstandsregeltempomat_Distronic_SA", 30, 1, null, EvidenceLevel.Unresolved,
            "DTR is present. Need to distinguish code 219 from code 233 before coding."),
        new("610_Nightview_SA", 31, 1, 1, EvidenceLevel.Confirmed,
            "Historical pre-fault topology shows NSA (Night View Assist) present."),
        new("233_Advanced_Distronic_SA", 34, 1, null, EvidenceLevel.Unresolved,
            "Do not set together with 219 until the exact equipment code is resolved."),

        new("223224_El__Fondlehnenverstellung_und_Kopfstuetzen_SA", 40, 1, 1, EvidenceLevel.StrongInference,
            "Historical topology shows rear electric seat/multicontour modules; exact SA mapping still needs confirmation."),
        new("224_El__Fondeinzelsitz_und_Kopfstuetzen_SA", 41, 1, 1, EvidenceLevel.StrongInference,
            "Historical topology strongly suggests electrically adjusted individual rear seats."),

        new("Reifendruckmodul_Bauart_SA", 46, 2, 2, EvidenceLevel.StrongInference,
            "Direct TPMS control unit N88 is present; D7 value 2 corresponds to code 475 Mid-Line."),
        new("Feste_Hoechstgeschwindigkeit", 48, 8, 0xD2, EvidenceLevel.StrongInference,
            "210 km/h is the factory governed speed for the 2008 S 600 Guard B6/B7; verify against the ECU/datacard before writing."),

        // Structural vehicle identity.
        new("DC_Group", 64, 4, 0, EvidenceLevel.Confirmed,
            "Mercedes-Benz/Maybach group."),
        new("Laendercode", 68, 4, null, EvidenceLevel.Unresolved,
            "Original country coding has not yet been recovered."),
        new("Baureihe", 72, 6, 0, EvidenceLevel.Confirmed,
            "BR 221."),
        new("Sonderschutzklasse", 78, 2, 2, EvidenceLevel.Confirmed,
            "S 600 Guard B6/B7 / Z07."),
        new("Aenderungsjahr_Version", 80, 2, null, EvidenceLevel.Unresolved,
            "Do not infer from calendar date alone."),
        new("Aenderungsjahr_Jahr", 82, 5, null, EvidenceLevel.Unresolved,
            "A near-sequential Guard VIN is MY808, but the exact D7 encoding must be verified."),
        new("Karosserie", 88, 5, 1, EvidenceLevel.Confirmed,
            "V - long-wheelbase sedan."),
        new("Lenkervariante", 93, 2, 1, EvidenceLevel.Confirmed,
            "Left-hand drive, confirmed by the diagnostic vehicle selection/history."),

        new("423427_Getriebesteuerung_Serie", 104, 1, 1, EvidenceLevel.Confirmed,
            "Automatic transmission control is present."),
        new("580581_KlimaanlageKlimatisierungsautomatik_Serie", 105, 1, 1, EvidenceLevel.Confirmed,
            "Automatic climate-control system is present."),
        new("551_Einbruch__und_Diebstahlwarnanlage___EDW_SA", 107, 1, null, EvidenceLevel.Unresolved,
            "Likely on this specification, but not yet proven from the target vehicle data."),
        new("882_Innenraumabsicherung_SA", 108, 1, null, EvidenceLevel.Unresolved,
            "Not yet proven from the target vehicle data."),
        new("228_Standheizung_SA", 109, 1, 1, EvidenceLevel.Confirmed,
            "Historical pre-fault topology shows STH present."),
        new("Dachvariante", 117, 3, null, EvidenceLevel.Unresolved,
            "Roof option not yet recovered."),

        new("Fahrspurassistent", 149, 1, 0, EvidenceLevel.StrongInference,
            "No lane-assist ECU has been identified in the historical topology; keep as inference until datacard is found."),
        new("Totwinkelassistent", 150, 1, null, EvidenceLevel.Unresolved,
            "Radar hardware exists, but blind-spot equipment code has not been proven."),
        new("Elektromotorenvariante", 152, 6, 0, EvidenceLevel.Confirmed,
            "Conventional M275 V12; no hybrid electric motor."),
        new("StoppStart_Automatik", 159, 1, 0, EvidenceLevel.Confirmed,
            "No start-stop system on this M275/722.649 configuration."),
        new("Power_steering_style___HPSEHPSEPS", 168, 2, 0, EvidenceLevel.StrongInference,
            "Hydraulic power steering expected for this W221 generation; verify before write."),
    };

    public sealed record CodingOverlay(byte[] Value, byte[] Mask)
    {
        public string ValueHex => Convert.ToHexString(Value);
        public string MaskHex => Convert.ToHexString(Mask);
    }

    /// <summary>
    /// Builds a value/mask pair. A mask bit of 1 means that bit is backed by the selected
    /// evidence level. A mask bit of 0 means W221Diag must NOT overwrite that bit.
    /// </summary>
    public static CodingOverlay BuildOverlay(bool includeStrongInferences = false)
    {
        var value = new byte[GlobalCodingLength];
        var mask = new byte[GlobalCodingLength];

        foreach (var field in TargetFacts)
        {
            if (field.Value is null)
                continue;

            bool accepted = field.Evidence == EvidenceLevel.Confirmed ||
                            (includeStrongInferences && field.Evidence == EvidenceLevel.StrongInference);
            if (!accepted)
                continue;

            SetField(value, mask, field.BitOffset, field.BitLength, field.Value.Value);
        }

        return new CodingOverlay(value, mask);
    }

    /// <summary>
    /// Applies only masked bits to an existing 24-byte coding dump. This is intended for
    /// offline comparison/reconstruction; it does not communicate with an ECU.
    /// </summary>
    public static byte[] ApplyOverlay(ReadOnlySpan<byte> original, CodingOverlay overlay)
    {
        if (original.Length != GlobalCodingLength)
            throw new ArgumentException($"D7 global coding must be {GlobalCodingLength} bytes.", nameof(original));

        var result = original.ToArray();
        for (int i = 0; i < result.Length; i++)
            result[i] = (byte)((result[i] & ~overlay.Mask[i]) | (overlay.Value[i] & overlay.Mask[i]));

        return result;
    }

    private static void SetField(byte[] value, byte[] mask, int bitOffset, int bitLength, ulong fieldValue)
    {
        if (bitOffset < 0 || bitLength <= 0 || bitOffset + bitLength > GlobalCodingLength * 8)
            throw new ArgumentOutOfRangeException(nameof(bitOffset));

        if (bitLength < 64 && fieldValue >= (1UL << bitLength))
            throw new ArgumentOutOfRangeException(nameof(fieldValue));

        // Caesar CBF variant coding uses little-endian bit numbering inside each byte:
        // bit 0 is byte0 bit0, bit 7 is byte0 bit7, bit 8 is byte1 bit0, etc.
        for (int i = 0; i < bitLength; i++)
        {
            int absoluteBit = bitOffset + i;
            int byteIndex = absoluteBit / 8;
            int bitInByte = absoluteBit % 8;
            byte bitMask = (byte)(1 << bitInByte);

            mask[byteIndex] |= bitMask;
            if (((fieldValue >> i) & 1UL) != 0)
                value[byteIndex] |= bitMask;
            else
                value[byteIndex] &= (byte)~bitMask;
        }
    }
}
