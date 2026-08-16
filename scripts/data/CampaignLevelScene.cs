using Godot;

namespace Wick.Data;

public partial class CampaignLevelScene : Node
{
    [Export] public int Index { get; set; }

    public LevelDefinition Definition => new LevelLoader().Load(Index);
}
