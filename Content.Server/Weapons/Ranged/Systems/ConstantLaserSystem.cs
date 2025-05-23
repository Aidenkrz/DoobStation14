// SPDX-FileCopyrightText: 2023 AIGoogle
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Beam;
using Content.Server.Beam.Components;
using Content.Shared.Beam.Components; // As requested
using Content.Shared.Damage;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Physics; // For CollisionGroup, as requested
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems; // For PhysicsSystem, FixtureSystem
using Robust.Shared.Physics; // For CollisionRay, RayCastResults
using Robust.Shared.Maths; // For Vector2, Angle (Angle might be from Robust.Shared.Rotation)
using Robust.Shared.Timing; // For frameTime (or related timing utilities)
using Robust.Shared.Transforms;
using System.Collections.Generic; // For List
using System.Linq; // For Sort, FirstOrDefault
using Content.Shared.Interaction; // For UseInHandEvent
using Content.Shared.Hands;       // For HandDeselectedEvent
using Content.Shared.Item;         // For DroppedEvent

namespace Content.Server.Weapons.Ranged.Systems
{
    public sealed partial class ConstantLaserSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly BeamSystem _beamSystem = default!;
        [Dependency] private readonly PhysicsSystem _physicsSystem = default!; // Specified from Robust.Shared.Physics.Systems
        [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ConstantLaserComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<ConstantLaserComponent, DroppedEvent>(OnDropped);
            SubscribeLocalEvent<ConstantLaserComponent, HandDeselectedEvent>(OnDeselected);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Corrected query to use _entityManager
            var query = _entityManager.EntityQueryEnumerator<ConstantLaserComponent>();
            while (query.MoveNext(out var uid, out var component))
            {
                if (component.IsFiring)
                {
                    if (component.ActiveBeamEntity == null || !_entityManager.EntityExists(component.ActiveBeamEntity))
                    {
                        CreateNewBeam(uid, component);
                    }
                    else
                    {
                        UpdateExistingBeam(uid, component, component.ActiveBeamEntity.Value, frameTime);
                        ApplyContinuousEffects(uid, component, component.ActiveBeamEntity.Value, frameTime);
                    }
                }
                else // Not firing
                {
                    if (component.ActiveBeamEntity != null && _entityManager.EntityExists(component.ActiveBeamEntity.Value))
                    {
                        _entityManager.QueueDeleteEntity(component.ActiveBeamEntity.Value);
                        component.ActiveBeamEntity = null;
                    }
                }
            }
        }

        public void StartFiring(EntityUid uid, ConstantLaserComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.IsFiring = true;
        }

        public void StopFiring(EntityUid uid, ConstantLaserComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.IsFiring = false;
        }

        private void CreateNewBeam(EntityUid owner, ConstantLaserComponent laserComp)
        {
            var ownerTransform = _transformSystem.GetTransform(owner);
            var ownerPos = ownerTransform.WorldPosition;
            var ownerRot = ownerTransform.WorldRotation;
            var dir = ownerRot.ToWorldVec();

            var mapId = ownerTransform.MapID;
            if (mapId == MapId.Nullspace)
            {
                // Cannot perform a raycast on a nullspace map.
                return;
            }

            var ray = new CollisionRay(ownerPos, dir, (int)laserComp.TargetFixtureCollisionGroup);
            var rayCastResults = new List<RayCastResults>();
            // Using SharedPhysicsSystem's IntersectRay which typically takes mapId, ray, length, ref results, ignored entity
            _physicsSystem.IntersectRay(mapId, ray, laserComp.Range, ref rayCastResults, owner);

            var targetPoint = ownerPos + dir * laserComp.Range; // Default to max range
            EntityUid? hitEntity = null;

            if (rayCastResults.Count > 0)
            {
                // Sort results by distance to find the closest hit
                rayCastResults.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                var closestHit = rayCastResults[0];
                targetPoint = closestHit.HitPos;
                hitEntity = closestHit.HitEntity;
            }

            var targetUid = hitEntity;
            EntityUid? tempTarget = null;

            if (targetUid == null)
            {
                // Spawn a temporary entity at the target point if no entity was hit
                // BeamSystem.TryCreateBeam requires an EntityUid for the target.
                // Spawn on the same map as the owner.
                var ownerMapCoords = _transformSystem.GetMapCoordinates(owner);
                tempTarget = _entityManager.SpawnEntity(null, ownerMapCoords);
                _transformSystem.SetWorldPosition(tempTarget.Value, targetPoint);
                targetUid = tempTarget;
            }

            // If targetUid is still null here, something went wrong (e.g. tempTarget failed to spawn)
            if (targetUid == null)
            {
                if (tempTarget != null) // Should not happen if targetUid is null
                    _entityManager.QueueDeleteEntity(tempTarget.Value);
                return;
            }

            // Create the beam controller entity
            var controllerUid = _entityManager.SpawnEntity(null, _transformSystem.GetMapCoordinates(owner));

            // Try to create the beam
            bool beamCreated = _beamSystem.TryCreateBeam(
                user: owner,
                target: targetUid.Value,
                bodyPrototype: laserComp.EffectPrototype,
                controller: controllerUid
            );

            if (beamCreated)
            {
                laserComp.ActiveBeamEntity = controllerUid;
                // BeamSystem's TryCreateBeam should handle setting up the BeamComponent on the controller,
                // including setting its BeamShooter to 'owner'.
            }
            else
            {
                // If beam creation failed, clean up the controller entity we spawned
                _entityManager.QueueDeleteEntity(controllerUid);
            }

            // Clean up the temporary target entity if it was created
            if (tempTarget != null)
            {
                _entityManager.QueueDeleteEntity(tempTarget.Value);
            }
        }

