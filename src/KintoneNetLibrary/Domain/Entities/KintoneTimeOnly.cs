using System.Globalization;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Extensions;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone の TIME フィールドを表現するクラス（時刻のみ）
/// </summary>
public class KintoneTimeOnly : IKintoneFieldConverter {
    /// <summary>
    /// 時刻の値を取得または設定します。
    /// </summary>
    public TimeOnly? Value { get; set; }

    /// <summary>
    /// 生の値を取得または設定します。
    /// </summary>
    public string? RawValue { get; set; }

    /// <summary>
    /// フィールドの種類を取得または設定します。
    /// </summary>
    public KintoneFieldType FieldType { get; set; } = KintoneFieldType.Time;

    /// <summary>
    /// 値が存在するかどうかを示す値を取得します。
    /// </summary>
    public bool HasValue => this.Value.HasValue;

    /// <summary>
    /// KintoneTimeOnly クラスの新しいインスタンスを初期化します。
    /// </summary>
    public KintoneTimeOnly() {
        this.Value = TimeOnly.MinValue;
    }

    /// <summary>
    /// 指定された時刻の値で KintoneTimeOnly クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="value">初期化する時刻の値。</param>
    public KintoneTimeOnly(TimeOnly? value) {
        this.Value = value?.TruncateToMinute();
    }

    /// <summary>
    /// 指定された生の値とフィールドの種類で KintoneTimeOnly クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="value">生の時刻の値。</param>
    /// <param name="fieldType">フィールドの種類。</param>
    public KintoneTimeOnly(string? value, KintoneFieldType fieldType) {
        this.FieldType = fieldType;
        this.RawValue = value;
        this.Value = TimeOnly.TryParse(value, out var to) ? to.TruncateToMinute() : null;
    }

    /// <summary>
    /// KintoneTimeOnly の文字列形式を返します。
    /// </summary>
    /// <returns>時刻の文字列（"HH:mm"形式）。</returns>
    public override string ToString() {
        return this.Value?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    /// <summary>
    /// TimeOnly 型から KintoneTimeOnly 型への暗黙の変換を定義します。
    /// </summary>
    /// <param name="value">変換する TimeOnly の値。</param>
    /// <returns>KintoneTimeOnly のインスタンス。</returns>
    public static implicit operator KintoneTimeOnly(TimeOnly value) {
        return new KintoneTimeOnly(value);
    }

    /// <summary>
    /// KintoneTimeOnly 型から TimeOnly 型への暗黙の変換を定義します。
    /// </summary>
    /// <param name="kto">変換する KintoneTimeOnly のインスタンス。</param>
    /// <returns>TimeOnly の値。</returns>
    /// <exception cref="InvalidOperationException">KintoneTimeOnly に値が含まれていない場合。</exception>
    public static implicit operator TimeOnly(KintoneTimeOnly kto) {
        if (kto.Value == null) {
            throw new InvalidOperationException("KintoneTimeOnly does not contain a value.");
        }
        return kto.Value.Value;
    }

    /// <summary>
    /// TimeSpan 型から KintoneTimeOnly 型への明示的な変換を定義します。
    /// </summary>
    /// <param name="ts">変換する TimeSpan の値。</param>
    /// <returns>KintoneTimeOnly のインスタンス。</returns>
    /// <exception cref="ArgumentOutOfRangeException">TimeSpan が 00:00 から 23:59 の範囲外の場合。</exception>
    public static explicit operator KintoneTimeOnly(TimeSpan ts) {
        if (ts < TimeSpan.Zero || ts >= TimeSpan.FromHours(24)) {
            throw new ArgumentOutOfRangeException(nameof(ts), "Time must be between 00:00 and 23:59.");
        }

        return new KintoneTimeOnly(TimeOnly.FromTimeSpan(ts));
    }

    /// <summary>
    /// KintoneTimeOnly を JSON 形式に変換します。
    /// </summary>
    /// <returns>時刻の文字列（"HH:mm"形式）または null。</returns>
    public object? ToJson() {
        return this.HasValue ? this.Value?.ToString("HH:mm", CultureInfo.InvariantCulture) : null;
    }

    /// <summary>
    /// 生の時刻の文字列を解析して KintoneTimeOnly のインスタンスを返します。
    /// </summary>
    /// <param name="raw">解析する生の時刻の文字列。</param>
    /// <returns>KintoneTimeOnly のインスタンス。</returns>
    public static KintoneTimeOnly Parse(string raw) {
        return new KintoneTimeOnly(TimeOnly.ParseExact(raw, "HH:mm", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// 生の時刻の文字列を解析して KintoneTimeOnly のインスタンスを返すか、成功したかどうかを示します。
    /// </summary>
    /// <param name="raw">解析する生の時刻の文字列。</param>
    /// <param name="result">解析結果の KintoneTimeOnly のインスタンス。</param>
    /// <returns>解析が成功した場合は true、それ以外は false。</returns>
    public static bool TryParse(string raw, out KintoneTimeOnly result) {
        if (TimeOnly.TryParseExact(raw, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)) {
            result = new KintoneTimeOnly(time);
            return true;
        }

        result = new KintoneTimeOnly();
        return false;
    }
}
