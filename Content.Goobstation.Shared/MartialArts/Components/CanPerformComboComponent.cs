using Content.Goobstation.Common.MartialArts;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.MartialArts.Components;
[RegisterComponent]
[NetworkedComponent]
public sealed partial class CanPerformComboComponent : Component
{
    [DataField]
    public EntityUid? CurrentTarget;

    [DataField]
    public ProtoId<ComboPrototype> BeingPerformed;

    [DataField]
    public List<ComboAttackType> LastAttacks = new();

    [DataField]
    public List<ComboPrototype> AllowedCombos = new();

    [DataField]
    public List<ProtoId<ComboPrototype>> RoundstartCombos = new();

    [DataField]
    public TimeSpan ResetTime = TimeSpan.Zero;

    [DataField]
    public int ConsecutiveGnashes = 0;
}
