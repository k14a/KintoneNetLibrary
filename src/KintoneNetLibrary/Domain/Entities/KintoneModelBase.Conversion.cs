namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// KintoneModelBase クラスの変換に関する部分
/// </summary>
/// <typeparam name="TSelf"></typeparam>
public abstract partial class KintoneModelBase<TSelf> : KintoneModelHookBase where TSelf : KintoneModelBase<TSelf>, new() {

    /// <summary>
    /// 変換辞書
    /// </summary>
    private IList<NameConverter> _convertDictionary = [
        NameConverter.Create("$id", nameof(RecordId), NameConverter.Direction.Read),
        NameConverter.Create("レコード番号", nameof(RecordId), NameConverter.Direction.Send),
        NameConverter.Create("$revision", nameof(Revision), NameConverter.Direction.Read),
        NameConverter.Create("作成日時", nameof(CreatedTime)),
        NameConverter.Create("更新日時", nameof(UpdatedTime)),
        NameConverter.Create("作成者", nameof(CreatedBy)),
        NameConverter.Create("更新者", nameof(UpdatedBy)),
        NameConverter.Create("ステータス", nameof(Status)),
        NameConverter.Create("作業者", nameof(Assignee))
    ];

    /// <summary>
    /// 変換辞書
    /// </summary>
    /// <remarks>
    /// このプロパティは、Kintoneのフィールド名とモデルのプロパティ名をマッピングするために使用されます。
    /// </remarks>
    public virtual IList<NameConverter> ConvertDictionary {
        get => this._convertDictionary;
        set => this._convertDictionary = value;
    }
}