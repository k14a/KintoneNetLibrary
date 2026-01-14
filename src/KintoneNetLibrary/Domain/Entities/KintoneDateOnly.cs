using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone の DATE フィールドを表現するクラス（日付のみ）
/// </summary>
public class KintoneDateOnly : IKintoneFieldConverter {
    /// <summary>
    /// 日付の値を取得または設定します。
    /// </summary>
    public DateOnly? Value { get; set; }
    /// <summary>
    /// 生の値を取得または設定します。
    /// </summary>
    public string? RawValue { get; set; }
    /// <summary>
    /// フィールドの種類を取得または設定します。
    /// </summary>
    public KintoneFieldType FieldType { get; set; } = KintoneFieldType.Date;

    /// <summary>
    /// 値が存在するかどうかを示す値を取得します。
    /// </summary>
    public bool HasValue => this.Value.HasValue;

    /// <summary>
    /// KintoneDateOnly クラスの新しいインスタンスを初期化します。
    /// </summary>
    public KintoneDateOnly() { }

    /// <summary>
    /// 指定された日付の値で KintoneDateOnly クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="value"></param>
    public KintoneDateOnly(DateOnly? value) {
        this.Value = value;
    }

    /// <summary>
    /// 指定された生の値で KintoneDateOnly クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="value"></param>
    public KintoneDateOnly(string? value) {
        this.RawValue = value;
        this.Value = DateOnly.TryParse(value, out var d) ? d : null;
    }

    /// <summary>
    /// KintoneDateOnly の文字列形式を返します。
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return this.Value?.ToString("yyyy-MM-dd") ?? string.Empty;
    }

    /// <summary>
    /// KintoneDateOnly を JSON シリアライズ可能な形式に変換します。
    /// </summary>
    /// <returns></returns>
    public object? ToJson() {
        return this.HasValue ? this.Value?.ToString("yyyy-MM-dd") : null;
    }

    /// <summary>
    /// 指定された文字列を KintoneDateOnly に変換します。
    /// </summary>
    /// <param name="raw"></param>
    /// <returns></returns>
    public static KintoneDateOnly Parse(string raw) {
        return new KintoneDateOnly(DateOnly.ParseExact(raw, "yyyy-MM-dd"));
    }

    /// <summary>
    /// 指定された文字列を KintoneDateOnly に変換できるかどうかを示します。
    /// </summary>
    /// <param name="raw"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TryParse(string raw, out KintoneDateOnly result) {
        if (DateOnly.TryParseExact(raw, "yyyy-MM-dd", out var d)) {
            result = new KintoneDateOnly(d);
            return true;
        }

        result = new KintoneDateOnly();
        return false;
    }

    /// <summary>
    /// DateOnly 型から KintoneDateOnly 型への暗黙の変換を定義します。
    /// </summary>
    /// <param name="value"></param>
    public static implicit operator KintoneDateOnly(DateOnly value) => new(value);

    /// <summary>
    /// KintoneDateOnly 型から DateOnly 型への暗黙の変換を定義します。
    /// </summary>
    /// <param name="kd"></param>
    public static implicit operator DateOnly?(KintoneDateOnly kd) => kd.Value;
}
