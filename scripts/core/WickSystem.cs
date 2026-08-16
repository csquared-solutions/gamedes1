using Godot;

namespace Wick.Core;

public partial class WickSystem : Node
{
    public int CurrentWick { get; private set; }
    public int CurrentRadius => CurrentWick / 2;
    public bool IsDepleted => CurrentWick <= 0;

    public void Initialize(int startingWick) => CurrentWick = startingWick;

    public bool Spend(int amount)
    {
        if (amount < 0 || amount > CurrentWick)
        {
            return false;
        }

        CurrentWick = Mathf.Max(0, CurrentWick - amount);
        return true;
    }

    public void Restore(int amount) => CurrentWick += amount;
}
