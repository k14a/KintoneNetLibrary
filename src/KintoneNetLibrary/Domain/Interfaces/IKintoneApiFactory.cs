using System.Text.Json;
using KintoneNetLibrary.Application.Interfaces;
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
    IKintoneApi Create<T>(T model) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// アクセス情報と アプリ Id から Kintone API インスタンスを作成します。
    /// </summary>
    /// <param name="access">Kintone アクセス情報</param>
    /// <param name="appId">アプリ Id</param>
    /// <param name="jsonOptions">JSON シリアライズオプション（省略時はデフォルト）</param>
    /// <returns>Kintone API インスタンス</returns>
    IKintoneApi Create(KintoneAccessBase access, int appId, JsonSerializerOptions? jsonOptions = null);
}
