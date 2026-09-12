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
