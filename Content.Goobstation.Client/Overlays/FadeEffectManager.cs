using Content.Goobstation.Common.Overlays;
using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace Content.Goobstation.Client.Overlays
{
    public sealed class FadeEffectManager : IFadeEffectManager
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private FadeOverlay _overlay = default!;

        private bool _fading = false;
        private bool _fadeIn = false;
        private float _fadeTime = 2.5f;
        private float _fadeTimer = 0.0f;

        public void Initialize()
        {
            _overlay = new FadeOverlay();
        }

        public void FrameUpdate(FrameEventArgs args)
        {
            if (!_fading)
                return;

            _fadeTimer += args.DeltaSeconds;

            if (_fadeTimer >= _fadeTime)
            {
                _fading = false;
                _overlay.Opacity = _fadeIn ? 1.0f : 0.0f;
                if (!_fadeIn)
                    _overlayManager.RemoveOverlay(_overlay);
                return;
            }

            var opacity = _fadeTimer / _fadeTime;
            if (!_fadeIn)
                opacity = 1.0f - opacity;

            _overlay.Opacity = opacity;
        }

        public void FadeIn(float time = 2.5f)
        {
            _overlay.Opacity = 0.0f;
            if (!_overlayManager.HasOverlay<FadeOverlay>())
                _overlayManager.AddOverlay(_overlay);

            _fading = true;
            _fadeIn = true;
            _fadeTime = time;
            _fadeTimer = 0.0f;
        }

        public void FadeOut(float time = 2.5f)
        {
            if (!_overlayManager.HasOverlay<FadeOverlay>())
                _overlayManager.AddOverlay(_overlay);

            _fading = true;
            _fadeIn = false;
            _fadeTime = time;
            _fadeTimer = 0.0f;
        }
    }
}
