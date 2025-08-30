using Robust.Shared.Timing;

namespace Content.Goobstation.Common.Overlays
{
    public interface IFadeEffectManager
    {
        void Initialize();
        void FrameUpdate(FrameEventArgs args);
        void FadeIn(float time = 1.0f);
        void FadeOut(float time = 1.0f);
    }
}
