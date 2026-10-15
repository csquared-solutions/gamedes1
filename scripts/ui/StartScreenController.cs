using Godot;
using Wick.Core;

namespace Wick.Ui;

public partial class StartScreenController : Control
{
    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!Visible || inputEvent is not InputEventKey key)
        {
            return;
        }

        // Swallow every key while the title card is up so R/P/Z cannot start play behind it.
        GetViewport().SetInputAsHandled();
        if (!key.Pressed || key.Echo || key.Keycode is not (Key.Enter or Key.KpEnter))
        {
            return;
        }

        Visible = false;
        GetNode<LevelManager>("../../LevelManager").BeginGame();
    }
}
