using Godot;
using Wick.Board;

namespace Wick.Actors;

public sealed class LampState
{
    public LampState(GridCoord cell)
    {
        Cell = cell;
        IsCarried = true;
    }

    public GridCoord Cell { get; private set; }
    public bool IsCarried { get; private set; }

    public void AttachToPlayer(GridCoord playerCell)
    {
        IsCarried = true;
        Cell = playerCell;
    }

    public void Place(GridCoord cell)
    {
        IsCarried = false;
        Cell = cell;
    }

    public LampSnapshot CaptureSnapshot() => new(Cell, IsCarried);

    public void RestoreSnapshot(LampSnapshot snapshot)
    {
        Cell = snapshot.Cell;
        IsCarried = snapshot.IsCarried;
    }
}

public readonly record struct LampSnapshot(GridCoord Cell, bool IsCarried);

public partial class LampController : Node2D
{
    private static readonly Vector2 BoardOrigin = new(152, 104);
    private const int CellSize = 48;

    public LampState State { get; private set; } = new(new GridCoord(1, 1));
    public GridCoord Cell => State.Cell;
    public bool IsCarried => State.IsCarried;

    public void Initialize(GridCoord startCell)
    {
        State = new LampState(startCell);
        SyncPosition();
    }

    public void AttachToPlayer(GridCoord playerCell)
    {
        State.AttachToPlayer(playerCell);
        SyncPosition();
    }

    public void Place(GridCoord cell)
    {
        State.Place(cell);
        SyncPosition();
    }

    public void RestoreSnapshot(LampSnapshot snapshot)
    {
        State.RestoreSnapshot(snapshot);
        SyncPosition();
    }

    public void SyncPosition()
    {
        Position = BoardOrigin + new Vector2((State.Cell.X + 0.5f) * CellSize, (State.Cell.Y + 0.5f) * CellSize);
    }
}
