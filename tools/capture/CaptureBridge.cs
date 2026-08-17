using Godot;
using Wick.Actors;
using Wick.Board;
using Wick.Core;
using Wick.Goals;
using Wick.Hazards;
using Wick.Pickups;

namespace Wick.Tools;

public partial class CaptureBridge : Node
{
    public void StageMothPreview()
    {
        var previewCell = new GridCoord(13, 9);
        var player = GetNode<PlayerController>("../GameRoot/Player");
        var lamp = GetNode<LampController>("../GameRoot/Lamp");
        var board = GetNode<BoardRenderer>("../GameRoot/Board/BoardRenderer");
        var wick = GetNode<WickSystem>("../GameRoot/WickSystem");
        var hazards = GetNode<HazardSystem>("../GameRoot/HazardSystem");
        var flasks = GetNode<OilFlaskController>("../GameRoot/OilFlask");
        var stair = GetNode<StairController>("../GameRoot/Stair");

        player.RestoreCell(previewCell);
        lamp.AttachToPlayer(previewCell);
        lamp.SyncPosition();
        board.RecomputeVisibility(previewCell, wick.CurrentRadius);
        hazards.RefreshVisibility();
        flasks.RefreshVisibility();
        stair.RefreshVisibility();
    }

    public void StageStairClear()
    {
        var player = GetNode<PlayerController>("../GameRoot/Player");
        var lamp = GetNode<LampController>("../GameRoot/Lamp");
        var stair = GetNode<StairController>("../GameRoot/Stair");
        var board = GetNode<BoardRenderer>("../GameRoot/Board/BoardRenderer");
        var wick = GetNode<WickSystem>("../GameRoot/WickSystem");
        var hazards = GetNode<HazardSystem>("../GameRoot/HazardSystem");
        var flasks = GetNode<OilFlaskController>("../GameRoot/OilFlask");

        wick.Spend(wick.CurrentWick - 3);
        player.RestoreCell(stair.Cell);
        lamp.AttachToPlayer(stair.Cell);
        board.RecomputeVisibility(stair.Cell, wick.CurrentRadius);
        stair.RefreshVisibility();
        hazards.RefreshVisibility();
        flasks.RefreshVisibility();
        stair.TryEnter(stair.Cell);
    }

    public void StageDepleted()
    {
        var wick = GetNode<WickSystem>("../GameRoot/WickSystem");
        wick.Spend(wick.CurrentWick);
    }

    public void StageSmothered()
    {
        GetNode<GameEvents>("../GameRoot/GameEvents")
            .EmitSignal(GameEvents.SignalName.LightOut, "smothered");
    }
}
