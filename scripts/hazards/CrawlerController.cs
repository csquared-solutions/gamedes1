using Wick.Board;

namespace Wick.Hazards;

public sealed class CrawlerController : HazardBase
{
    private bool _enteredLitSpace;
    private bool _skipNextLitTurn;

    public CrawlerController(GridCoord startCell) : base(startCell)
    {
    }

    public override GridCoord TakeTurn(HazardContext context)
    {
        var nextStep = FindNextStep(context);
        if (nextStep != Cell && !context.IsLit(Cell) && context.IsLit(nextStep) && !_enteredLitSpace)
        {
            _enteredLitSpace = true;
            return Cell;
        }

        if (context.IsLit(Cell) && _skipNextLitTurn)
        {
            _skipNextLitTurn = false;
            return Cell;
        }

        var previousCell = Cell;
        MoveTowardLamp(context);
        if (Cell != previousCell && context.IsLit(Cell))
        {
            _skipNextLitTurn = true;
        }

        return Cell;
    }

    public CrawlerSnapshot CaptureSnapshot() => new(Cell, HasReachedLamp, _enteredLitSpace, _skipNextLitTurn);

    public void RestoreSnapshot(CrawlerSnapshot snapshot)
    {
        Cell = snapshot.Cell;
        HasReachedLamp = snapshot.HasReachedLamp;
        _enteredLitSpace = snapshot.EnteredLitSpace;
        _skipNextLitTurn = snapshot.SkipNextLitTurn;
    }
}

public readonly record struct CrawlerSnapshot(
    GridCoord Cell,
    bool HasReachedLamp,
    bool EnteredLitSpace,
    bool SkipNextLitTurn);
