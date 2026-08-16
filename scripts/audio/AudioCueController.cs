using Godot;
using Wick.Core;
using Wick.Hazards;

namespace Wick.Audio;

public partial class AudioCueController : Node
{
    private readonly AudioCueState _state = new();
    private WickSystem _wick;
    private LevelManager _levels;
    private HazardSystem _hazards;
    private AudioStreamPlayer _mothWake;
    private AudioStreamPlayer _lowWick;
    private int _levelIndex = -1;

    public override void _Ready()
    {
        _wick = GetNode<WickSystem>("../WickSystem");
        _levels = GetNode<LevelManager>("../LevelManager");
        _hazards = GetNode<HazardSystem>("../HazardSystem");
        _mothWake = GetNode<AudioStreamPlayer>("MothWakeCue");
        _lowWick = GetNode<AudioStreamPlayer>("LowWickCue");
    }

    public override void _Process(double delta)
    {
        if (_levelIndex != _levels.CurrentLevelIndex)
        {
            _levelIndex = _levels.CurrentLevelIndex;
            _state.Reset();
        }

        var cue = _state.Update(_wick.CurrentWick, _wick.CurrentRadius, _hazards.HasMoths);
        if ((cue & AudioCue.MothWake) != 0)
        {
            _mothWake.Play();
        }

        if ((cue & AudioCue.LowWick) != 0)
        {
            _lowWick.Play();
        }
    }
}
