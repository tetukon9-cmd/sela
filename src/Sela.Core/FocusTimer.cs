namespace Sela.Core;

public enum FocusSession { Focus, ShortBreak, LongBreak }
public enum FocusStatus { Ready, Running, Paused, Completed }

public sealed record FocusSnapshot(FocusSession Session, FocusStatus Status, TimeSpan Remaining);

/// <summary>
/// In-memory, manually selected focus sessions. Elapsed timestamps avoid wall-clock/time-zone jumps;
/// a late poll (including after sleep) completes once. No automatic cycles, storage, or network.
/// Independently implemented from feature observations documented in docs/nusa-static-analysis.md.
/// </summary>
public sealed class FocusTimer(TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private FocusSession _session;
    private FocusStatus _status;
    private TimeSpan _remaining = Duration(FocusSession.Focus);
    private long _startedAt;
    private bool _completionPending;

    public static TimeSpan Duration(FocusSession session) => TimeSpan.FromMinutes(session switch
    {
        FocusSession.Focus => 25,
        FocusSession.ShortBreak => 5,
        FocusSession.LongBreak => 15,
        _ => throw new ArgumentOutOfRangeException(nameof(session)),
    });

    public FocusSnapshot Snapshot
    {
        get
        {
            var remaining = Remaining();
            if (_status == FocusStatus.Running && remaining == TimeSpan.Zero)
            {
                _remaining = TimeSpan.Zero;
                _status = FocusStatus.Completed;
                _completionPending = true;
            }
            return new(_session, _status, remaining);
        }
    }

    public void Select(FocusSession session)
    {
        var duration = Duration(session); // reject invalid input before changing state
        _session = session;
        _remaining = duration;
        _status = FocusStatus.Ready;
        _completionPending = false;
    }

    public void Start()
    {
        var snapshot = Snapshot;
        if (snapshot.Status == FocusStatus.Running) return;
        if (snapshot.Status == FocusStatus.Completed) _remaining = Duration(_session);
        _completionPending = false;
        _startedAt = _time.GetTimestamp();
        _status = FocusStatus.Running;
    }

    public void Pause()
    {
        var snapshot = Snapshot;
        if (snapshot.Status != FocusStatus.Running) return;
        _remaining = snapshot.Remaining;
        _status = FocusStatus.Paused;
    }

    public void Reset() => Select(_session);

    /// <summary>Consumes completion at most once per run; call before starting a new run.</summary>
    public bool TakeCompletion()
    {
        _ = Snapshot;
        var completed = _completionPending;
        _completionPending = false;
        return completed;
    }

    private TimeSpan Remaining()
    {
        if (_status != FocusStatus.Running) return _remaining;
        var remaining = _remaining - _time.GetElapsedTime(_startedAt);
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
