namespace Wick.Core;

public sealed class PracticeModeState
{
    private GameplaySnapshot _undoSnapshot;

    public bool IsEnabled { get; private set; }
    public bool CanUndo => IsEnabled && _undoSnapshot is not null;

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        if (!enabled)
        {
            _undoSnapshot = null;
        }
    }

    public void RecordSuccessfulAction(GameplaySnapshot snapshot)
    {
        if (IsEnabled)
        {
            _undoSnapshot = snapshot;
        }
    }

    public bool TryTakeUndo(out GameplaySnapshot snapshot)
    {
        if (!CanUndo)
        {
            snapshot = null;
            return false;
        }

        snapshot = _undoSnapshot;
        _undoSnapshot = null;
        return true;
    }

    public void ClearUndo() => _undoSnapshot = null;
}
