using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace W221Diag;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<HostRow> _hosts = new();
    private readonly string _captureDir;
    private string? _etlPath;
    private string? _pcapPath;

    public MainWindow()
    {
        InitializeComponent();
        HostsList.ItemsSource = _hosts;
        _captureDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "W221Diag", "Captures");
        Directory.CreateDirectory(_captureDir);
        Log("W221Diag spusteny v read-only analyzatore.");
        Log("USB sa nepouziva. Ciel: Wi-Fi komunikacia PC <-> TXT Multihub.");
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
            {
                Log("Siet je vacsia ako /24. Z bezpecnostnych dovodov skenujem iba lokalny /24 segment.");
            }

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
            Log("Vyber zariadenie, ktore zodpoveda Multihubu, alebo zadaj jeho IP rucne.");
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
            Log("Teraz v TEXA IDC otvor W221/ZGW a vykonaj iba citanie identifikacie alebo chyb.");
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
                {
                    int prefix = ua.PrefixLength;
                    return (ua.Address, prefix);
                }
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
