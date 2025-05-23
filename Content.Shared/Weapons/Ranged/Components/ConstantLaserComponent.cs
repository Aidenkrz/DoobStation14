// SPDX-FileCopyrightText: 2023 AIGoogle
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Physics.Dynamics; // For CollisionGroup, though the specific enum is in Content.Shared.Physics
using Content.Shared.Physics; // For CollisionGroup.MobMask
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom; // For CustomTypeSerializer on EntityUid if needed later

namespace Content.Shared.Weapons.Ranged.Components
{
    [RegisterComponent]
    public partial class ConstantLaserComponent : Component
    {
        [DataField("isFiring")]
        public bool IsFiring = false;

        [DataField("range")]
        public float Range = 10f;

        [DataField("damagePerSecond")]
        public float DamagePerSecond = 5f; // TODO: Consider DamageSpecifier for typed damage

        [DataField("effectPrototype")]
        public string EffectPrototype = "Beam";

        [DataField("targetFixtureCollisionGroup")]
        public int TargetFixtureCollisionGroup = (int)CollisionGroup.MobMask;

        [DataField("activeBeamEntity")]
        public EntityUid? ActiveBeamEntity = null;
    }
}
