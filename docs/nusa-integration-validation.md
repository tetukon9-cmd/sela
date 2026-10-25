# Nusa-inspired focus timer: validation

Validated on Windows x64 on 2026-10-25 with the Microsoft .NET SDK 10.0.402.
The SDK ZIP was obtained from Microsoft's .NET release metadata and its
SHA-512 was checked against that metadata. The SDK and inspection tools live
outside the repository; no SDK/package version was changed in Sela.

## Results

| Check | Result | Evidence / scope |
| --- | --- | --- |
| Baseline Release build | PASS | Base `2767256e11245435da26e332e08f6317e24cffea`, zero warnings/errors |
| Baseline core tests | PASS | 171 passed, zero failed/skipped |
| Changed solution Release build | PASS | Zero warnings/errors |
| Changed core tests | PASS | 180 passed, zero failed/skipped, including 9 new focus-timer cases |
| New C# files: whitespace verification | PASS | `FocusTimer.cs`, `App.Focus.cs`, `NotchWindow.Focus.cs`, `FocusTimerTests.cs` |
| All touched C# files: whitespace verification | FAIL, pre-existing | Exactly 10 findings in unchanged dictionary-initializer lines of `Contracts.cs` (103–105, 120–121, 135). Reproduced at the same line/column locations in a separate archive of the baseline commit. No new finding from this change. Unrelated formatting left intact. |
| Diff whitespace | PASS | `git diff --check` |
| ZIP integrity | PASS | CRC verification and exactly one expected entry |
| WPF integration probe | PASS | 22 assertions using the real `App`, `NotchState`, `NotchWindow`, and routed button/selection events, with a controlled timestamp provider |
| English/Indonesian panel renders | PASS | Rendered actual WPF content offscreen; inspected both languages and checked hint bounds |

The repository does not define a separate lint target. `dotnet format
whitespace --verify-no-changes` was used as an additional formatting check;
the broad check is **not** represented as passing.

Core tests cover all three durations, completion at the exact boundary,
one-shot completion, delayed polls, repeated Start, pause/resume excluding
paused time, reset, replacing a session, explicit restart, invalid session
input, and independence from wall-clock reads.

The desktop probe instantiates the app without its startup path, injects a
controlled focus clock, drives the real controls, and calls the real app clock
tick. It checks Start, Pause, Resume, Reset, session selection, countdown,
completion queue entry and deduplication, live translation, and navigation
across all four tabs. It keeps reminders queued in panel mode and renders the
panel offscreen. This avoids calendar/network access, user settings, registry
changes, and notification sounds. Static renders settle the tab animation for
capture; they do not verify animation timing.

This is automated desktop integration evidence, not a full interactive user
acceptance run. Native notification display/audio, physical suspend/resume,
mixed-DPI monitors, and installer/release packaging were not exercised. The
late-poll/sleep behavior is proven with the controlled clock, not an actual
machine suspension. Nusa executables were never executed.

## Reproduction

From a Windows checkout with a .NET 10 SDK:

```powershell
dotnet build Sela.sln -c Release
dotnet test tests/Sela.Core.Tests -c Release --no-build
dotnet format whitespace Sela.sln --verify-no-changes --no-restore --include src/Sela.Core/FocusTimer.cs src/Sela/App.Focus.cs src/Sela/Views/NotchWindow.Focus.cs tests/Sela.Core.Tests/FocusTimerTests.cs
git diff --check
```

For an interactive follow-up, start Sela in its existing `--mock --lang=en
--panel` mode, open Focus, exercise the three session choices and controls,
and repeat with `--lang=id`. Completing a session queues the existing reminder
card; leaving the panel lets that queue display. Mock mode protects calendar
and settings flows but is not evidence about a real account or live services.

## Files added or changed

| File | Change |
| --- | --- |
| `docs/nusa-static-analysis.md` | Added static evidence, input/tool hashes, compatibility audit, provenance, and integration limits |
| `docs/nusa-integration-validation.md` | Added validation results and reproduction commands |
| `src/Sela.Core/FocusTimer.cs` | Added original timer state machine with injected elapsed-time provider |
| `src/Sela.Core/Contracts.cs` | Appended focus reminder kind, preserving existing enum values |
| `src/Sela/App.Focus.cs` | Added app-owned timer actions and one-shot completion delivery |
| `src/Sela/App.xaml.cs` | Connected timer polling to existing app clock |
| `src/Sela/AppContracts.cs` | Added focus state and view-to-app commands |
| `src/Sela/Views/NotchWindow.Focus.cs` | Added timer rendering and routed control handlers |
| `src/Sela/Views/NotchWindow.Content.cs` | Registered fourth tab, sizing, and reminder appearance |
| `src/Sela/Views/NotchWindow.xaml` | Added Focus panel and fourth tab using Sela controls |
| `src/Sela/Views/NotchWindow.xaml.cs` | Refreshes focus content with existing state changes |
| `src/Sela/Localization/Loc.Notch.cs` | Added English/Indonesian focus text |
| `tests/Sela.Core.Tests/FocusTimerTests.cs` | Added timer regression cases |
| `README.md` | Documented behavior and feature inspiration |
| `README.id.md` | Documented behavior and feature inspiration in Indonesian |

No Nusa executable, extracted asset, raw string dump, decompiled code,
credential, token, new dependency, license file, or release artifact is added.
The original Sela license and copyright remain unchanged. See the
[static inspection](nusa-static-analysis.md) for the scope and unresolved gaps.
