using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using System.Numerics;
using Robust.Client.UserInterface;

namespace Content.Goobstation.Client.Overlays
{
    public sealed class FadeOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;
        public int? RenderOrder { get; } = int.MaxValue;
        private readonly ShaderInstance _shader;

        public float Opacity { get; set; } = 0.0f;

        public FadeOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("Fade").InstanceUnique();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (args.Space != OverlaySpace.ScreenSpace)
                return;

            _shader.SetParameter("opacity", Opacity);

            var screenHandle = args.ScreenHandle;
            screenHandle.UseShader(_shader);
            var screenBounds = UIBox2.FromDimensions(Vector2.Zero, _uiManager.RootControl.PixelSize);
            screenHandle.DrawRect(screenBounds, Color.White);
            screenHandle.UseShader(null);
        }
    }
}
