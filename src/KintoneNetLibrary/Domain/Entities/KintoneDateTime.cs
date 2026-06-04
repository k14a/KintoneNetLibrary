using System.Globalization;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone 日時フィールドを表すクラス
/// </summary>
public class KintoneDateTime : IKintoneFieldConverter {
    /// <summary>
    /// Kintone 日時の値
    /// </summary>
    public DateTime Value { get; set; }

    /// <summary>
    /// Kintone フィールドタイプ
    /// </summary>
    public KintoneFieldType FieldType { get; set; } = KintoneFieldType.DateTime;

    /// <summary>
    /// 生の値（Kintoneからの文字列）
    /// </summary>
    public string? RawValue { get; set; } = string.Empty;

    /// <summary>
    /// 日付部分を取得
    /// </summary>
    public DateOnly DateOnly => DateOnly.FromDateTime(this.Value);

    /// <summary>
    /// 時間部分を取得
    /// </summary>
    public TimeOnly TimeOnly => TimeOnly.FromDateTime(this.Value);

    /// <summary>
    /// KintoneDateTimeが値を持っているかどうか
    /// </summary>
    /// <returns>値が設定されている場合はtrue、それ以外はfalse</returns>
    public bool HasValue => this.Value != DateTime.MinValue;

    /// <summary>
    /// KintoneDateTimeのコンストラクタ
    /// </summary>
    public KintoneDateTime() {
        this.Value = DateTime.MinValue;
    }

    /// <summary>
    /// KintoneDateTimeのコンストラクタ
    /// </summary>
    /// <param name="value">日時の値</param>
    public KintoneDateTime(DateTime value) {
        this.Value = value;
    }

    /// <summary>
    /// KintoneDateTimeのコンストラクタ
    /// </summary>
    /// <param name="value">日付の値</param>
    public KintoneDateTime(DateOnly value) {
        this.Value = value.ToDateTime(TimeOnly.MinValue);
        this.FieldType = KintoneFieldType.Date;
    }

    /// <summary>
    /// KintoneDateTimeのコンストラクタ
    /// </summary>
    /// <param name="kintoneStringValue">Kintoneからの文字列値</param>
    /// <param name="fieldType">Kintoneフィールドタイプ</param>
    public KintoneDateTime(string? kintoneStringValue, KintoneFieldType fieldType) {
        this.FieldType = fieldType;
        this.RawValue = kintoneStringValue;

        if (string.IsNullOrWhiteSpace(kintoneStringValue)) {
            this.Value = DateTime.MinValue;
            return;
        }

        this.Value = DateTime.TryParse(kintoneStringValue, out var dt) ? dt : DateTime.MinValue;
    }

    /// <summary>
    /// KintoneDateTimeの文字列表現を取得
    /// </summary>
    /// <returns>フォーマットされた文字列</returns>
    public override string? ToString() {
        return this.ToFormattedString(this.FieldType, forJson: false);
    }

    /// <summary>
    /// KintoneDateTimeの文字列表現を取得
    /// </summary>
    /// <param name="type">Kintoneフィールドタイプ</param>
    /// <returns>フォーマットされた文字列</returns>
    public string? ToString(KintoneFieldType type) {
        return this.ToFormattedString(type, forJson: false);
    }

    /// <summary>
    /// KintoneDateTimeの文字列表現を取得
    /// </summary>
    /// <param name="type">Kintoneフィールドタイプ</param>
    /// <param name="forJson">JSON用のフォーマットかどうか</param>
    /// <returns>フォーマットされた文字列</returns>
    /// <exception cref="InvalidOperationException">不明なKintoneFieldTypeの場合</exception>
    private string ToFormattedString(KintoneFieldType type, bool forJson) {
        return type switch {
            KintoneFieldType.Date => this.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            KintoneFieldType.DateTime => forJson
                ? this.Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
                : this.Value.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException("Unknown KintoneFieldType")
        };
    }

    /// <summary>
    /// KintoneDateTimeをKintoneTimeOnlyに変換  
    /// </summary>
    /// <returns>KintoneTimeOnly</returns>
    public KintoneTimeOnly ToKintoneTimeOnly() {
        return new KintoneTimeOnly(TimeOnly.FromDateTime(this.Value));
    }

