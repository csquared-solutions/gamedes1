namespace Wick.Core;

public sealed class TurnState : ITurnAdvancer
{
    public int CurrentTurn { get; private set; }

    public void AdvanceTurn()
    {
        CurrentTurn++;
    }

    public void Reset()
    {
        CurrentTurn = 0;
    }
}
