using System;
using System.Collections.Generic;
using System.Linq;
using Wick.Board;
using Wick.Core;

namespace Wick.Hazards;

public sealed class HazardSimulationState
{
    private readonly BoardState _board;
    private readonly CrawlerController[] _crawlers;
    private readonly MothController[] _moths;
    private readonly HazardPathTieBreaker _tieBreaker;
    private HazardMotionState[] _crawlerMotion;
    private HazardMotionState[] _mothMotion;
    private bool _smothered;

    public HazardSimulationState(
        BoardState board,
        IEnumerable<GridCoord> crawlerStarts,
        IEnumerable<GridCoord> mothStarts,
        uint tieBreakerSeed = 1)
    {
        _board = board ?? throw new ArgumentNullException(nameof(board));
        _tieBreaker = new HazardPathTieBreaker(tieBreakerSeed);
        _crawlers = crawlerStarts.Select(cell => new CrawlerController(cell)).ToArray();
        _moths = mothStarts.Select(cell => new MothController(cell)).ToArray();
        _crawlerMotion = Enumerable.Repeat(HazardMotionState.Stalled, _crawlers.Length).ToArray();
        _mothMotion = Enumerable.Repeat(HazardMotionState.Stalled, _moths.Length).ToArray();
    }

    public IReadOnlyList<GridCoord> CrawlerCells => _crawlers.Select(crawler => crawler.Cell).ToArray();
    public IReadOnlyList<GridCoord> MothCells => _moths.Select(moth => moth.Cell).ToArray();
    public IReadOnlyList<HazardMotionState> CrawlerMotion => _crawlerMotion;
    public IReadOnlyList<HazardMotionState> MothMotion => _mothMotion;
    public bool IsSmothered => _smothered;

    public bool AdvanceTurn(VisibilitySystem visibility, GridCoord lampCell, int radius)
    {
        if (_smothered)
        {
            return false;
        }

        var context = new HazardContext(_board, visibility, lampCell, radius, _tieBreaker);
        for (var index = 0; index < _crawlers.Length; index++)
        {
            var previous = _crawlers[index].Cell;
            _crawlers[index].TakeTurn(context);
            _crawlerMotion[index] = GetCrawlerMotion(previous, _crawlers[index].Cell, context);
        }

        for (var index = 0; index < _moths.Length; index++)
        {
            var previous = _moths[index].Cell;
            _moths[index].TakeTurn(context);
            _mothMotion[index] = previous == _moths[index].Cell
                ? HazardMotionState.Stalled
                : HazardMotionState.Moving;
        }

        if (!_crawlers.Any(crawler => crawler.HasReachedLamp) && !_moths.Any(moth => moth.HasReachedLamp))
        {
            return false;
        }

        _smothered = true;
        return true;
    }

    public HazardSimulationSnapshot CaptureSnapshot() => new(
        _crawlers.Select(crawler => crawler.CaptureSnapshot()).ToArray(),
        _moths.Select(moth => moth.CaptureSnapshot()).ToArray(),
        _crawlerMotion.ToArray(),
        _mothMotion.ToArray(),
        _tieBreaker.CaptureState(),
        _smothered);

    public void RestoreSnapshot(HazardSimulationSnapshot snapshot)
    {
        if (snapshot.Crawlers.Length != _crawlers.Length || snapshot.Moths.Length != _moths.Length)
        {
            throw new ArgumentException("Hazard snapshot does not match the authored hazard set.", nameof(snapshot));
        }

        for (var index = 0; index < _crawlers.Length; index++)
        {
            _crawlers[index].RestoreSnapshot(snapshot.Crawlers[index]);
        }

        for (var index = 0; index < _moths.Length; index++)
        {
            _moths[index].RestoreSnapshot(snapshot.Moths[index]);
        }

        _crawlerMotion = snapshot.CrawlerMotion.ToArray();
        _mothMotion = snapshot.MothMotion.ToArray();
        _tieBreaker.RestoreState(snapshot.TieBreakerState);
        _smothered = snapshot.IsSmothered;
    }

    private static HazardMotionState GetCrawlerMotion(GridCoord previous, GridCoord current, HazardContext context) =>
        previous == current
            ? HazardMotionState.Stalled
            : context.IsLit(current) ? HazardMotionState.Slowed : HazardMotionState.Moving;
}

public sealed record HazardSimulationSnapshot(
    CrawlerSnapshot[] Crawlers,
    MothSnapshot[] Moths,
    HazardMotionState[] CrawlerMotion,
    HazardMotionState[] MothMotion,
    uint TieBreakerState,
    bool IsSmothered);
