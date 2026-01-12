using Content.Shared._Goobstation.CCVar;
using Content.Shared.TTS;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IAudioManager _audioInt = default!;

    private ISawmill _sawmill = default!;
    private float _volume = GoobCVars.TTSVolume.DefaultValue;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(GoobCVars.TTSVolume, OnTtsVolumeChanged, true);
        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(GoobCVars.TTSVolume, OnTtsVolumeChanged);
    }

    public void RequestPreviewTTS(string voiceId)
    {
        RaiseNetworkEvent(new RequestPreviewTTSEvent(voiceId));
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        _sawmill.Verbose($"Play TTS audio {ev.Data.Length} bytes from {ev.SourceUid} entity");

        if (ev.Data.Length == 0)
        {
            _sawmill.Error("Received empty TTS audio data");
            return;
        }

        var shortArray = new short[ev.Data.Length / 2];
        for (var i = 0; i < shortArray.Length; i++)
            shortArray[i] = (short) ((ev.Data[i * 2 + 1] << 8) | (ev.Data[i * 2] & 0xFF));

        var audioStream = _audioInt.LoadAudioRaw(shortArray, 1, 22050);

        var audioParams = AudioParams.Default
            .WithVolume(GetVolume(ev.IsWhisper))
            .WithMaxDistance(GetDistance(ev.IsWhisper));

        if (ev.SourceUid != null)
            _audio.PlayEntity(audioStream, GetEntity(ev.SourceUid.Value), audioParams);
        else
            _audio.PlayGlobal(audioStream, audioParams);
    }

    private float GetVolume(bool isWhisper)
    {
        var volume = _volume;

        if (isWhisper)
            volume = 0.05f + (volume - 0.05f) * 0.25f;

        volume *= _volume / 3f;

        return SharedAudioSystem.GainToVolume(volume);
    }

    private float GetDistance(bool isWhisper)
    {
        return isWhisper ? 5f : 10f;
    }
}
