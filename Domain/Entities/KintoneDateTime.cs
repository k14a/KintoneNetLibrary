using System.Globalization;

namespace KintoneNetLibrary.Domain.Entities;

public class KintoneDateTime : IKintoneFieldConverter {
    public DateTime Value { get; set; }
    public KintoneFieldType FieldType { get; set; } = KintoneFieldType.DateTime;
    public string? RawValue { get; set; } = string.Empty;
    public DateOnly DateOnly => DateOnly.FromDateTime(this.Value);
    public TimeOnly TimeOnly => TimeOnly.FromDateTime(this.Value);
    public bool HasValue => this.Value != DateTime.MinValue;

    public KintoneDateTime() {
        this.Value = DateTime.MinValue;
    }

    public KintoneDateTime(DateTime value) {
        this.Value = value;
    }

    public KintoneDateTime(DateOnly value) {
        this.Value = value.ToDateTime(TimeOnly.MinValue);
        this.FieldType = KintoneFieldType.Date;
    }

    public KintoneDateTime(string? kintoneStringValue, KintoneFieldType fieldType) {
        this.FieldType = fieldType;
        this.RawValue = kintoneStringValue;

        if (string.IsNullOrWhiteSpace(kintoneStringValue)) {
            this.Value = DateTime.MinValue;
            return;
        }

        this.Value = DateTime.TryParse(kintoneStringValue, out var dt) ? dt : DateTime.MinValue;
    }

    public override string? ToString() {
        return ToFormattedString(this.FieldType, forJson: false);
    }

    public string? ToString(KintoneFieldType type) {
        return ToFormattedString(type, forJson: false);
    }

    private string ToFormattedString(KintoneFieldType type, bool forJson) {
        return type switch {
            KintoneFieldType.Date => this.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            KintoneFieldType.DateTime => forJson
                ? this.Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
                : this.Value.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException("Unknown KintoneFieldType")
        };
    }

    public KintoneTimeOnly ToKintoneTimeOnly() {
        return new KintoneTimeOnly(TimeOnly.FromDateTime(this.Value));
    }

    public static KintoneDateTime Combine(DateOnly date, KintoneTimeOnly time) {
        if (!time.Value.HasValue) {
            throw new InvalidOperationException("KintoneTimeOnly.Value is null.");
        }

        var dt = date.ToDateTime(time.Value.Value);
        return new KintoneDateTime(dt);
    }

    public object? ToJson() {
        return this.HasValue ? ToFormattedString(this.FieldType, forJson: true) : null;
    }

    public object? ToJson(KintoneFieldType overrideType) {
        return this.HasValue ? ToFormattedString(overrideType, forJson: true) : null;
    }

    public static KintoneDateTime Parse(string raw, KintoneFieldType fieldType) {
        return fieldType switch {
            KintoneFieldType.Date => new KintoneDateTime(DateOnly.ParseExact(raw, "yyyy-MM-dd")),
            KintoneFieldType.DateTime => new KintoneDateTime(DateTime.Parse(raw, null, DateTimeStyles.RoundtripKind)),
            _ => throw new ArgumentException("Invalid type.")
        };
    }

    public static bool TryParse(string raw, KintoneFieldType fieldType, out KintoneDateTime result) {
        try {
            result = Parse(raw, fieldType);
            return true;
        } catch {
            result = new KintoneDateTime();
            return false;
        }
    }
    // DateTime → KintoneDateTime
    public static implicit operator KintoneDateTime(DateTime value) {
        return new KintoneDateTime(value);
    }

    // KintoneDateTime → DateTime
    public static implicit operator DateTime(KintoneDateTime kdt) {
        return kdt?.Value ?? default;
    }

}
