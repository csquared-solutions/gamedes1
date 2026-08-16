using Godot;
using Wick.Actors;

namespace Wick.Ui;

public partial class LampTetherController : Node2D
{
    private PlayerController _player;
    private LampController _lamp;

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("../Player");
        _lamp = GetNode<LampController>("../Lamp");
    }

    public override void _Process(double delta) => QueueRedraw();

    public int GetReturnCost()
    {
        if (_player is null || _lamp is null)
        {
            return 0;
        }

        return Mathf.Abs(_player.Cell.X - _lamp.Cell.X) + Mathf.Abs(_player.Cell.Y - _lamp.Cell.Y);
    }

    public override void _Draw()
    {
        if (_player is null || _lamp is null || _lamp.IsCarried)
        {
            return;
        }

        DrawDashedLine(_player.Position, _lamp.Position, new Color(1f, 0.78f, 0.25f, 0.72f), 2f, 9f);
    }
}
