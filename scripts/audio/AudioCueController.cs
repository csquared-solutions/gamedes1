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
    private GameEvents _events;
    private AudioStreamPlayer _mothWake;
    private AudioStreamPlayer _lowWick;
    private AudioStreamPlayer _lightOut;
    private int _levelIndex = -1;
    private bool _wasTerminal;

    public override void _Ready()
    {
        _wick = GetNode<WickSystem>("../WickSystem");
        _levels = GetNode<LevelManager>("../LevelManager");
        _hazards = GetNode<HazardSystem>("../HazardSystem");
        _events = GetNode<GameEvents>("../GameEvents");
        _mothWake = GetNode<AudioStreamPlayer>("MothWakeCue");
        _lowWick = GetNode<AudioStreamPlayer>("LowWickCue");
        _lightOut = GetNode<AudioStreamPlayer>("LightOutCue");
        _events.Connect(GameEvents.SignalName.LightOut, Callable.From<string>(OnLightOut));
    }

    public override void _Process(double delta)
    {
        if (_levelIndex != _levels.CurrentLevelIndex || (_wasTerminal && !_levels.IsTerminal))
        {
            _levelIndex = _levels.CurrentLevelIndex;
            _state.Reset();
        }

        _wasTerminal = _levels.IsTerminal;

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

    private void OnLightOut(string cause)
    {
        if ((_state.ReportLightOut() & AudioCue.LightOut) != 0)
        {
            _lightOut.Play();
        }
    }
}
