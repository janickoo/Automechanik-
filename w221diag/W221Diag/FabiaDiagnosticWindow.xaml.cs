using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace W221Diag;

public partial class FabiaDiagnosticWindow : Window
{
    private TexaSessionInspector.SessionInspection? _session;
    private bool _dirty;

    public FabiaDiagnosticWindow()
    {
        InitializeComponent();
        ProcedureBox.ItemsSource = FabiaDiagnosticProfile.Procedures;
        ProcedureBox.SelectedIndex = 0;
        _dirty = false;
        Closing += ConfirmClose;
    }

    private FabiaDiagnosticProfile.CaseInput ReadInput() => new(
        VinBox.Text.Trim(), YearBox.Text.Trim(), MileageBox.Text.Trim(), TransmissionBox.Text.Trim(),
        SymptomsBox.Text.Trim(), ConditionsBox.Text.Trim(), DtcsBox.Text.Trim(),
        RepairsBox.Text.Trim(), MeasurementsBox.Text.Trim());

    private void Input_Changed(object sender, TextChangedEventArgs e)
    {
        _dirty = true;
        if (ImportStatusText is not null && VinBox is not null) UpdateImportStatus();
    }

    private void Procedure_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (WorkflowBox is null || ProcedureBox.SelectedItem is not string procedure) return;
        WorkflowBox.Text = FabiaDiagnosticProfile.GetWorkflow(procedure);
        _dirty = true;
    }

    private void UpdateImportStatus()
    {
        if (_session is null)
        {
            ImportStatusText.Text = "TEXA session zatiaľ nenačítaná.";
            return;
        }
        var vehicle = _session.Vehicle;
        var match = FabiaDiagnosticProfile.CheckVehicle(vehicle, VinBox.Text);
        string details = $"{vehicle?.Brand} {vehicle?.Model} | {vehicle?.EngineCode} | VIN {vehicle?.Vin}";
        ImportStatusText.Text = details + Environment.NewLine + (match switch
        {
            VehicleMatch.Match => $"Zhoda profilu a VIN. ECU: {_session.EcuResults.Count}, DTC: {_session.DtcCount}.",
            VehicleMatch.Mismatch => "NEZHODA vozidla, motora alebo VIN — importované DTC nebudú vložené do protokolu Fabie.",
            _ => "Identifikácia nie je úplná. Doplň platný VIN z tejto session; bez overenej zhody sa DTC nepriradia k Fabii."
        });
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Vyber súbor z TEXA session Fabie",
            Filter = "TEXA session (*.xml;*.bin;*.pdemo;*.pjson;*.xjson)|*.xml;*.bin;*.pdemo;*.pjson;*.xjson|Všetky súbory|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true) return;
        _session = null;
        UpdateImportStatus();
        ImportButton.IsEnabled = false;
        StatusText.Text = "Načítavam TEXA session…";
        try
        {
            string folder = Path.GetDirectoryName(dialog.FileName) ?? throw new InvalidDataException("Chýba priečinok.");
            _session = await Task.Run(() => TexaSessionInspector.InspectFolder(folder));
            _dirty = true;
            UpdateImportStatus();
            StatusText.Text = "Session načítaná. Over údaje a VIN v pravej časti.";
        }
        catch (Exception ex)
        {
            _session = null;
            UpdateImportStatus();
            StatusText.Text = "Import zlyhal: " + ex.Message;
        }
        finally { ImportButton.IsEnabled = true; }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var procedure = ProcedureBox.SelectedItem as string ?? "Vstupná kontrola";
        var report = FabiaDiagnosticProfile.CreateReport(ReadInput(), procedure, _session);
        var dialog = new SaveFileDialog
        {
            Title = "Uložiť diagnostický protokol Fabie",
            Filter = "Čitateľný protokol (*.txt)|*.txt|JSON protokol (*.json)|*.json",
            FileName = $"Fabia-CBZA-{DateTime.Now:yyyyMMdd-HHmmss}"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(new JsonStringEnumConverter());
            string text = dialog.FilterIndex == 2 ? JsonSerializer.Serialize(report, options) : RenderReport(report);
            File.WriteAllText(dialog.FileName, text, System.Text.Encoding.UTF8);
            _dirty = false;
            StatusText.Text = $"Uložené: {dialog.FileName}" +
                (report.MissingInputs.Count == 0 ? "" : " | Chýba: " + string.Join(", ", report.MissingInputs));
        }
        catch (Exception ex) { StatusText.Text = "Uloženie zlyhalo: " + ex.Message; }
    }

    private static string RenderReport(FabiaDiagnosticProfile.CaseReport report)
    {
        var input = report.Input;
        var lines = new List<string>
        {
            report.Vehicle, $"Vytvorené UTC: {report.CreatedAtUtc:O}",
            $"VIN: {input.Vin} | Rok: {input.Year} | km: {input.Mileage} | Prevodovka: {input.Transmission}",
            "Symptómy: " + input.Symptoms, "Podmienky: " + input.Conditions,
            "Ručne zapísané DTC: " + input.Dtcs, "Predchádzajúce zásahy: " + input.PreviousRepairs,
            "Chýbajúce údaje: " + string.Join(", ", report.MissingInputs),
            "Overenie importu: " + report.ImportStatus,
            "", "MERANIA", input.Measurements, "", "POSTUP", report.Workflow, "", "OVERENÝ TEXA IMPORT"
        };
        foreach (var ecu in report.ImportedEcus)
        {
            lines.Add("ECU: " + ecu.ShortName);
            foreach (var dtc in ecu.Dtcs) lines.Add($"DTC {dtc.Code} | stav {dtc.Status} | {dtc.Detail}");
        }
        if (report.ImportStatus != VehicleMatch.Match) lines.Add("Importované ECU/DTC neboli priradené: chýba overená zhoda vozidla, motora a VIN.");
        return string.Join(Environment.NewLine, lines);
    }

    private void Network_Click(object sender, RoutedEventArgs e) => new MainWindow { Owner = this }.Show();

    private void ConfirmClose(object? sender, CancelEventArgs e)
    {
        if (_dirty && MessageBox.Show(this, "Údaje ešte nie sú uložené. Zavrieť bez uloženia?",
            "Rozpracovaný protokol", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            e.Cancel = true;
    }
}
