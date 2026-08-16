using Godot;

namespace Wick.Core;

public partial class TurnManager : Node, ITurnAdvancer
{
    private readonly TurnState _state = new();

    public int CurrentTurn => _state.CurrentTurn;

    public void AdvanceTurn() => _state.AdvanceTurn();

    public void Reset() => _state.Reset();
}
