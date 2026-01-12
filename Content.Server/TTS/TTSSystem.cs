using System.Linq;
using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Shared._Goobstation.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.TTS;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;

    private readonly List<string> _sampleText =
    [
        "Hello station, I have teleported the janitor.",
        "Yes, Ms. Sarah, about the theater issue -- will Engineering be dealing with it?",
        "Since Samuel was detained should we change it to a code green?",
        "He wants to do an interview, where are you?",
        "Samuel Rodriguez broke the door to the bridge with an e-mag!",
        "I want to give credit where it's due -- the newspaper is working, and it's doing quite well. I like it.",
        "Praise and glory from NT.",
        "Will someone build a podium in the theater?",
        "Clown, I'm about to be interviewed, I'll be gone about 10 minutes.",
        "Chief, I'm about to be interviewed, I'll be gone for about 10 minutes.",
        "As far as I understand, the anomaly broke the barrier between the Singularity and the station."
    ];

    private const int MaxMessageChars = 100 * 2;
    private bool _isEnabled;

    public override void Initialize()
    {
        _cfg.OnValueChanged(GoobCVars.TTSEnabled, OnTtsEnabledChanged, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);
    }

    private async void OnTtsEnabledChanged(bool enabled)
    {
        _isEnabled = enabled;

        if (enabled)
            await EnsureVoicesDownloaded();
    }

    private async Task EnsureVoicesDownloaded()
    {
        var requiredVoices = _prototypeManager
            .EnumeratePrototypes<TTSVoicePrototype>()
            .Select(v => v.Model)
            .Distinct()
            .ToList();

        await _ttsManager.EnsureVoicesDownloadedAsync(requiredVoices);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _ttsManager.ClearCache();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        if (!_cfg.GetCVar(GoobCVars.TTSCacheRoundPersistence))
            _ttsManager.ClearCache();
    }

    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled
            || !_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
        {
            return;
        }

        var previewText = _rng.Pick(_sampleText);
        var soundData = await GenerateTTS(previewText, protoVoice.Model, protoVoice.Speaker);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData), Filter.SinglePlayer(args.SenderSession));
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        var voiceId = component.VoicePrototypeId;
        if (!_isEnabled || args.Message.Length > MaxMessageChars || voiceId == null)
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return;

        if (args.IsWhisper)
            HandleWhisper(uid, args.Message, protoVoice.Model, protoVoice.Speaker);
        else
            HandleSay(uid, args.Message, protoVoice.Model, protoVoice.Speaker);
    }

    private async void HandleSay(EntityUid uid, string message, string model, string speaker)
    {
        var soundData = await GenerateTTS(message, model, speaker);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData, GetNetEntity(uid)), Filter.Pvs(uid));
    }

    private async void HandleWhisper(EntityUid uid, string message, string model, string speaker)
    {
        var soundData = await GenerateTTS(message, model, speaker, true);
        if (soundData is null)
            return;

        var ttsEvent = new PlayTTSEvent(soundData, GetNetEntity(uid), true);
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);

        foreach (var session in Filter.Pvs(uid).Recipients)
        {
            if (!session.AttachedEntity.HasValue)
                continue;

            var xform = xformQuery.GetComponent(session.AttachedEntity.Value);
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();

            if (distance <= 100) // 10 * 10
                RaiseNetworkEvent(ttsEvent, session);
        }
    }

    private async Task<byte[]?> GenerateTTS(string text, string model, string speaker, bool isWhisper = false)
    {
        var textSanitized = Sanitize(text);
        if (string.IsNullOrEmpty(textSanitized))
            return null;

        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        return await _ttsManager.ConvertTextToSpeech(model, speaker, textSanitized);
    }
}

public sealed class TransformSpeakerVoiceEvent(EntityUid sender, string voiceId) : EntityEventArgs
{
    public EntityUid Sender = sender;
    public string VoiceId = voiceId;
}
