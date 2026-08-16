using Wick.Board;

namespace Wick.Hazards;

public sealed class MothController : HazardBase
{
    private bool _enteredLitSpace;

    public MothController(GridCoord startCell) : base(startCell)
    {
    }

    public override GridCoord TakeTurn(HazardContext context)
    {
        if (context.LampRadius < 5)
        {
            return Cell;
        }

        var nextStep = FindNextStep(context);
        if (nextStep != Cell && !context.IsLit(Cell) && context.IsLit(nextStep) && !_enteredLitSpace)
        {
            _enteredLitSpace = true;
            return Cell;
        }

        return MoveTowardLamp(context);
    }
}
