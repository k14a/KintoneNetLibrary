using System.Reflection;

namespace KintoneNetLibrary.Domain.Entities;

public abstract partial class KintoneModelBase : KintoneModelHookBase {
    // ----- 判定・ユーティリティ -----
    public virtual bool HasUpdateKeyOrID() {
        if (!string.IsNullOrEmpty(ID)) {
            return true;
        }

        return GetType().GetProperties()
            .Where(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true)
            .Any(p => p.GetValue(this) is string s ? !string.IsNullOrEmpty(s) : p.GetValue(this) is not null);
    }
    protected virtual Task RefreshIdFromKeyAsync() => Task.CompletedTask;
    private KintoneIndex ApplyIndex(KintoneIndexes indexes, int index = 0) {
        if (indexes.IDs.Count > index && indexes.Revisions.Count > index) {
            this.RecordID = indexes.IDs[index];
            this.Revision = Convert.ToInt32(indexes.Revisions[index]);

            return new KintoneIndex {
                ID = RecordID,
                Revision = this.Revision
            };
        }

        return new KintoneIndex();
    }

}