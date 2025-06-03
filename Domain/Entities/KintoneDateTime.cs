namespace KintoneNetLibrary.Domain.Entities;

public class KintoneDateTime {
    /// <summary>
    /// 日付データ
    /// </summary>
    public DateTime Value { get; set; }
    public KintoneFieldType Type { get; set; } = KintoneFieldType.Unknown;
    public string? RawValue { get; set; } = string.Empty;

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
            KintoneFieldType.Date => this.Value.ToString("yyyy-MM-dd"),
            KintoneFieldType.DateTime => this.Value.ToString("yyyy-MM-ddTHH:mm"),
            _ => this.Value.ToString("yyyy-MM-ddTHH:mm"),
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

}
