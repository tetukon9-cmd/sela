# Nusa package inspection and Sela integration

Inspected on 2026-10-25. Input: the user-supplied `NusaSetup_2.zip`.
This is a static inspection of one specific package, not a malware clearance,
runtime test, source recovery, or a claim of feature parity with Nusa.

## Input identity and archive structure

| Object | Bytes | SHA-256 |
| --- | ---: | --- |
| `NusaSetup_2.zip` | 2,294,679 | `1eac6e9206a3a17ca1edaff81cbde169475d1135613b2a45aed088d99749ef27` |
| ZIP entry `NusaSetup.exe` | 2,809,698 | `63170651afbabd0aefce1a011b01a8fb28c887790e3463da5952e2236c533c99` |
| Installer payload `{app}/nusa.exe` | 909,312 | `2dfa517fe20fb944cd263597e9f731b8245fc2b5253d9f1a241221636e8d1bdb` |

The ZIP has exactly one entry, `NusaSetup.exe`, compressed with DEFLATE
(2,294,483 compressed bytes). No source tree, dependency manifest, external
configuration, source map, PDB, or reuse license is included in the ZIP.

## Method and tooling provenance

1. Listed ZIP entries before extraction; read only the explicitly named entry.
2. Calculated SHA-256 hashes and inspected PE headers, import tables, resource
   tables, version records, and ASCII/UTF-16 strings without loading either PE.
3. Used Python 3.14, `pefile` 2024.8.26, and the separately downloaded
   `innounp` 2.67.11 to list and unpack the installer as data. Neither
   `NusaSetup.exe` nor `nusa.exe` was launched, installed, or loaded as a library.
4. Inspected the recovered installer description and application strings.
   No Nusa API request, user-data read, registry change, or uninstaller run
   was needed.

