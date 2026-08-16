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
}
