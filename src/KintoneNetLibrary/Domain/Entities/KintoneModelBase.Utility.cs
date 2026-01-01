using System.Reflection;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneのレコードモデルの基底クラス
/// </summary>
public abstract partial class KintoneModelBase<TSelf> : KintoneModelHookBase where TSelf : KintoneModelBase<TSelf>, new() {
    // ----- 判定・ユーティリティ -----
    /// <summary>
    /// 更新キーまたはIDが設定されているかどうかを判定します。
    /// </summary>
    /// <returns></returns>
    public virtual bool HasUpdateKeyOrID() {
        if (!string.IsNullOrEmpty(this.ID)) {
            return true;
        }

        return this.GetType().GetProperties()
            .Where(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true)
            .Any(p => p.GetValue(this) is string s ? !string.IsNullOrEmpty(s) : p.GetValue(this) is not null);
    }
    /// <summary>
    /// 更新キーまたはIDからIDを再取得します。
    /// </summary>
    /// <returns></returns>
    protected virtual Task RefreshIdFromKeyAsync() => Task.CompletedTask;
    /// <summary>
    /// インデックス情報を適用します。
    /// </summary>
    /// <param name="indexes"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private KintoneIndex ApplyIndex(KintoneIndexes indexes, int index = 0) {
        if (indexes.IDs.Count > index && indexes.Revisions.Count > index) {
            this.RecordID = indexes.IDs[index];
            this.Revision = Convert.ToInt32(indexes.Revisions[index]);

            return new KintoneIndex {
                ID = this.RecordID!,
                Revision = this.Revision
            };
        }

        return new KintoneIndex();
    }

}