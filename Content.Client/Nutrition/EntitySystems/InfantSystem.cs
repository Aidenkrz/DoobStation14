// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Nutrition.AnimalHusbandry;
using Robust.Client.GameObjects;

namespace Content.Client.Nutrition.EntitySystems;

/// <summary>
/// This handles visuals for <see cref="InfantComponent"/>
/// </summary>
public sealed class InfantSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<InfantComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<InfantComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, InfantComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        component.DefaultScale = sprite.Scale;
        _sprite.SetScale((uid, sprite), component.VisualScale);
    }

    private void OnShutdown(EntityUid uid, InfantComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.SetScale((uid, sprite), component.DefaultScale);
    }
}