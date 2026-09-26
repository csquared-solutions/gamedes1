using Godot;

namespace Wick.Core;

public partial class GameEvents : Node
{
    [Signal] public delegate void WickChangedEventHandler(int wickValue);
    [Signal] public delegate void LightOutEventHandler(string cause);
    [Signal] public delegate void LevelClearedEventHandler(int levelIndex, int remainingWick);
}
