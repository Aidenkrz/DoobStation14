using System.Collections.Concurrent;
using Content.Shared._Goobstation.CCVar;
using Content.Shared.TTS;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;

namespace Content.Client.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IAudioManager _audioManager = default!;

    private ISawmill _sawmill = default!;
    private float _volume;
    private float _radioVolume;

    private readonly ConcurrentQueue<QueuedTTS> _radioQueue = new();
    private (EntityUid Entity, AudioComponent Component)? _currentRadioPlaying;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(GoobCVars.TTSVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(GoobCVars.TTSRadioVolume, OnTtsRadioVolumeChanged, true);
        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(GoobCVars.TTSVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(GoobCVars.TTSRadioVolume, OnTtsRadioVolumeChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_currentRadioPlaying.HasValue)
        {
            if (Deleted(_currentRadioPlaying.Value.Entity))
                _currentRadioPlaying = null;
            else
                return;
        }

        if (_radioQueue.TryDequeue(out var queued))
            _currentRadioPlaying = PlayTTSBytes(queued.Data, queued.SourceUid, queued.Params);
    }

    public void RequestPreviewTTS(string voiceId)
    {
        RaiseNetworkEvent(new RequestPreviewTTSEvent(voiceId));
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnTtsRadioVolumeChanged(float volume)
    {
        _radioVolume = volume;
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        _sawmill.Verbose($"Play TTS audio {ev.Data.Length} bytes from {ev.SourceUid} entity");

        if (ev.Data.Length == 0)
        {
            _sawmill.Error("Received empty TTS audio data");
            return;
        }

        var volume = ev.IsRadio ? _radioVolume : _volume;
        var audioParams = AudioParams.Default
            .WithVolume(GetVolume(volume, ev.IsWhisper))
            .WithMaxDistance(GetDistance(ev.IsWhisper, ev.IsRadio));

        var sourceUid = ev.SourceUid.HasValue ? GetEntity(ev.SourceUid.Value) : (EntityUid?) null;

        if (ev.IsRadio)
        {
            _radioQueue.Enqueue(new QueuedTTS(ev.Data, sourceUid, audioParams));
            return;
        }

        PlayTTSBytes(ev.Data, sourceUid, audioParams);
    }

    private (EntityUid Entity, AudioComponent Component)? PlayTTSBytes(byte[] data, EntityUid? sourceUid, AudioParams audioParams)
    {
        var shortArray = new short[data.Length / 2];
        for (var i = 0; i < shortArray.Length; i++)
            shortArray[i] = (short) ((data[i * 2 + 1] << 8) | (data[i * 2] & 0xFF));

        var audioStream = _audioManager.LoadAudioRaw(shortArray, 1, 22050);

        if (sourceUid != null)
            return _audio.PlayEntity(audioStream, sourceUid.Value, audioParams);

        return _audio.PlayGlobal(audioStream, audioParams);
    }

    private float GetVolume(float baseVolume, bool isWhisper)
    {
        var volume = baseVolume;

        if (isWhisper)
            volume = 0.05f + (volume - 0.05f) * 0.25f;

        volume *= baseVolume / 3f;

        return SharedAudioSystem.GainToVolume(volume);
    }

    private float GetDistance(bool isWhisper, bool isRadio)
    {
        if (isRadio)
            return 0f;
        return isWhisper ? 5f : 10f;
    }

    private sealed record QueuedTTS(byte[] Data, EntityUid? SourceUid, AudioParams Params);
}
