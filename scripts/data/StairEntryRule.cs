using Wick.Board;

namespace Wick.Data;

public static class StairEntryRule
{
    public static bool CanEnter(LevelDefinition level, GridCoord playerCell, bool lampIsCarried) =>
        playerCell == level.StairCell && !IsBlockedByCarriedLamp(level, playerCell, lampIsCarried);

    public static bool IsBlockedByCarriedLamp(LevelDefinition level, GridCoord playerCell, bool lampIsCarried) =>
        playerCell == level.StairCell && level.RequiresLampPlacement && lampIsCarried;
}
