namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone の TIME フィールドを表現するクラス（時刻のみ）
/// </summary>
public class KintoneTimeOnly {
    public TimeOnly Value { get; set; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public KintoneTimeOnly() {
        this.Value = TimeOnly.MinValue;
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="value"></param>
    public KintoneTimeOnly(TimeOnly value) {
        this.Value = value;
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
}
