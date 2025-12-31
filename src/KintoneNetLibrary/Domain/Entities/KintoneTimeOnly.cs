using System.Globalization;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Extensions;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone の TIME フィールドを表現するクラス（時刻のみ）
/// </summary>
public class KintoneTimeOnly : IKintoneFieldConverter {
    public TimeOnly? Value { get; set; }
    public string? RawValue { get; set; }
    public KintoneFieldType FieldType { get; set; } = KintoneFieldType.Time;
    public bool HasValue => this.Value.HasValue;

    public KintoneTimeOnly() {
        this.Value = TimeOnly.MinValue;
    }

    public KintoneTimeOnly(TimeOnly? value) {
        this.Value = value?.TruncateToMinute();
    }

    public KintoneTimeOnly(string? value, KintoneFieldType fieldType) {
        this.FieldType = fieldType;
        this.RawValue = value;
        this.Value = TimeOnly.TryParse(value, out var to) ? to.TruncateToMinute() : null;
    }

    public override string ToString() {
        return this.Value?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    public static implicit operator KintoneTimeOnly(TimeOnly value) {
        return new KintoneTimeOnly(value);
    }

    public static implicit operator TimeOnly(KintoneTimeOnly kto) {
        if (kto.Value == null) {
            throw new InvalidOperationException("KintoneTimeOnly does not contain a value.");
        }
        return kto.Value.Value;
    }

    public static explicit operator KintoneTimeOnly(TimeSpan ts) {
        if (ts < TimeSpan.Zero || ts >= TimeSpan.FromHours(24)) {
            throw new ArgumentOutOfRangeException(nameof(ts), "Time must be between 00:00 and 23:59.");
        }

        return new KintoneTimeOnly(TimeOnly.FromTimeSpan(ts));
    }

    public object? ToJson() {
        return this.HasValue ? this.Value?.ToString("HH:mm", CultureInfo.InvariantCulture) : null;
    }

    public static KintoneTimeOnly Parse(string raw) {
        return new KintoneTimeOnly(TimeOnly.ParseExact(raw, "HH:mm", CultureInfo.InvariantCulture));
    }

    public static bool TryParse(string raw, out KintoneTimeOnly result) {
        if (TimeOnly.TryParseExact(raw, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)) {
            result = new KintoneTimeOnly(time);
            return true;
        }

        result = new KintoneTimeOnly();
        return false;
    }

}
