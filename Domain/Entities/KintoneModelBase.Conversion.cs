using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Domain.Entities;

public abstract partial class KintoneModelBase : KintoneModelHookBase {

    // ----- 項目名変換 -----
    private IList<NameConvertor> _convertDictionary =
    [
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

    public virtual IList<NameConvertor> ConvertDictionary {
        get => _convertDictionary;
        set => _convertDictionary = value;
    }
}