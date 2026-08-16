using Godot;

namespace Wick.Core;

public partial class TurnManager : Node
{
    public int CurrentTurn { get; private set; }

    public void AdvanceTurn()
    {
        CurrentTurn++;
    }
}
