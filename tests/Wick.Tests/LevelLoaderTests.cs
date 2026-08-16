using Wick.Board;
using Wick.Data;
using Xunit;

namespace Wick.Tests;

public sealed class LevelLoaderTests
{
    [Fact]
    public void Load_ReturnsAuthoredSingleRoomDefinition_ByIndex()
    {
        var definition = new LevelDefinition
        {
            Index = 1,
            StartingWick = 12,
            PlayerStart = new GridCoord(1, 1),
            LampStart = new GridCoord(1, 1),
            StairCell = new GridCoord(17, 11)
        };
        var loader = new LevelLoader(new[] { definition });

        var loaded = loader.Load(1);

        Assert.Same(definition, loaded);
        Assert.Equal(19, loaded.Width);
        Assert.Equal(13, loaded.Height);
    }
}
