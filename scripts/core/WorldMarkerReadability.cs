using System;
using Wick.Board;

namespace Wick.Core;

public static class WorldMarkerReadability
{
    public static bool ShouldShow(VisibilitySystem visibility, GridCoord cell, bool isConsumed)
    {
        if (visibility is null)
        {
            throw new ArgumentNullException(nameof(visibility));
        }

        return !isConsumed && visibility.IsVisible(cell);
    }
}
