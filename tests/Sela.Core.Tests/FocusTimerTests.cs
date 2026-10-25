namespace Sela.Core.Tests;

public class FocusTimerTests
{
    private sealed class Clock : TimeProvider
    {
        private long _ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _ticks;
        public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("No wall-clock dependency");
        public void Advance(TimeSpan by) => _ticks += by.Ticks;
    }

    [Theory]
    [InlineData(FocusSession.Focus, 25)]
    [InlineData(FocusSession.ShortBreak, 5)]
    [InlineData(FocusSession.LongBreak, 15)]
    public void Selected_session_runs_to_one_completion(FocusSession session, int minutes)
    {
        var clock = new Clock();
        var timer = new FocusTimer(clock);
        timer.Select(session);
        Assert.Equal(new FocusSnapshot(session, FocusStatus.Ready, TimeSpan.FromMinutes(minutes)), timer.Snapshot);
        timer.Start();
        clock.Advance(TimeSpan.FromMinutes(minutes) - TimeSpan.FromMilliseconds(1));
        Assert.Equal(FocusStatus.Running, timer.Snapshot.Status);
        Assert.False(timer.TakeCompletion());
        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.True(timer.TakeCompletion());
        Assert.False(timer.TakeCompletion());
        Assert.Equal(new FocusSnapshot(session, FocusStatus.Completed, TimeSpan.Zero), timer.Snapshot);
    }

    [Fact]
    public void Pause_resume_excludes_paused_time_and_repeated_start_does_not_extend_run()
    {
        var clock = new Clock();
        var timer = new FocusTimer(clock);
        timer.Start();
        clock.Advance(TimeSpan.FromMinutes(10));
        timer.Start();
        timer.Pause();
        clock.Advance(TimeSpan.FromDays(1));
        timer.Pause();
        Assert.Equal(new FocusSnapshot(FocusSession.Focus, FocusStatus.Paused, TimeSpan.FromMinutes(15)), timer.Snapshot);
        timer.Start();
        clock.Advance(TimeSpan.FromMinutes(15));
        Assert.True(timer.TakeCompletion());
    }

    [Fact]
    public void Delayed_poll_completes_once_and_does_not_start_another_session()
    {
        var clock = new Clock();
        var timer = new FocusTimer(clock);
        timer.Start();
        clock.Advance(TimeSpan.FromHours(8));
        Assert.True(timer.TakeCompletion());
        clock.Advance(TimeSpan.FromHours(1));
        Assert.False(timer.TakeCompletion());
        timer.Pause();
        Assert.Equal(FocusStatus.Completed, timer.Snapshot.Status);
        Assert.Equal(TimeSpan.Zero, timer.Snapshot.Remaining);
    }

    [Fact]
    public void Reset_or_selection_discards_old_run_and_pending_completion()
    {
        var clock = new Clock();
        var timer = new FocusTimer(clock);
        timer.Start();
        clock.Advance(TimeSpan.FromMinutes(2));
        timer.Reset();
        Assert.Equal(FocusStatus.Ready, timer.Snapshot.Status);
        Assert.Equal(TimeSpan.FromMinutes(25), timer.Snapshot.Remaining);
        clock.Advance(TimeSpan.FromHours(1));
        Assert.False(timer.TakeCompletion());
        timer.Start();
        clock.Advance(TimeSpan.FromHours(1));
        Assert.Equal(FocusStatus.Completed, timer.Snapshot.Status);
        timer.Select(FocusSession.LongBreak);
        Assert.False(timer.TakeCompletion());
        Assert.Equal(TimeSpan.FromMinutes(15), timer.Snapshot.Remaining);
    }

    [Fact]
    public void Selecting_while_running_cancels_old_session_without_a_completion()
    {
        var clock = new Clock();
        var timer = new FocusTimer(clock);
        timer.Start();
        clock.Advance(TimeSpan.FromMinutes(24));
        timer.Select(FocusSession.ShortBreak);
        clock.Advance(TimeSpan.FromMinutes(10));
        Assert.False(timer.TakeCompletion());
        Assert.Equal(new FocusSnapshot(FocusSession.ShortBreak, FocusStatus.Ready, TimeSpan.FromMinutes(5)), timer.Snapshot);
    }

    [Fact]
    public void Completed_session_can_be_explicitly_restarted()
    {
        var clock = new Clock();
        var timer = new FocusTimer(clock);
        timer.Start();
        clock.Advance(TimeSpan.FromMinutes(25));
        Assert.True(timer.TakeCompletion());
        timer.Start();
        Assert.Equal(FocusStatus.Running, timer.Snapshot.Status);
        Assert.Equal(TimeSpan.FromMinutes(25), timer.Snapshot.Remaining);
        Assert.False(timer.TakeCompletion());
    }

    [Fact]
    public void Invalid_selection_preserves_current_session()
    {
        var timer = new FocusTimer(new Clock());
        timer.Start();
        var before = timer.Snapshot;
        Assert.Throws<ArgumentOutOfRangeException>(() => timer.Select((FocusSession)99));
        Assert.Equal(before, timer.Snapshot);
    }
}
