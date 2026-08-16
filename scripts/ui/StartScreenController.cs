using Godot;
using Wick.Core;

namespace Wick.Ui;

public partial class StartScreenController : Control
{
    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true, Echo: false } key ||
            key.Keycode is not (Key.Enter or Key.KpEnter))
        {
            return;
        }

        Visible = false;
        GetNode<LevelManager>("../../LevelManager").BeginGame();
        GetViewport().SetInputAsHandled();
    }
}
