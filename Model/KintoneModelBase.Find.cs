using KintoneNetLibrary.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KintoneNetLibrary.Model;

public abstract partial class KintoneModelBase
{
    /* =========================================================
       Find – 静的検索メソッド
       ========================================================= */

    /// <summary>
    /// Kintone クエリ文字列からレコードを検索（非同期）。
    /// </summary>
    public static async Task<IList<T>> FindByQueryAsync<T>(string query) where T : KintoneModelBase, new() {
        var api = CreateApiInstance<T>();
        return await api.FindByQueryAsync<T>(query);
    }

    /// <summary>
    /// 単一レコード ID で検索（非同期）。該当なしは null。
    /// </summary>
    public static async Task<T?> FindByIDAsync<T>(string id) where T : KintoneModelBase, new() {
        var api = CreateApiInstance<T>();
        return await api.FindByIDAsync<T>(id);
    }

    /// <summary>
    /// 複数レコード ID で検索（非同期）。
    /// </summary>
    public static async Task<IList<T>> FindByIDsAsync<T>(IList<string> ids) where T : KintoneModelBase, new() {
        var api = CreateApiInstance<T>();
        return await api.FindByIDsAsync<T>(ids);
    }

    /// <summary>
    /// フィールド = 値 で検索（非同期）。
    /// </summary>
    public static async Task<IList<T>> FindByFieldAsync<T>(string field, string value)
        where T : KintoneModelBase, new() {
        var api = CreateApiInstance<T>();
        return await api.FindByFieldAsync<T>(field, value);
    }

    /// <summary>
    /// 全件取得（非同期）。大量件数の場合は Api 内部でカーソルを使用。
    /// </summary>
    public static async Task<IList<T>> FindAllAsync<T>() where T : KintoneModelBase, new() {
        var api = CreateApiInstance<T>();
        return await api.FindAllAsync<T>();
    }

    /* ----------  内部ユーティリティ  ---------- */

    /// <summary>
    /// モデル T から KintoneApi インスタンスを生成。
    /// Domain / ApiToken など共有設定を派生型が Override で渡す運用も可能。
    /// </summary>
    private static KintoneApi CreateApiInstance<T>() where T : KintoneModelBase, new() {
        // ここでは簡易に new していますが、DI で共有 HttpClient を渡す設計にも拡張できる
        var modelSample = new T();
        var api = new KintoneApi(new HttpClient(), modelSample.ApiToken);
        api.SetDomain(modelSample.Domain);   // Domain / Token をモデル側プロパティから流用
        return api;
    }
}
