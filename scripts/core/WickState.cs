using System;

namespace Wick.Core;

public sealed class WickState
{
    public int CurrentWick { get; private set; }
    public int CurrentRadius => CurrentWick / 2;
    public bool IsDepleted => CurrentWick <= 0;

    public event Action<int> WickChanged;
    public event Action Depleted;

    public void Initialize(int startingWick)
    {
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
}
