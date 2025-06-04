using System.Globalization;

namespace KintoneNetLibrary.Domain.Entities;

public class KintoneDateTime : IKintoneFieldConverter {
    /// <summary>
    /// 日付データ
    /// </summary>
    public DateTime Value { get; set; }
    public KintoneFieldType Type { get; set; } = KintoneFieldType.DateTime;
    public string? RawValue { get; set; } = string.Empty;
    public DateOnly DateOnly => DateOnly.FromDateTime(this.Value);
    public TimeOnly TimeOnly => TimeOnly.FromDateTime(this.Value);

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public KintoneDateTime() {
        this.Value = DateTime.MinValue;
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="value"></param>
    public KintoneDateTime(DateTime value) {
        this.Value = value;
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="value"></param>
    public KintoneDateTime(DateOnly value) {
        this.Value = value.ToDateTime(TimeOnly.MinValue);
        this.Type = KintoneFieldType.Date;
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="kintoneStringValue"></param>
    /// <param name="type"></param>
    public KintoneDateTime(string? kintoneStringValue, KintoneFieldType type) {
        this.Type = type;
        this.RawValue = kintoneStringValue;
        this.Value = DateTime.TryParse(kintoneStringValue, out var dt) ? dt : DateTime.MinValue;
    }


    public override string? ToString() {
        return this.ToString(KintoneFieldType.DateTime);
    }
    public string? ToString(KintoneFieldType type) {
        return type switch {
            KintoneFieldType.Date => this.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            KintoneFieldType.DateTime => this.Value.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException("Unknown KintoneFieldType"),
        };
    }

    /// <summary>
    /// 時刻部分だけを KintoneTimeOnly として取得
    /// </summary>
    public KintoneTimeOnly ToKintoneTimeOnly() {
        return new KintoneTimeOnly(TimeOnly.FromDateTime(this.Value));
    }

    /// <summary>
    /// 日付（yyyy-MM-dd）＋ KintoneTimeOnly から新しい KintoneDateTime を構築
    /// </summary>
    public static KintoneDateTime Combine(DateOnly date, KintoneTimeOnly time) {
        var dt = date.ToDateTime(time.Value);
        return new KintoneDateTime(dt);
    }

    public object? ToJson() {
        return Type switch {
            KintoneFieldType.Date => Value.ToString("yyyy-MM-dd"),
            KintoneFieldType.DateTime => Value.ToString("yyyy-MM-ddTHH:mm:ssZ"), // UTC対応など必要なら調整
            _ => throw new InvalidOperationException("Invalid KintoneDateTimeType.")
        };
    }

    public static KintoneDateTime Parse(string raw, KintoneFieldType type) {
        return type switch {
            KintoneFieldType.Date => new KintoneDateTime(DateOnly.ParseExact(raw, "yyyy-MM-dd")),
            KintoneFieldType.DateTime => new KintoneDateTime(DateTime.Parse(raw, null, DateTimeStyles.RoundtripKind)),
            _ => throw new ArgumentException("Invalid type.")
        };
    }

}
