using System;

namespace Wick.Core;

public sealed class WickState : IPlayerWick
{
    private int _flareTurnsRemaining;
    private bool _decreaseFrozen;

    public int CurrentWick { get; private set; }
    public int CurrentRadius => (CurrentWick / 2) * (_flareTurnsRemaining > 0 ? 2 : 1);
    public bool IsDepleted => CurrentWick <= 0;
    public bool IsFlareActive => _flareTurnsRemaining > 0;
    public bool IsDecreaseFrozen => _decreaseFrozen;

    public event Action<int> WickChanged;
    public event Action Depleted;
    public event Action FlareFired;

    public void StartFreshLevel(int startingWick, bool freezeDecrease)
    {
        Initialize(startingWick);
        SetDecreaseFrozen(freezeDecrease);
    }

    public void Initialize(int startingWick)
    {
        _flareTurnsRemaining = 0;

        if (CurrentWick == startingWick)
        {
            return;
        }

        CurrentWick = startingWick;
        WickChanged?.Invoke(CurrentWick);
    }

    public bool Spend(int amount)
    {
        if (amount < 0 || amount > CurrentWick)
        {
            return false;
        }

        if (_decreaseFrozen)
        {
            return true;
        }

        var previousWick = CurrentWick;
        CurrentWick -= amount;

        if (CurrentWick != previousWick)
        {
            WickChanged?.Invoke(CurrentWick);
        }

        if (previousWick > 0 && CurrentWick == 0)
        {
            Depleted?.Invoke();
        }

        return true;
    }

    public void Restore(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        CurrentWick += amount;
        WickChanged?.Invoke(CurrentWick);
    }

    public bool TryFireFlare()
    {
        if (CurrentWick < Data.LevelTuning.FlareCost)
        {
            return false;
        }

        Spend(Data.LevelTuning.FlareCost);
        _flareTurnsRemaining = 1;
        FlareFired?.Invoke();
        return true;
    }

    public void AdvanceTurn()
    {
        if (_flareTurnsRemaining > 0)
        {
            _flareTurnsRemaining--;
        }
    }

    public void SetDecreaseFrozen(bool frozen) => _decreaseFrozen = frozen;

    public WickSnapshot CaptureSnapshot() => new(CurrentWick, _flareTurnsRemaining, _decreaseFrozen);

    public void RestoreSnapshot(WickSnapshot snapshot)
    {
        var changed = CurrentWick != snapshot.CurrentWick;
        CurrentWick = snapshot.CurrentWick;
        _flareTurnsRemaining = snapshot.FlareTurnsRemaining;
        _decreaseFrozen = snapshot.IsDecreaseFrozen;
        if (changed)
        {
            WickChanged?.Invoke(CurrentWick);
        }
    }
}

public readonly record struct WickSnapshot(int CurrentWick, int FlareTurnsRemaining, bool IsDecreaseFrozen);
