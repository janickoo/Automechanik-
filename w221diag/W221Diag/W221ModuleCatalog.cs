namespace W221Diag;

public static class W221ModuleCatalog
{
    public sealed record ModuleDefinition(
        string ShortName,
        string DisplayName,
        string Network,
        bool ReadOnlyReady,
        string Notes);

    public static IReadOnlyList<ModuleDefinition> Modules { get; } = new[]
    {
        new ModuleDefinition("ZGW", "Central Gateway", "CAN-D / gateway", true, "Primary first target for identification and DTC readout."),
        new ModuleDefinition("ME", "Engine control", "CAN-C", false, "Transport mapping pending real capture."),
        new ModuleDefinition("EGS", "Transmission control", "CAN-C", false, "Transport mapping pending real capture."),
        new ModuleDefinition("ESP", "ESP / ABS", "CAN-C", false, "Transport mapping pending real capture."),
        new ModuleDefinition("CGW-BODY", "Body network behind gateway", "CAN-B", false, "Enumerate after gateway transport is decoded.")
    };
}
