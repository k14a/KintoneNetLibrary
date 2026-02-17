using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Domain.Interfaces;

/// <summary>
/// Kintone API ファクトリ インターフェイス
/// </summary>
public interface IKintoneApiFactory {
    /// <summary>
    /// 指定されたモデルに基づいて Kintone API インスタンスを作成します。
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <returns>Kintone API インスタンス</returns>
    KintoneApi Create<T>(T model) where T : KintoneModelBase<T>, new();
}
