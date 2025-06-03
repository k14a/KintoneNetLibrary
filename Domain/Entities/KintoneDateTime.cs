namespace KintoneNetLibrary.Domain.Entities;

public class KintoneDateTime {
    // public enum DateTimeType {
    //     /// <summary>
    //     /// 日時型
    //     /// </summary>
    //     DateTime,
    //     /// <summary>
    //     /// 時刻型
    //     /// </summary>
    //     Time,
    //     /// <summary>
    //     /// 日付型
    //     /// </summary>
    //     Ymd,
    // }

    /// <summary>
    /// 日付データ
    /// </summary>
    public DateTime Value { get; set; }

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

    public override string ToString() {
        return this.ToString(KintoneDateTimeType.DateTime);
    }
    public string ToString(KintoneDateTimeType type) {
        return type switch {
            KintoneDateTimeType.DateOnly => this.Value.ToString("yyyy-MM-dd"),
            KintoneDateTimeType.DateTime => this.Value.ToString("yyyy-MM-ddTHH:mm"),
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
