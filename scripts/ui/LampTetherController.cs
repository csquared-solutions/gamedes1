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

        DrawDashedLine(_player.Position, _lamp.Position, new Color(0.04f, 0.035f, 0.025f, 0.9f), 5f, 9f);
        DrawDashedLine(_player.Position, _lamp.Position, new Color(0.85f, 0.67f, 0.31f, 0.82f), 1.5f, 9f);
    }
}