        private void UpdateExistingBeam(EntityUid owner, ConstantLaserComponent laserComp, EntityUid beamControllerUid, float frameTime)
        {
            // Try to get the beam component from the controller
            if (!_entityManager.TryGetComponent<SharedBeamComponent>(beamControllerUid, out var beamComp))
            {
                laserComp.ActiveBeamEntity = null; // Mark active beam as null
                // The main Update loop in this system will call CreateNewBeam on the same or next tick.
                return;
            }

            // Get owner's current transform
            var ownerTransform = _transformSystem.GetTransform(owner);
            var ownerPos = ownerTransform.WorldPosition;
            var ownerRot = ownerTransform.WorldRotation;
            var dir = ownerRot.ToWorldVec();

            // Perform a new raycast
            var mapId = ownerTransform.MapID;
            if (mapId == MapId.Nullspace)
            {
                // Cannot do a raycast on a null map. Invalidate current beam.
                _entityManager.QueueDeleteEntity(beamControllerUid);
                laserComp.ActiveBeamEntity = null;
                return;
            }

            var ray = new CollisionRay(ownerPos, dir, (int)laserComp.TargetFixtureCollisionGroup);
            var rayCastResults = new List<RayCastResults>();
            _physicsSystem.IntersectRay(mapId, ray, laserComp.Range, ref rayCastResults, owner); // Ignore owner

            // Determine the new primary hit entity from the raycast
            EntityUid? newHitEntity = null;
            if (rayCastResults.Count > 0)
            {
                // Sort by distance to get the closest one
                rayCastResults.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                newHitEntity = rayCastResults[0].HitEntity;
            }

            // Determine the current primary target of the existing beam
            // The BeamComponent's HitTargets might contain multiple entities if the beam pierces or hits multiple fixtures.
            // We are interested in the "main" target, usually the first non-owner entity.
            EntityUid? currentPrimaryTarget = null;
            if (beamComp.HitTargets != null)
            {
                currentPrimaryTarget = beamComp.HitTargets.FirstOrDefault(e => e != owner && _entityManager.EntityExists(e));
            }

            // Compare the new hit entity with the current primary target
            if (newHitEntity != currentPrimaryTarget)
            {
                // If the target has changed (e.g., hit a different entity, or went from entity to space, or space to entity)
                // delete the old beam. The main update loop will create a new one.
                _entityManager.QueueDeleteEntity(beamControllerUid);
                laserComp.ActiveBeamEntity = null;
            }
            // Else: The primary target is the same (or both are null, meaning firing into space without hitting a new entity).
            // In this simplified version, we don't do further checks for minor positional adjustments on the same target
            // or subtle aim changes in empty space. The existing beam is considered "good enough".
        }

        private void ApplyContinuousEffects(EntityUid owner, ConstantLaserComponent laserComp, EntityUid beamControllerUid, float frameTime)
        {
            if (!_entityManager.TryGetComponent<SharedBeamComponent>(beamControllerUid, out var beamComp))
                return;

            if (beamComp.HitTargets == null || beamComp.HitTargets.Count == 0)
                return;

            var damageToApply = new DamageSpecifier();
            // TODO: Check for an existing "Laser" DamageTypePrototype ID. For now, using "Heat".
            damageToApply.DamageDict.Add("Heat", laserComp.DamagePerSecond * frameTime);

            // Iterate over a copy in case the collection is modified during damage application (e.g., entity deleted)
            foreach (var hitUid in beamComp.HitTargets.ToArray())
            {
                if (!_entityManager.EntityExists(hitUid) || hitUid == owner)
                    continue;

                _damageableSystem.TryChangeDamage(hitUid, damageToApply, ignoreResistances: false, origin: owner);
            }
        }

        private void OnUseInHand(EntityUid uid, ConstantLaserComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            if (component.IsFiring)
                StopFiring(uid, component);
            else
                StartFiring(uid, component);

            args.Handled = true;
        }

        private void OnDropped(EntityUid uid, ConstantLaserComponent component, DroppedEvent args)
        {
            StopFiring(uid, component);
        }

        private void OnDeselected(EntityUid uid, ConstantLaserComponent component, HandDeselectedEvent args)
        {
            StopFiring(uid, component);
        }
    }
}
