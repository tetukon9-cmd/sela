using System.Windows;
using System.Windows.Controls;
using Sela.Core;
using Sela.Localization;

namespace Sela.Views;

public partial class NotchWindow
{
    private void RefreshFocus()
    {
        var focus = State.Focus;
        var seconds = (int)Math.Ceiling(focus.Remaining.TotalSeconds);
        FocusCountdown.Text = $"{seconds / 60:00}:{seconds % 60:00}";
        FocusStatusText.Text = $"{Loc.T($"Focus_{focus.Session}")} · {Loc.T($"Focus_{focus.Status}")}";
        FocusToggle.Content = Loc.T(focus.Status switch
        {
            FocusStatus.Running => "Focus_Pause",
            FocusStatus.Paused => "Focus_Resume",
            _ => "Focus_Start",
        });
        // Pausing/resetting first makes replacing a live session an explicit action.
        FocusSessionPicker.IsEnabled = focus.Status != FocusStatus.Running;
        FocusSessionPicker.SelectedIndex = (int)focus.Session;
    }

    private void FocusSession_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (FocusSessionPicker is null || FocusCountdown is null) return; // XAML initialization
        var index = FocusSessionPicker.SelectedIndex;
        if (index >= 0 && index != (int)State.Focus.Session) Commands.SelectFocus((FocusSession)index);
    }

    private void FocusToggle_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (State.Focus.Status == FocusStatus.Running) Commands.PauseFocus();
        else Commands.StartFocus();
    }

    private void FocusReset_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Commands.ResetFocus();
    }
}
