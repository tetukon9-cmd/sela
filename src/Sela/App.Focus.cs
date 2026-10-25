using Sela.Core;
using Sela.Localization;

namespace Sela;

public partial class App
{
    private readonly FocusTimer _focus = new();

    public void SelectFocus(FocusSession session)
    {
        _focus.Select(session);
        UpdateFocus();
    }

    public void StartFocus()
    {
        UpdateFocus(); // deliver an elapsed run before explicitly starting another
        _focus.Start();
        UpdateFocus();
    }

    public void PauseFocus()
    {
        _focus.Pause();
        UpdateFocus();
    }

    public void ResetFocus()
    {
        _focus.Reset();
        UpdateFocus();
    }

    private void UpdateFocus()
    {
        _state.Focus = _focus.Snapshot;
        if (!_focus.TakeCompletion()) return;
        _notch.ShowReminder(new Reminder($"focus|{Guid.NewGuid():N}", ReminderKind.Focus,
            Loc.F("Focus_CompletedTitle", Loc.T($"Focus_{_state.Focus.Session}")),
            Loc.T("Focus_CompletedBody"), null, null, DateTime.Now));
    }
}
