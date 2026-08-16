using Godot;

namespace Wick.Core;

public partial class TurnManager : Node
{
    private readonly TurnState _state = new();

    public int CurrentTurn => _state.CurrentTurn;

    public void AdvanceTurn() => _state.AdvanceTurn();
}
