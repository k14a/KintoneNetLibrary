using System.Globalization;
using KintoneNetLibrary.Extensions;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone の TIME フィールドを表現するクラス（時刻のみ）
/// </summary>
public class KintoneTimeOnly : IKintoneFieldConverter {
    public TimeOnly? Value { get; set; }
    public string? RawValue { get; set; }
    public KintoneFieldType Type { get; set; } = KintoneFieldType.Time;
    public bool HasValue => this.Value.HasValue;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public KintoneTimeOnly() {
        this.Value = TimeOnly.MinValue;
    }

    /// <summary>
    /// コンストラクタ（null許容）
    /// </summary>
    /// <param name="value">TimeOnly 値。null の場合は null として扱う</param>
    public KintoneTimeOnly(TimeOnly? value) {
        this.Value = value?.TruncateToMinute();
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="value"></param>
    /// <param name="type"></param>
    public KintoneTimeOnly(string? value, KintoneFieldType type) {
        this.Type = type;
        this.RawValue = value;
        this.Value = TimeOnly.TryParse(value, out var to) ? to.TruncateToMinute() : TimeOnly.MinValue;
    }

    /// <summary>
    /// "HH:mm" 形式で Kintone への文字列化出力
    /// </summary>
    public override string ToString() {
        return this.Value.ToString("HH:mm");
    }

    /// <summary>
    /// TimeOnly からの暗黙的変換
    /// </summary>
    public static implicit operator KintoneTimeOnly(TimeOnly value) {
        return new KintoneTimeOnly(value);
    }

    /// <summary>
    /// KintoneTimeOnly → TimeOnly の暗黙的変換
    /// </summary>
    public static implicit operator TimeOnly(KintoneTimeOnly kto) {
        return kto.Value;
    }

    /// <summary>
    /// TimeSpan からの明示的変換（00:00～23:59 のみ許容）
    /// </summary>
    public static explicit operator KintoneTimeOnly(TimeSpan ts) {
        if (ts < TimeSpan.Zero || ts >= TimeSpan.FromHours(24)) {
            throw new ArgumentOutOfRangeException(nameof(ts), "Time must be between 00:00 and 23:59.");
        }

        return new KintoneTimeOnly(TimeOnly.FromTimeSpan(ts));
    }

    public object? ToJson() {
        return this.Value.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

}
