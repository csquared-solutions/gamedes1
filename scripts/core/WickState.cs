using System;

namespace Wick.Core;

public sealed class WickState : IPlayerWick
{
    private int _flareTurnsRemaining;

    public int CurrentWick { get; private set; }
    public int CurrentRadius => (CurrentWick / 2) * (_flareTurnsRemaining > 0 ? 2 : 1);
    public bool IsDepleted => CurrentWick <= 0;
    public bool IsFlareActive => _flareTurnsRemaining > 0;

    public event Action<int> WickChanged;
    public event Action Depleted;

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
        return true;
    }

    public void AdvanceTurn()
    {
        if (_flareTurnsRemaining > 0)
        {
            _flareTurnsRemaining--;
        }
    }
}
