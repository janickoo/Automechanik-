using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace W221Diag;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<HostRow> _hosts = new();
    private readonly string _captureDir;
    private string? _etlPath;
    private string? _pcapPath;
    private IReadOnlyList<CaptureAnalyzer.FlowSummary>? _captureFlows;
    private string? _analyzedPath;
    private string? _analyzedTarget;

    public MainWindow()
    {
        InitializeComponent();
        HostsList.ItemsSource = _hosts;
        _captureDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "W221Diag", "Captures");
        Directory.CreateDirectory(_captureDir);
        Log("W221Diag spusteny v read-only analyzatore.");
        Log("USB sa nepouziva. Ciel: Wi-Fi komunikacia PC <-> TXT Multihub.");
        Log("Offline import vie citat TEXA data.xml, rgeFB.xml a identifikovat sifrovane session subory.");
    }

    private async void AnalyzeCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Otvoriť PCAPNG záznam komunikácie",
            Filter = "PCAPNG záznam (*.pcapng)|*.pcapng",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true) return;
        AnalyzeCaptureButton.IsEnabled = false;
        ExportCaptureButton.IsEnabled = false;
        _captureFlows = null;
        FlowsList.ItemsSource = null;
        CaptureSummaryText.Text = "Analyzujem záznam…";
        string target = TargetIpBox.Text.Trim();
        try
        {
            var flows = await Task.Run(() => CaptureAnalyzer.Analyze(dialog.FileName, target));
            _captureFlows = flows;
            _analyzedPath = dialog.FileName;
            _analyzedTarget = target;
            FlowsList.ItemsSource = flows;
            CaptureSummaryText.Text = $"{Path.GetFileName(dialog.FileName)} | toky: {flows.Count} | pakety: {flows.Sum(f => (long)f.Packets)} | filter: {(target.Length == 0 ? "všetky IPv4" : target)}";
            ExportCaptureButton.IsEnabled = true;
            Log(flows.Count == 0
                ? "Nenašli sa podporované IPv4 toky pre zvolený filter. Prázdny výsledok nepotvrdzuje neprítomnosť komunikácie."
                : "PCAPNG načítané. Porty sú pozorované sieťové údaje; diagnostický protokol zatiaľ nie je identifikovaný.");
        }
        catch (Exception ex)
        {
            CaptureSummaryText.Text = "Analýza zlyhala.";
            Log("Chyba PCAPNG: " + ex.Message);
        }
        finally { AnalyzeCaptureButton.IsEnabled = true; }
    }

    private void ExportCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        if (_captureFlows is null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Uložiť výsledok analýzy",
            Filter = "JSON protokol (*.json)|*.json",
            FileName = Path.GetFileNameWithoutExtension(_analyzedPath) + "-flows.json"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var report = new
            {
                SchemaVersion = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CaptureFile = Path.GetFileName(_analyzedPath),
                TargetIpv4 = _analyzedTarget,
                Scope = "Offline PCAPNG; Ethernet IPv4, directional flows; no diagnostic protocol decoding",
                Flows = _captureFlows
            };
            File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            Log("Protokol uložený: " + dialog.FileName);
        }
        catch (Exception ex) { Log("Export protokolu zlyhal: " + ex.Message); }
    }

    private void ImportSessionButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Vyber data.xml, rgeFB.xml alebo iny subor z TEXA session",
            Filter = "TEXA session (*.xml;*.bin;*.pdemo;*.pjson;*.xjson)|*.xml;*.bin;*.pdemo;*.pjson;*.xjson|Vsetky subory (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
            return;

        string? folder = Path.GetDirectoryName(dialog.FileName);
        if (string.IsNullOrWhiteSpace(folder))
            return;

        try
        {
            var inspection = TexaSessionInspector.InspectFolder(folder);
            var vehicle = inspection.Vehicle;
            string vehicleText = vehicle is null
                ? "vozidlo nezistene"
                : $"{vehicle.Brand} {vehicle.Model} | VIN {vehicle.Vin}";

            SessionSummaryText.Text = $"{vehicleText} | ECU: {inspection.EcuResults.Count} | DTC: {inspection.DtcCount}";
            Log("OFFLINE TEXA SESSION");
            Log($"Priecinok: {inspection.Folder}");
            if (!string.IsNullOrWhiteSpace(inspection.SessionName))
                Log($"Session: {inspection.SessionName}");
            if (vehicle is not null)
            {
                Log($"Vozidlo: {vehicle.Brand} {vehicle.Model}");
                Log($"Motor: {vehicle.Engine} {vehicle.EngineCode}".Trim());
                Log($"VIN: {vehicle.Vin}");
                Log($"IDC/TEXA product version: {vehicle.ProductVersion}");
            }

            Log($"ECU zaznamy: {inspection.EcuResults.Count}; DTC zaznamy: {inspection.DtcCount}");

            foreach (var ecu in inspection.EcuResults.Where(x => x.Dtcs.Count > 0))
            {
                string ecuInfo = ecu.Index is null ? ecu.ShortName : $"{ecu.ShortName}[{ecu.Index}]";
                Log($"{ecuInfo} dataset={ecu.DataSetId} CP5={ecu.Cp5} RD={ecu.ReadState} EA={ecu.ErrorState}");
                foreach (var dtc in ecu.Dtcs)
                    Log($"  DTC {dtc.Code} ST={dtc.Status} D={dtc.Detail}");
            }

            foreach (var file in inspection.Files.Where(x => x.LooksEncrypted))
                Log($"Opaque/encrypted: {file.FileName} ({file.Format}, {file.Size} B)");
        }
        catch (Exception ex)
        {
            Log("Chyba pri importe TEXA session: " + ex.Message);
        }
    }

    private async void DiscoverButton_Click(object sender, RoutedEventArgs e)
    {
        DiscoverButton.IsEnabled = false;
        _hosts.Clear();
        try
        {
            var network = GetPrimaryIpv4Network();
            if (network is null)
            {
                Log("Nenasiel som aktivne IPv4 rozhranie.");
                return;
            }

            Log($"Aktivne rozhranie: {network.Value.localAddress} / {network.Value.prefixLength}");
            if (network.Value.prefixLength < 24)
                Log("Siet je vacsia ako /24. Skenujem iba lokalny /24 segment.");

            var octets = network.Value.localAddress.GetAddressBytes();
            string prefix = $"{octets[0]}.{octets[1]}.{octets[2]}.";
            Log($"Skenujem {prefix}1-254 ...");

            using var gate = new SemaphoreSlim(32);
            var tasks = Enumerable.Range(1, 254).Select(async i =>
            {
                await gate.WaitAsync();
                try
                {
                    string ip = prefix + i;
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(ip, 250);
                    if (reply.Status == IPStatus.Success)
                    {
                        string hostName = "";
                        try { hostName = (await Dns.GetHostEntryAsync(ip)).HostName; } catch { }
                        await Dispatcher.InvokeAsync(() => _hosts.Add(new HostRow(ip, hostName, $"{reply.RoundtripTime} ms")));
                    }
                }
                catch { }
                finally { gate.Release(); }
            });

            await Task.WhenAll(tasks);
            Log($"Hotovo. Aktivnych hostov: {_hosts.Count}");
        }
        finally
        {
            DiscoverButton.IsEnabled = true;
        }
    }

    private void HostsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HostsList.SelectedItem is HostRow row)
        {
            TargetIpBox.Text = row.Ip;
            Log($"Vybrane zariadenie: {row.Ip} {row.HostName}".Trim());
        }
    }

    private async void StartCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        _etlPath = Path.Combine(_captureDir, $"w221diag-{stamp}.etl");
        _pcapPath = Path.Combine(_captureDir, $"w221diag-{stamp}.pcapng");

        try
        {
            await RunProcessAsync("pktmon", "stop");
            await RunProcessAsync("pktmon", $"start --capture --comp nics --pkt-size 0 --file-name \"{_etlPath}\"");
            Log("Zaznam spusteny.");
            if (!string.IsNullOrWhiteSpace(TargetIpBox.Text))
                Log($"Cielovy Multihub: {TargetIpBox.Text.Trim()}");
            StartCaptureButton.IsEnabled = false;
            StopCaptureButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            Log("Nepodarilo sa spustit pktmon: " + ex.Message);
        }
    }

    private async void StopCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RunProcessAsync("pktmon", "stop");
            if (_etlPath is not null && _pcapPath is not null)
            {
                await RunProcessAsync("pktmon", $"pcapng \"{_etlPath}\" -o \"{_pcapPath}\"");
                Log("Zaznam zastaveny a exportovany:");
                Log(_pcapPath);
            }
        }
        catch (Exception ex)
        {
            Log("Chyba pri exporte: " + ex.Message);
        }
        finally
        {
            StartCaptureButton.IsEnabled = true;
            StopCaptureButton.IsEnabled = false;
        }
    }

    private static (IPAddress localAddress, int prefixLength)? GetPrimaryIpv4Network()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                 n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        {
            var props = ni.GetIPProperties();
            foreach (var ua in props.UnicastAddresses)
            {
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
                    return (ua.Address, ua.PrefixLength);
            }
        }
        return null;
    }

    private async Task RunProcessAsync(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Neda sa spustit {fileName}.");
        string stdout = await p.StandardOutput.ReadToEndAsync();
        string stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(stdout)) Log(stdout.Trim());
        if (!string.IsNullOrWhiteSpace(stderr)) Log(stderr.Trim());
        if (p.ExitCode != 0) throw new InvalidOperationException($"{fileName} skoncil kodom {p.ExitCode}.");
    }

    private void Log(string text)
    {
        LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
        LogBox.ScrollToEnd();
    }

    public sealed record HostRow(string Ip, string HostName, string Latency);
}
