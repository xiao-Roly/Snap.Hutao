﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Model.Intrinsic.Immutable;

/// <summary>
/// 本地化的不可变的原生枚举
/// </summary>
[HighQuality]
internal sealed class LocalizedIntrinsicImmutables
{
    private readonly ImmutableHashSet<string> associationTypes = Enum.GetValues<AssociationType>().Select(e => e.GetLocalizedDescriptionOrDefault()).OfType<string>().ToImmutableHashSet();
    private readonly ImmutableHashSet<string> weaponTypes = Enum.GetValues<WeaponType>().Select(e => e.GetLocalizedDescriptionOrDefault()).OfType<string>().ToImmutableHashSet();
    private readonly ImmutableHashSet<string> itemQualities = Enum.GetValues<ItemQuality>().Select(e => e.GetLocalizedDescriptionOrDefault()).OfType<string>().ToImmutableHashSet();
    private readonly ImmutableHashSet<string> bodyTypes = Enum.GetValues<BodyType>().Select(e => e.GetLocalizedDescriptionOrDefault()).OfType<string>().ToImmutableHashSet();
    private readonly ImmutableHashSet<string> fightProperties = Enum.GetValues<FightProperty>().Select(e => e.GetLocalizedDescriptionOrDefault()).OfType<string>().ToImmutableHashSet();
    private readonly ImmutableHashSet<string> elementNames = new HashSet<string>(7)
    {
        SH.ModelIntrinsicElementNameFire,
        SH.ModelIntrinsicElementNameWater,
        SH.ModelIntrinsicElementNameGrass,
        SH.ModelIntrinsicElementNameElec,
        SH.ModelIntrinsicElementNameWind,
        SH.ModelIntrinsicElementNameIce,
        SH.ModelIntrinsicElementNameRock,
    }.ToImmutableHashSet();

    /// <summary>
    /// 所属地区
    /// </summary>
    public ImmutableHashSet<string> AssociationTypes { get => associationTypes; }

    /// <summary>
    /// 武器类型
    /// </summary>
    public ImmutableHashSet<string> WeaponTypes { get => weaponTypes; }

    /// <summary>
    /// 物品类型
    /// </summary>
    public ImmutableHashSet<string> ItemQualities { get => itemQualities; }

    /// <summary>
    /// 身材类型
    /// </summary>
    public ImmutableHashSet<string> BodyTypes { get => bodyTypes; }

    /// <summary>
    /// 战斗属性
    /// </summary>
    public ImmutableHashSet<string> FightProperties { get => fightProperties; }

    /// <summary>
    /// 元素名称
    /// </summary>
    public ImmutableHashSet<string> ElementNames { get => elementNames; }
}