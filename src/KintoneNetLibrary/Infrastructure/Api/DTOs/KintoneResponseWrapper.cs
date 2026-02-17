using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Api.DTOs;

/// <summary>
/// Kintoneのレスポンスをラップするクラス
/// </summary>
/// <typeparam name="T">Kintoneのレコードデータの型</typeparam>
public class KintoneResponseWrapper<T> where T : KintoneModelBase<T>, new() {
    /// <summary>
    /// Kintoneのレコードデータ
    /// </summary>
    public T Record { get; set; } = new T();
}

/// <summary>
/// Kintoneのレスポンスリストをラップするクラス
/// </summary>
/// <typeparam name="T">Kintoneのレコードデータの型</typeparam>
public class KintoneResponseListWrapper<T> where T : KintoneModelBase<T>, new() {
    /// <summary>
    /// Kintoneのレコードデータリスト
    /// </summary>
    public List<T> Records { get; set; } = [];
}