using System.Net.Http;
using System.Threading.Tasks;
using KintoneNetLibrary.Api;
using KintoneNetLibrary.Types;
using KintoneNetLibrary.Extensions;

namespace KintoneNetLibrary.Model;

public abstract partial class KintoneModelBase {
    /* =========================================================
       CRUD（非同期）
       ========================================================= */

    public async Task<KintoneIndex> CreateAsync() {
        var api = BuildApi();
        var indexes = await api.CreateAsync(new List<KintoneModelBase> { this });
        return ApplyIndex(indexes, 0);
    }

    public async Task<KintoneIndex> UpdateAsync() {
        var api = BuildApi();
        var indexes = await api.UpdateAsync(new List<KintoneModelBase> { this });
        return ApplyIndex(indexes, 0);
    }

    public async Task<KintoneIndex> SaveAsync() {
        var api = BuildApi();
        var indexes = await api.SaveAsync(new List<KintoneModelBase> { this });
        return ApplyIndex(indexes, 0);
    }

    public async Task<IList<string>> DeleteAsync() {
        if (string.IsNullOrEmpty(this.RecordID)) {
            // レコード ID が無い場合はキー検索で取得してみる
            await RefreshIdFromKeyAsync();
        }

        if (string.IsNullOrEmpty(this.RecordID)) {
            return []; // 既に削除済み
        }

        var api = BuildApi();
        var result = await api.DeleteAsync(this.AppID, [this.RecordID]);

        /*     if (ok) {
                this.RecordID = string.Empty;
                this.Revision = -1;
            }
      */
        return result;
    }

    /* ---------- 内部ユーティリティ ---------- */

    /// <summary>
    /// Domain / ApiToken 設定を反映した KintoneApi インスタンスを生成。
    /// </summary>
    private KintoneApi BuildApi() {
        // ※ DI を導入する場合はここを差し替え
        var httpClient = new HttpClient {
            BaseAddress = string.IsNullOrWhiteSpace(Domain)
                ? null
                : new Uri($"https://{Domain.TrimEnd('/')}/k/v1/")
        };
        var api = new KintoneApi(httpClient, ApiToken);
        return api;
    }

    /// <summary>
    /// Save / Create / Update の結果 (KintoneIndexes) をモデルへ反映し、単一結果を返す。
    /// </summary>
    private KintoneIndex ApplyIndex(KintoneIndexes indexes, int index = 0) {
        if (indexes.IDs.Count > index && indexes.Revisions.Count > index) {
            this.RecordID = indexes.IDs[index];
            // this.Revision = Convert.ToInt32(indexes.Revisions[index]);
            this.Revision = Convert.ToInt32(indexes.Revisions[index]);

            return new KintoneIndex {
                ID = RecordID,
                Revision = this.Revision,
            };
        }
        return new KintoneIndex();
    }

    /// <summary>
    /// キー項目から再検索して RecordID を取得（必要に応じて派生クラスで実装）。
    /// </summary>
    protected virtual Task RefreshIdFromKeyAsync() {
        // ここではデフォルト実装を空にしておき、
        // 派生クラスがキー情報を持つ場合は override で ID を埋める。
        return Task.CompletedTask;
    }
}
