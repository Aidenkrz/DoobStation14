// SPDX-FileCopyrightText: 2024 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2024 Kira Bridgeton <161087999+Verbalase@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Armok <155400926+ARMOKS@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 SaffronFennec <firefoxwolf2020@protonmail.com>
// SPDX-FileCopyrightText: 2025 Solstice <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._DV.CCVars;

/// <summary>
/// DeltaV specific cvars.
/// </summary>
[CVarDefs]
// ReSharper disable once InconsistentNaming - Shush you
public sealed class DCCVars
{
    /// <summary>
    /// Disables all vision filters for species like Vulpkanin or Harpies. There are good reasons someone might want to disable these.
    /// </summary>
    public static readonly CVarDef<bool> NoVisionFilters =
        CVarDef.Create("accessibility.no_vision_filters", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Cosmic Cult
     */

    /// <summary>
    /// The multiplier for the difficulty of the monument.
    /// </summary>
    public static readonly CVarDef<float> CosmicCultistDifficultyMultiplier =
        CVarDef.Create("cosmiccult.difficulty_multiplier", 1.5f, CVar.SERVER);

    /// <summary>
    /// How much entropy a convert is worth towards the next monument tier.
    /// </summary>
    public static readonly CVarDef<int> CosmicCultistEntropyValue =
        CVarDef.Create("cosmiccult.cultist_entropy_value", 5, CVar.SERVER);

    /// <summary>
    /// How much of the crew the cult is aiming to convert for a tier 3 monument.
    /// </summary>
    public static readonly CVarDef<int> CosmicCultTargetConversionPercent =
        CVarDef.Create("cosmiccult.target_conversion_percent", 40, CVar.SERVER);

    /// <summary>
    /// How long the timer for the cult's stewardship vote lasts.
    /// </summary>
    public static readonly CVarDef<int> CosmicCultStewardVoteTimer =
        CVarDef.Create("cosmiccult.steward_vote_timer", 120, CVar.SERVER);

    /// <summary>
    /// The delay between the monument getting upgraded to tier 2 and the crew learning of that fact. the monument cannot be upgraded again in this time.
    /// </summary>
    public static readonly CVarDef<int> CosmicCultT2RevealDelaySeconds =
        CVarDef.Create("cosmiccult.t2_reveal_delay_seconds", 120, CVar.SERVER);

    /// <summary>
    /// The delay between the monument getting upgraded to tier 3 and the crew learning of that fact. the monument cannot be upgraded again in this time.
    /// </summary>
    public static readonly CVarDef<int> CosmicCultT3RevealDelaySeconds =
        CVarDef.Create("cosmiccult.t3_reveal_delay_seconds", 60, CVar.SERVER);

    /// <summary>
    /// The delay between the monument getting upgraded to tier 3 and the finale starting.
    /// </summary>
    public static readonly CVarDef<int> CosmicCultFinaleDelaySeconds =
        CVarDef.Create("cosmiccult.extra_entropy_for_finale", 150, CVar.SERVER);
}
