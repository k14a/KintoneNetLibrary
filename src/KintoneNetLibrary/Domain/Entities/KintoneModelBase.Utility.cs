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

        return this.HasUpdateKey();
    }

    /// <summary>
    /// 更新キーが設定されているかどうかを判定します。
    /// </summary>
    /// <returns></returns>
    public virtual bool HasUpdateKey() {
        return this.GetType().GetProperties()
            .Where(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true)
            .Any(p => p.GetValue(this) is string s ? !string.IsNullOrEmpty(s) : p.GetValue(this) is not null);
    }

    /// <summary>
    /// 更新キーの値を取得します。更新キーが複数ある場合は、最初に見つかったものを返します。
    /// 更新キーが設定されていない場合はnullを返します。
    /// </summary>
    /// <returns>更新キーの値、またはnull</returns>
    public virtual string? GetUpdateKeyValue() {
        var keyProp = this.GetType().GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true);

        if (keyProp == null) { return null; }

        var value = keyProp.GetValue(this);
        return value?.ToString();
    }

    /// <summary>
    /// 更新キーまたはIDからIDを再取得します。
    /// </summary>
    /// <returns></returns>
    protected virtual Task RefreshIdFromKeyAsync() => Task.CompletedTask;

    /// <summary>
    /// インデックス情報を適用します。
    /// </summary>
    /// <param name="indexes">インデックス情報</param>
    /// <param name="index">適用するインデックスの位置</param>
    /// <returns>適用後のKintoneIndex</returns>
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