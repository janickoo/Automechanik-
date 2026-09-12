namespace W221Diag;

public static class W221ModuleCatalog
{
    public sealed record ModuleDefinition(
        string ShortName,
        string DisplayName,
        string Network,
        bool ReadOnlyReady,
        string Notes);

    public sealed record KnownDtc(
        string Module,
        string Code,
        string Description,
        string StatusExample);

    public static IReadOnlyList<ModuleDefinition> Modules { get; } = new[]
    {
        new ModuleDefinition("CGW", "Central gateway", "Central CAN bus", true, "Primary first target for identification and DTC readout."),
        new ModuleDefinition("ABR", "Adaptive Brake / ESP", "Chassis CAN bus", false, "Seen in the W221 diagnostic report; transport mapping pending capture."),
        new ModuleDefinition("SGR", "Radar sensors control unit", "Chassis CAN bus", false, "Seen in the W221 diagnostic report; useful for CAN-off and missing-message diagnostics."),
        new ModuleDefinition("EPB", "Electric parking brake", "Front end CAN bus", false, "Seen in the W221 diagnostic report."),
        new ModuleDefinition("TPMS", "Tire Pressure Monitoring System", "Interior CAN bus", false, "Seen in the W221 diagnostic report."),
        new ModuleDefinition("SAM_DR", "Driver signal acquisition and actuation module", "Interior CAN bus", false, "Front SAM / driver-side body electronics."),
        new ModuleDefinition("REAR_SAM", "Rear signal acquisition and actuation module", "Interior CAN bus", false, "Rear SAM body electronics."),
        new ModuleDefinition("SCM", "Steering column module", "Chassis CAN bus", false, "Seen in the W221 diagnostic report."),
        new ModuleDefinition("COMAND", "COMAND / telematics gateway", "MOST / Central CAN", false, "Seen in the W221 diagnostic report."),
        new ModuleDefinition("DCM_FL", "Door control module front left", "Interior CAN bus", false, "Seen in the W221 diagnostic report."),
        new ModuleDefinition("DCM_FR", "Door control module front right", "Interior CAN bus", false, "Seen in the W221 diagnostic report."),
        new ModuleDefinition("DCM_RL", "Door control module rear left", "Interior CAN bus", false, "Seen in the W221 diagnostic report."),
        new ModuleDefinition("DCM_RR", "Door control module rear right", "Interior CAN bus", false, "Seen in the W221 diagnostic report."),
        new ModuleDefinition("KG", "Keyless GO", "Interior CAN bus", false, "Seen in the W221 diagnostic report."),
        new ModuleDefinition("ME", "Engine control", "CAN-C", false, "Transport mapping pending real capture."),
        new ModuleDefinition("EGS", "Transmission control", "CAN-C", false, "Transport mapping pending real capture.")
    };

    public static IReadOnlyList<KnownDtc> KnownDtcs { get; } = new[]
    {
        new KnownDtc("CGW", "9010", "Supply voltage of the control unit too low (undervoltage).", "Stored"),
        new KnownDtc("EPB", "6003", "Variant coding from N93 (Central Gateway) is unacceptable or missing.", "Stored"),
        new KnownDtc("EPB", "5600", "Terminal 30 supply undervoltage.", "Stored"),
        new KnownDtc("ABR", "5775", "Hydraulic traction-system unit A7/3 has an internal fault.", "Stored"),
        new KnownDtc("ABR", "5776", "Hydraulic traction-system unit A7/3 has an internal fault.", "Current and stored"),
        new KnownDtc("ABR", "5781", "Hydraulic traction-system unit A7/3 has an internal fault.", "Current and stored"),
        new KnownDtc("SGR", "7409", "No CAN message received from N47-5 ESP control unit.", "Current and stored"),
        new KnownDtc("SGR", "6972", "Rear short-range radar sensor unit CAN controller: CAN bus OFF.", "Stored"),
        new KnownDtc("SGR", "6902", "Front short-range radar sensor unit CAN controller: CAN bus OFF.", "Stored"),
        new KnownDtc("KG", "93B0", "No CAN message received from EZS.", "Stored")
    };
}
