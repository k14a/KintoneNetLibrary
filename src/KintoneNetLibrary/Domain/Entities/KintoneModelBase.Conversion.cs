using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneモデル基底クラス（変換機能付き）
/// </summary>
/// <typeparam name="TSelf"></typeparam>
public abstract partial class KintoneModelBase<TSelf> : KintoneModelHookBase where TSelf : KintoneModelBase<TSelf>, new() {

    /// <summary>
    /// 変換辞書
    /// </summary>
    private IList<NameConvertor> _convertDictionary = [
        NameConvertor.Create("$id", nameof(RecordID), NameConvertor.Direction.Read),
        NameConvertor.Create("レコード番号", nameof(RecordID), NameConvertor.Direction.Send),
        NameConvertor.Create("$revision", nameof(Revision), NameConvertor.Direction.Read),
        NameConvertor.Create("作成日時", nameof(CreatedTime)),
        NameConvertor.Create("更新日時", nameof(UpdatedTime)),
        NameConvertor.Create("作成者", nameof(CreatedBy)),
        NameConvertor.Create("更新者", nameof(UpdatedBy)),
        NameConvertor.Create("ステータス", nameof(Status)),
        NameConvertor.Create("作業者", nameof(Assignee))
    ];

    /// <summary>
    /// 変換辞書
    /// </summary>
    /// <remarks>
    /// このプロパティは、Kintoneのフィールド名とモデルのプロパティ名をマッピングするために使用されます。
    /// </remarks>
    public virtual IList<NameConvertor> ConvertDictionary {
        get => this._convertDictionary;
        set => this._convertDictionary = value;
    }
}