    /// <summary>
    /// KintoneDateTimeをKintoneDateOnlyに変換
    /// </summary>
    /// <param name="date">DateOnly</param>
    /// <param name="time">KintoneTimeOnly</param>
    /// <returns>KintoneDateOnly</returns>
    /// <exception cref="InvalidOperationException">KintoneTimeOnlyの値がnullの場合</exception>
    public static KintoneDateTime Combine(DateOnly date, KintoneTimeOnly time) {
        if (!time.Value.HasValue) {
            throw new InvalidOperationException("KintoneTimeOnly.Value is null.");
        }

        var dt = date.ToDateTime(time.Value.Value);
        return new KintoneDateTime(dt);
    }

    /// <summary>
    /// KintoneDateTimeをJSON形式に変換
    /// </summary>
    /// <returns>JSON形式のオブジェクト</returns>
    /// <exception cref="InvalidOperationException">KintoneDateTimeに値が設定されていない場合</exception>
    /// <exception cref="ArgumentException">無効なKintoneFieldTypeの場合</exception>
    public object? ToJson() {
        return this.HasValue ? this.ToFormattedString(this.FieldType, forJson: true) : null;
    }

    /// <summary>
    /// KintoneDateTimeをJSON形式に変換
    /// </summary>
    /// <param name="overrideType">オーバーライドするKintoneフィールドタイプ</param>
    /// <returns>JSON形式のオブジェクト</returns>
    /// <exception cref="InvalidOperationException">KintoneDateTimeに値が設定されていない場合</exception>
    /// <exception cref="ArgumentException">無効なKintoneFieldTypeの場合</exception>
    public object? ToJson(KintoneFieldType overrideType) {
        return this.HasValue ? this.ToFormattedString(overrideType, forJson: true) : null;
    }

    /// <summary>
    /// KintoneDateTimeをパースして新しいインスタンスを生成
    /// </summary>
    /// <param name="raw">生の文字列値</param>
    /// <param name="fieldType">Kintoneフィールドタイプ</param>
    /// <returns>KintoneDateTimeの新しいインスタンス</returns>
    /// <exception cref="ArgumentException">無効なKintoneFieldTypeの場合</exception>
    /// <exception cref="FormatException">日付のフォーマットが不正な場合</exception>
    /// <exception cref="ArgumentNullException">rawがnullの場合</exception>
    public static KintoneDateTime Parse(string raw, KintoneFieldType fieldType) {
        return fieldType switch {
            KintoneFieldType.Date => new KintoneDateTime(DateOnly.ParseExact(raw, "yyyy-MM-dd")),
            KintoneFieldType.DateTime => new KintoneDateTime(DateTime.Parse(raw, null, DateTimeStyles.RoundtripKind)),
            _ => throw new ArgumentException("Invalid type.")
        };
    }

    /// <summary>
    /// KintoneDateTimeをパースして新しいインスタンスを生成
    /// </summary>
    /// <param name="raw">生の文字列値</param>
    /// <param name="fieldType">Kintoneフィールドタイプ</param>
    /// <param name="result">パース成功時に設定されるKintoneDateTimeインスタンス</param>
    /// <returns>パースに成功した場合はtrue、失敗した場合はfalse</returns>
    /// <exception cref="ArgumentException">無効なKintoneFieldTypeの場合</exception>
    /// <exception cref="ArgumentNullException">rawがnullの場合</exception>
    public static bool TryParse(string raw, KintoneFieldType fieldType, out KintoneDateTime result) {
        try {
            result = Parse(raw, fieldType);
            return true;
        } catch {
            result = new KintoneDateTime();
            return false;
        }
    }

    /// <summary>
    /// KintoneDateTimeをDateTimeに変換
    /// </summary>
    /// <param name="value">日時の値</param>
    /// <returns>KintoneDateTimeの新しいインスタンス</returns>
    /// <exception cref="ArgumentNullException">valueがnullの場合</exception>
    public static implicit operator KintoneDateTime(DateTime value) {
        return new KintoneDateTime(value);
    }

    /// <summary>
    /// KintoneDateTimeをDateTimeに変換
    /// </summary>
    /// <param name="kdt">KintoneDateTimeのインスタンス</param>
    /// <returns>DateTimeの値</returns>
    /// <exception cref="ArgumentNullException">kdtがnullの場合</exception>
    /// <exception cref="InvalidOperationException">KintoneDateTimeに値が設定されていない場合</exception>
    public static implicit operator DateTime(KintoneDateTime kdt) {
        return kdt?.Value ?? default;
    }
}
