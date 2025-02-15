﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Text;

namespace Snap.Hutao.Web.Hoyolab.Takumi.GameRecord.DailyNote;

/// <summary>
/// 参量质变仪恢复时间包装
/// 已准备完成 $后可再次使用
/// 冷却中     可使用
/// </summary>
[HighQuality]
internal sealed class RecoveryTime
{
    /// <summary>
    /// 日
    /// </summary>
    [JsonPropertyName("Day")]
    public int Day { get; set; }

    /// <summary>
    /// 时
    /// </summary>
    [JsonPropertyName("Hour")]
    public int Hour { get; set; }

    /// <summary>
    /// 分
    /// </summary>
    [JsonPropertyName("Minute")]
    public int Minute { get; set; }

    /// <summary>
    /// 秒
    /// </summary>
    [JsonPropertyName("Second")]
    public int Second { get; set; }

    /// <summary>
    /// 是否已经到达
    /// </summary>
    [JsonPropertyName("reached")]
    public bool Reached { get; set; }

    /// <summary>
    /// 剩余秒数
    /// </summary>
    [JsonIgnore]
    public int TotalSeconds
    {
        get => (60 * 60 * 24 * 7) - (Second + (60 * Minute) + (60 * 60 * Hour) + (60 * 60 * 24 * Day));
    }

    /// <summary>
    /// 获取格式化的剩余时间
    /// </summary>
    [JsonIgnore]
    public string TimeFormatted
    {
        get
        {
            if (Reached)
            {
                return SH.WebDailyNoteTransformerReady;
            }
            else
            {
                return new StringBuilder()
                    .AppendIf(Day > 0, string.Format(SH.WebDailyNoteTransformerDaysFormat, Day))
                    .AppendIf(Hour > 0, string.Format(SH.WebDailyNoteTransformerHoursFormat, Hour))
                    .AppendIf(Minute > 0, string.Format(SH.WebDailyNoteTransformerMinutesFormat, Minute))
                    .AppendIf(Second > 0, string.Format(SH.WebDailyNoteTransformerSecondsFormat, Second))
                    .Append(SH.WebDailyNoteTransformerAppend)
                    .ToString();
            }
        }
    }

    /// <summary>
    /// 获取格式化的状态
    /// </summary>
    [JsonIgnore]
    public string ReachedFormatted
    {
        get => Reached ? SH.WebDailyNoteTransformerReached : SH.WebDailyNoteTransformerNotReached;
    }
}