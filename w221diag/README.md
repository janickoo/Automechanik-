# W221Diag

Windows desktop diagnostic project for Mercedes-Benz W221 with TEXA TXT Multihub.

## Current milestone

The current build is intentionally read-only and does not send diagnostic commands to the vehicle.

Implemented:
- WPF/.NET 8 Windows application scaffold
- local IPv4 /24 discovery
- manual selection of TXT Multihub IP address
- Windows Packet Monitor (`pktmon`) capture start/stop
- automatic conversion of ETL capture to PCAPNG
- capture storage under `Documents/W221Diag/Captures`
- administrator manifest for packet capture

## Why Wi-Fi first

The target TXT Multihub has a non-working USB port, therefore this project does not rely on USB/J2534 transport. The first engineering goal is to observe and identify the actual PC <-> TXT Multihub Wi-Fi protocol used while TEXA IDC performs harmless read operations.

## Test procedure

1. Connect the Windows laptop and TXT Multihub in the same working Wi-Fi arrangement used by TEXA IDC.
2. Start `W221Diag` as administrator.
3. Click `Vyhladat zariadenia` and select the Multihub, or enter its IP address manually if already known.
4. Click `Spustit zaznam`.
5. In TEXA IDC connect to the W221 and perform only harmless read operations first: ECU identification or read DTCs.
6. Return to W221Diag and click `Zastavit a exportovat`.
7. The capture is exported to PCAPNG under `Documents/W221Diag/Captures`.

## Next milestone

Analyze the captures to identify:
- TCP/UDP endpoints and ports used by Multihub
- session setup / keepalive behavior
- framing between IDC and the VCI
- CAN request/response transport encapsulation

Only after the transport is understood will direct W221 read-only diagnostics be implemented. Writes/coding are intentionally deferred until read-only communication and backups are proven reliable.


## PCAPNG flow analysis (2026-10-07)

Click **Otvoriť PCAPNG** to inspect an existing capture. Enter the Multihub IPv4 address before opening to filter both directions; leave it empty to show all supported IPv4 traffic. The **Sieťové toky** tab lists directional TCP/UDP endpoints, packet counts and captured frame bytes. **Exportovať JSON** saves the analyzed flows and the filter used. Analysis runs in the background; a failed import clears the previous result and disables export.

The analyzer supports Ethernet IPv4 frames and one 802.1Q tag, multiple PCAPNG sections and either byte order. Unsupported link types and unknown interfaces are skipped. Non-initial IPv4 fragments are counted without ports. Damaged block lengths or trailers are rejected rather than presented as a complete report. IPv6, stream reassembly and TEXA protocol decoding are not implemented. An empty flow list does not prove that no communication occurred. Observed TCP/UDP ports do not establish a vehicle diagnostic protocol.

### Build and regression checks

On a machine with .NET 8 SDK:

```powershell
dotnet run --project w221diag/tests/CaptureAnalyzer.Tests.csproj
dotnet build w221diag/W221Diag/W221Diag.csproj -c Release
```

The dependency-free regression executable exercises target filtering, TCP/UDP, endian handling, section-local interface IDs, IP fragments, damaged captures and invalid filters. The GitHub Actions workflow also builds the WPF app on Windows and uploads a portable Windows build. The application still requires a real Windows/TEXA session to verify packet capture and the desktop controls; it does not send vehicle diagnostic commands.
