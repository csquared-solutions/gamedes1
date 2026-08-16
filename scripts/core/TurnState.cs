namespace Wick.Core;

public sealed class TurnState
{
    public int CurrentTurn { get; private set; }

    public void AdvanceTurn()
    {
        CurrentTurn++;
    }
}