The unpacker came from the maintainer's
[InnoUnpacker repository](https://github.com/jrathlev/InnoUnpacker-Windows-GUI/tree/master/innounp-2).
Downloaded `innounp-267.zip` was checked against Git blob
`38095659b9f86bdeee339a56b4d8cd4485dabece`; its SHA-256 was
`ac1d98bba6588072ade06163781938df274f444bec7318069668608d1e5faae8`.
The unpacker executable SHA-256 was
`1c8453c198dd3fe7947f3705e59d2294ddce3dfa9f96a737e94449b119f44181`.
These hashes identify the analysis tool; they do not certify it or Nusa as safe.

## Installer and binary format

| Area | Directly observed evidence | Interpretation / limit |
| --- | --- | --- |
| Installer | PE32, machine `0x014c`; `Inno Setup Setup Data (6.7.0)`; unpacker recognizes Unicode 6.7.0 | 32-bit Inno loader distributing an x64 app |
| App | PE32+, machine `0x8664`; CLR directory RVA `0`; no overlay | Native x64 executable; not a managed .NET assembly |
| Language | `src\main.rs`, `/rustc/…` paths, `windows-0.58.0`, `windows-core-0.58.0` strings | Strong Rust / windows-rs evidence; exact compiler version and complete dependency graph unverified |
| Graphics | Imports `D2D1CreateFactory`, `DWriteCreateFactory`, `DCompositionCreateDevice`, `D3D11CreateDevice` | Uses native Windows graphics interfaces; internal rendering architecture unverified |
| OS services | Win32 window/tray/registry/drag-drop functions and WinRT class names | Native Windows integration; presence of a symbol alone does not prove a runtime action |
| Signing | PE security directories empty; Windows Authenticode reports `NotSigned` for both files | Publisher authenticity is not established by signing |
| Version | App version resource: Nusa `1.0.0`, company `Rama`; installer `1.0.0.0` | Self-declared metadata, not independent authorship verification |

No Electron, Chromium, Python bundle, Qt package, managed CLR payload, or
separate runtime package was observed. The string `mscoree.dll` alone is not
evidence of .NET: the app's CLR directory is empty and its imports are native.

## Recovered installer contents and effects

| Recovered path | Bytes | Role |
| --- | ---: | --- |
| `{app}/nusa.exe` | 909,312 | Sole installed application file |
| `embedded/CompiledCode.bin` | 946 | Compiled installer Pascal script; not executed or fully decompiled |
| `embedded/WizardImage0.bmp` | 154,542 | Installer artwork |
| `embedded/WizardSmallImage0.bmp` | 9,798 | Installer artwork |
| `embedded/en.isl` | 205 | Extractor-generated language stub |
| `install_script.iss` | 2,588 | Extractor-reconstructed description, not original source |

The recovered description declares publisher `Rama`, homepage/support/update
URL `https://rama2555.github.io/nusa/`, per-user privileges (`lowest`), x64
compatibility, default directory `{autopf}\Nusa`, Start menu and optional
desktop shortcuts, and optional startup value `Nusa` in
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. Its post-install
action launches `{app}\nusa.exe`. The custom message about deleting local
data on uninstall exists, but the compiled script's actual deletion behavior
was not verified. Extraction did not perform these actions.

Both PEs contain 15 icon-image resources plus an icon group and version
metadata. The installer additionally contains string tables, raw resources,
and a manifest; the app has no manifest resource. No standalone media,
lyrics database, font, task database, or configuration file was installed by
the recovered file list. Graphics/audio generated at runtime cannot be
enumerated from this file list.

## Configuration, endpoints, and feature evidence

Offsets below are decimal byte offsets in the application identified above.
They are string evidence, not a fully reconstructed protocol or file schema.

| Evidence | Offset / source | What can be concluded |
| --- | --- | --- |
| `https://lrclib.net/api/search?` | 604040 | Lyrics search endpoint literal; not contacted |
| `track_name=`, `&artist_name=` | 604001, 604014 | Search parameter-name literals; full query construction and HTTP method unverified |
| `Nusa-Notch/1.0 (Windows)` | App string | User-agent-like literal |
| `Windows.Web.Http.HttpClient` | 649824 | WinRT HTTP client class name |
| `GlobalSystemMediaTransportControlsSessionManager` | 649873 | System media-session API name; media-control behavior not exercised |
| `UserNotificationListener` | 650043 | Notification-listener API name; permissions/access not verified |
| `LOCALAPPDATA`, `NusaShelf`, `APPDATA` | App strings | Local storage-location hints; exact directory construction unverified |
| `settings.txt`, `minimal=` | 603506, 603526 | Settings filename and key hints |
| `todo.txt`, `shelf.txt`, `water.txt` | 607326, 606408, 608437 | Local persistence filenames; no user files read or imported |
| `bottle=`, `goal=`, `start=`, `taken=`, `fired=`, `hours=` | 608473–608546 | Water-settings/log key hints; units/encoding/schema unverified |
| `25 min`, `5 minute short break`, `15 min` | 606874, 606896, 606932 | Focus and short/long-break duration labels |
| `Task Time!`, `Add Task`, `Edit Task`, `Time to Drink`, `Time to Refill` | App strings | To-do and hydration UI/reminder concepts |
| File-drop, charging, connectivity, Caps Lock, mascot, minimal-notch labels | App strings/imports | Additional UI concepts, not runtime acceptance proof |

The publisher's [Nusa page](https://rama2555.github.io/nusa/) describes a Rust
Windows notch with music, lyrics, focus, hydration, tasks, and a file shelf.
This corroborates the feature labels; its privacy, performance, antivirus,
and runtime claims were not independently tested. No credential or token was
identified in the inspected configuration/endpoint literals. Raw string dumps
and binaries are deliberately excluded from this repository.

## Sela compatibility audit and integration boundary

Repository: `tetukon9-cmd/sela`, default branch `main`, inspected base
`2767256e11245435da26e332e08f6317e24cffea`. Existing copyright and MIT license
remain in place.

| Sela area | Existing responsibility | Integration decision |
| --- | --- | --- |
| `src/Sela.Core` | Platform-neutral .NET 10 services and scheduling | Add an original, deterministic focus-timer state machine |
| `src/Sela` | WPF .NET 10 Windows notch, bilingual text, app-owned state/commands | Add a Focus tab and use the existing app clock and reminder queue |
| `tests/Sela.Core.Tests` | xUnit | Exercise timer lifecycle, delayed ticks, pause/resume, and completion deduplication |
| `.github/workflows/ci.yml` | Windows Release build + core tests | Keep the same validation path; no Nusa tool/runtime dependency |
| `installer`, `tools` | Inno installer and release/media tools | No Nusa binaries, artwork, startup entries, or packaging changes |

Rust machine code is not a compatible C# project reference. The supplied
package provides no reuse license for its code or artwork. Do not treat free
download availability as redistribution permission. This change uses only
general feature ideas and independently written Sela code; it does not copy
machine code, decompiled implementation, branding, layout, icons, text
collections, music, or lyrics. This is not a formal independently staffed
clean-room process or a legal opinion.

The selected feature is a local focus timer with explicit choices of
25-minute focus, 5-minute short break, and 15-minute long break. Start,
pause/resume, reset, remaining time, and one completion card form the complete
path. Sela's behavior and UI are original: sessions are manually selected,
there is no assumed Nusa cycle algorithm, no automatic next session, and no
session persistence across app exit. No network or new package is required.

Music control/lyrics, hydration, to-do, shelf, hardware overlays, notification
access, Nusa file-format import, and asset reuse remain outside this change.
Their exact contracts/permissions or reuse rights are not established. Adding
them would require a separate implementation and acceptance scope. This PR
does not claim to port all of Nusa or reproduce its behavior.

## Remaining limits

- No installer/application execution, dynamic network capture, unpacked-source
  recovery, complete call-graph analysis, or malware verdict.
- No verified correspondence between this local file and the publisher's
  current release; self-declared metadata is recorded as evidence only.
- No full Nusa storage schemas or installer compiled-script reconstruction.
- Sela's focus sessions are in memory; closing Sela discards them. Completion
  cards use Sela's existing queue and can wait behind another reminder.

See [integration validation](nusa-integration-validation.md) for the actual
build, test, and review results for this branch.
