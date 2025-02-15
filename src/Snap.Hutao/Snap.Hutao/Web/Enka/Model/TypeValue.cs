﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Model.Intrinsic;

namespace Snap.Hutao.Web.Enka.Model;

/// <summary>
/// 类型与值
/// </summary>
[HighQuality]
internal sealed class TypeValue
{
    /// <summary>
    /// 构造一个新的类型与值
    /// </summary>
    [JsonConstructor]
    public TypeValue()
    {
    }

    /// <summary>
    /// 构造一个新的类型与值
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="value">值</param>
    public TypeValue(PlayerProperty type, string? value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>
    /// 类型
    /// </summary>
    [JsonPropertyName("type")]
    public PlayerProperty Type { get; set; }

    /// <summary>
    /// 值
    /// </summary>
    [JsonPropertyName("val")]
    public string? Value { get; set; }

    /// <summary>
    /// 值 Int32
    /// </summary>
    [JsonIgnore]
    public int ValueInt32
    {
        get
        {
            _ = int.TryParse(Value, out int result);
            return result;
        }
    }
}