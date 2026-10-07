using W221Diag;
var tests = new (string Name, Action Run)[]
{
    ("Matching CBZA and VIN accepted", () => Check(FabiaDiagnosticProfile.CheckVehicle(Vehicle(), "TMB12345678901234") == VehicleMatch.Match, "Matching session rejected")),
    ("Wrong engine rejected", () => Check(FabiaDiagnosticProfile.CheckVehicle(Vehicle() with { EngineCode = "CBZB" }, "TMB12345678901234") == VehicleMatch.Mismatch, "Wrong engine accepted")),
    ("Wrong brand rejected", () => Check(FabiaDiagnosticProfile.CheckVehicle(Vehicle() with { Brand = "Mercedes" }, "TMB12345678901234") == VehicleMatch.Mismatch, "Wrong brand accepted")),
    ("Different VIN rejected", () => Check(FabiaDiagnosticProfile.CheckVehicle(Vehicle(), "TMB12345678901235") == VehicleMatch.Mismatch, "Different car accepted")),
    ("Missing VIN remains unverified", () => Check(FabiaDiagnosticProfile.CheckVehicle(Vehicle(), "") == VehicleMatch.Unknown, "Missing VIN accepted")),
    ("Missing engine remains unverified", () => Check(FabiaDiagnosticProfile.CheckVehicle(Vehicle() with { EngineCode = null }, "TMB12345678901234") == VehicleMatch.Unknown, "Unknown engine accepted")),
    ("Foreign DTCs excluded from Fabia report", () =>
    {
        var report = FabiaDiagnosticProfile.CreateReport(Input(), "Vstupná kontrola", Session(Vehicle() with { Brand = "Mercedes" }));
        Check(report.ImportedEcus.Count == 0 && report.ImportStatus == VehicleMatch.Mismatch, "Foreign DTC leaked into report");
    }),
    ("Matching DTCs retained", () =>
    {
        var report = FabiaDiagnosticProfile.CreateReport(Input(), "Vstupná kontrola", Session(Vehicle()));
        Check(report.ImportedEcus.Count == 1 && report.ImportedEcus[0].Dtcs[0].Code == "P0301", "Matched DTC was lost");
    }),
    ("Incomplete intake remains visible", () =>
    {
        var report = FabiaDiagnosticProfile.CreateReport(Input() with { Symptoms = "", Year = "" }, "Vstupná kontrola", null);
        Check(report.MissingInputs.Contains("Symptómy") && report.MissingInputs.Contains("Rok"), "Missing inputs hidden");
    }),
    ("Invalid VIN never confirms session", () => Check(FabiaDiagnosticProfile.CheckVehicle(Vehicle() with { Vin = "INVALID" }, "INVALID") == VehicleMatch.Unknown, "Malformed VIN accepted"))
};
int failures = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine("PASS " + test.Name); }
    catch (Exception ex) { failures++; Console.WriteLine("FAIL " + test.Name + ": " + ex.Message); }
}
return failures == 0 ? 0 : 1;
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static TexaSessionInspector.VehicleMetadata Vehicle() => new(null, "ŠKODA", "Fabia II Combi", "1.2 TSI", "cbza", "TMB12345678901234");
static FabiaDiagnosticProfile.CaseInput Input() => new("TMB12345678901234", "2012", "100000", "manuál", "test", "teplý", "P0301", "žiadne", "");
static TexaSessionInspector.SessionInspection Session(TexaSessionInspector.VehicleMetadata vehicle)
{
    var ecu = new TexaSessionInspector.EcuScanResult("Engine", null, null, null, null, null, null,
        new[] { new TexaSessionInspector.DtcEntry("P0301", "Stored", null) });
    return new("test", null, vehicle, new[] { ecu }, Array.Empty<TexaSessionInspector.SessionFileInfo>());
}
