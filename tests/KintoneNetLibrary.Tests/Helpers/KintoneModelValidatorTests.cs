using KintoneNetLibrary.Infrastructure.Helpers;
using KintoneNetLibrary.Domain.Entities;
using Xunit;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Tests.Helpers;

/// <summary>
/// KintoneModelValidator クラスの単体テスト。
/// </summary>
public class KintoneModelValidatorTests {
    #region <<Test classes>>
    /// <summary>
    /// 複数のプロパティに IsKey=true が設定されたモデル。キーの一意性検証で例外が発生することを確認するためのテストクラス。
    /// </summary>
    public class FakeModelWithMultipleKeys : KintoneModelBase<FakeModelWithMultipleKeys> {
        public override int AppID { get; init; } = 8888;
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("dummyDomain", "dummyApiToken");
        [KintoneItem(IsKey = true)]
        public string CodeA { get; set; } = "A001";
        [KintoneItem(IsKey = true)]
        public string CodeB { get; set; } = "B001";
    }

    /// <summary>
    /// キー属性があるが値が null のモデル。更新キーの値が未設定の場合に例外が発生することを確認するためのテストクラス。
    /// </summary>
    public class FakeModelWithMissingKey : KintoneModelBase<FakeModelWithMissingKey> {
        public override int AppID { get; init; } = 7777;
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("dummyDomain", "dummyApiToken");
        [KintoneItem(IsKey = true)]
        public string? KeyCode { get; set; } = null;
    }

    /// <summary>
    /// キー属性があるモデル。キー値の重複検証で例外が発生することを確認するためのテストクラス。
    /// </summary>
    public class FakeModelWithKey : KintoneModelBase<FakeModelWithKey> {
        public override int AppID { get; init; } = 6666;
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("dummyDomain", "dummyApiToken");
        [KintoneItem(IsKey = true)]
        public string? Code { get; set; }
    }

    /// <summary>
    /// URL、電話番号、メールアドレスのリンクフィールドを持つモデル。リンクフィールドの形式検証で例外が発生することを確認するためのテストクラス。
    /// </summary>
    public class FakeModelWithLinks : KintoneModelBase<FakeModelWithLinks> {
        public override int AppID { get; init; } = 5555;
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("dummyDomain", "dummyApiToken");
        [KintoneItem(FieldType = KintoneFieldType.LinkUrl)]
        public string? Website { get; set; }

        [KintoneItem(FieldType = KintoneFieldType.LinkTelephone)]
        public string? Phone { get; set; }

        [KintoneItem(FieldType = KintoneFieldType.LinkEmail)]
        public string? Email { get; set; }
    }

    /// <summary>
    /// サブテーブルの行を表すクラス。サブテーブルの構造検証で、List<T> 型であることが要求されるため、正しい構造のサブテーブル行クラスを定義するためのテストクラス。
    /// </summary>
    public class FakeSubRow : KintoneSubTableBase {
        [KintoneItem(FieldCode = "Text")]
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// サブテーブルフィールドに List<T> 型以外のプロパティが定義されたモデル。サブテーブルの構造検証で例外が発生することを確認するためのテストクラス。
    /// </summary>
    public class FakeModelWithInvalidSubTable : KintoneModelBase<FakeModelWithInvalidSubTable> {
        public override int AppID { get; init; } = 5555;
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("dummyDomain", "dummyApiToken");
        [KintoneItem(fieldType: KintoneFieldType.SubTable)]
        public string NotAList { get; set; } = "invalid";
    }

    /// <summary>
    /// サブテーブルフィールドに List<T> 型のプロパティが定義されたモデル。サブテーブルの構造検証で例外が発生しないことを確認するためのテストクラス。
    /// </summary>
    public class FakeModelWithValidSubTable : KintoneModelBase<FakeModelWithValidSubTable> {
        public override int AppID { get; init; } = 5555;
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("dummyDomain", "dummyApiToken");
        [KintoneItem(fieldType: KintoneFieldType.SubTable)]
        public List<FakeSubRow> SubRows { get; set; } = [];
    }

    /// <summary>
    /// ファイル、複数選択型（チェックボックス、ドロップダウン、カテゴリ）フィールドを持つモデル。これらの構造化フィールドの検証で例外が発生することを確認するためのテストクラス。
    /// </summary>
    public class FakeModelWithStructuredFields : KintoneModelBase<FakeModelWithStructuredFields> {
        public override int AppID { get; init; } = 4444;
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("dummyDomain", "dummyApiToken");
        [KintoneItem(FieldType = KintoneFieldType.File)]
        public object? AttachedFiles { get; set; }

        [KintoneItem(FieldType = KintoneFieldType.CheckBox)]
        public IList<string>? CheckValues { get; set; } = [];

        [KintoneItem(FieldType = KintoneFieldType.MultiSelect)]
        public IList<string>? SelectValues { get; set; } = [];

        [KintoneItem(FieldType = KintoneFieldType.Category)]
        public IList<string>? CategoryValues { get; set; } = [];
    }

    /// <summary>
    /// 複数の検証ルールを組み合わせたモデル。更新キー、ファイルフィールド、複数選択型フィールドなど、複数の検証ルールが同時に適用されるケースをテストするためのクラス。
    /// </summary>
    public class CompositeTestModel : KintoneModelBase<CompositeTestModel> {
        public override int AppID { get; init; } = 3333;
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("dummyDomain", "dummyApiToken");

        [KintoneItem(IsKey = true)]
        public string? KeyCode { get; set; }
        [KintoneItem(FieldType = KintoneFieldType.File)]
        public object? Files { get; set; }
    }
    #endregion

    #region <<Test methods>>
    /// <summary>
    /// 複数のプロパティに IsKey=true が設定されたモデルを検証した場合に、例外が発生することを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateUniqueKeyPropertyWhenMultipleKeysExistThrowsException() {
        var model = new FakeModelWithMultipleKeys();

        var ex = Assert.Throws<InvalidOperationException>(() => KintoneModelValidator.ValidateKeyIntegrity(model));

        Assert.Contains("IsKey が複数", ex.Message);
        Assert.Contains("CodeA", ex.Message);
        Assert.Contains("CodeB", ex.Message);
    }

    /// <summary>
    /// キー属性があるが値が null または空文字列のモデルを検証した場合に、例外が発生することを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateUpdateKeyWhenKeyPropertyIsNullOrEmptyThrowsException() {
        var model = new FakeModelWithMissingKey(); // KeyCode は null

        var ex = Assert.Throws<InvalidOperationException>(() => KintoneModelValidator.ValidateKeyIntegrity(model));

        Assert.Contains("KeyCode", ex.Message);
        Assert.Contains("値が未設定", ex.Message);

        // 空文字列でも例外になることを確認
        model.KeyCode = "";
        var ex2 = Assert.Throws<InvalidOperationException>(() => KintoneModelValidator.ValidateKeyIntegrity(model));

        Assert.Contains("KeyCode", ex2.Message);
    }

    /// <summary>
    /// キー属性があるモデルのリストを検証した場合に、同じキー値が複数存在する場合は例外が発生することを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateDuplicateKeyValuesWhenDuplicateKeyValuesExistThrowsException() {
        var models = new List<FakeModelWithKey> {
            new() { Code = "X001" },
            new() { Code = "X002" },
            new() { Code = "X001" } // 重複
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateKeyValueUniqueness(models)
        );

        Assert.Contains("同じキー値が複数", ex.Message);
        Assert.Contains("X001", ex.Message);
    }

    /// <summary>
    /// URL、電話番号、メールアドレスのリンクフィールドを持つモデルを検証した場合に、各フィールドの形式が正しくない場合は例外が発生することを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateLinkFieldsWhenUrlIsInvalidThrowsException() {
        var model = new FakeModelWithLinks {
            Website = "ftp://invalid.com"
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateLinkFields(model)
        );

        Assert.Contains("Website", ex.Message);
        Assert.Contains("URL", ex.Message);
    }

    /// <summary>
    /// URL、電話番号、メールアドレスのリンクフィールドを持つモデルを検証した場合に、各フィールドの形式が正しくない場合は例外が発生することを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateLinkFieldsWhenPhoneIsInvalidThrowsException() {
        var model = new FakeModelWithLinks {
            Phone = "ABC-DEF-GHIJ"
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateLinkFields(model)
        );

        Assert.Contains("Phone", ex.Message);
        Assert.Contains("電話番号", ex.Message);
    }

    /// <summary>
    /// URL、電話番号、メールアドレスのリンクフィールドを持つモデルを検証した場合に、各フィールドの形式が正しくない場合は例外が発生することを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateLinkFieldsWhenEmailIsInvalidThrowsException() {
        var model = new FakeModelWithLinks {
            Email = "abc@" // 不完全なメール形式
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateLinkFields(model)
        );

        Assert.Contains("Email", ex.Message);
        Assert.Contains("メールアドレス", ex.Message);
    }

    /// <summary>
    /// URL、電話番号、メールアドレスのリンクフィールドを持つモデルを検証した場合に、すべてのフィールドの形式が正しい場合は例外が発生しないことを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateLinkFieldsWhenAllLinksAreValidDoesNotThrow() {
        var model = new FakeModelWithLinks {
            Website = "https://valid.com",
            Phone = "+81-90-1234-5678",
            Email = "test@example.com"
        };

        KintoneModelValidator.ValidateLinkFields(model); // 例外が発生しなければ OK
    }

    /// <summary>
    /// サブテーブルフィールドに List<T> 型以外のプロパティが定義されたモデルを検証した場合に、例外が発生することを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateSubTablePropertiesWhenTypeIsNotListThrowsException() {
        var model = new FakeModelWithInvalidSubTable();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateStructuredFields(model)
        );

        Assert.Contains("NotAList", ex.Message);
        Assert.Contains("List<T> 型", ex.Message);
    }

    /// <summary>
    /// サブテーブルフィールドに List<T> 型のプロパティが定義されたモデルを検証した場合に、例外が発生しないことを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateSubTablePropertiesWhenTypeIsValidListDoesNotThrow() {
        var model = new FakeModelWithValidSubTable();
        // 例外が発生しなければ OK
        KintoneModelValidator.ValidateStructuredFields(model);
    }

    /// <summary>
    /// ファイル、複数選択型（チェックボックス、ドロップダウン、カテゴリ）フィールドを持つモデルを検証した場合に、ファイルフィールドが null の場合や、複数選択型フィールドが null の場合に例外が発生することを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateStructuredFieldsWhenFileIsNullThrowsException() {
        var model = new FakeModelWithStructuredFields {
            AttachedFiles = null
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateStructuredFields(model)
        );

        Assert.Contains("File型", ex.Message);
    }

    /// <summary>
    /// ファイル、複数選択型（チェックボックス、ドロップダウン、カテゴリ）フィールドを持つモデルを検証した場合に、ファイルフィールドの型が正しくない場合に例外が発生することを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateStructuredFieldsWhenFileTypeIsIncorrectThrowsException() {
        var model = new FakeModelWithStructuredFields {
            AttachedFiles = new List<string> { "wrong.pdf" }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateStructuredFields(model)
        );

        Assert.Contains("File型", ex.Message);
    }

    /// <summary>
    /// ファイル、複数選択型（チェックボックス、ドロップダウン、カテゴリ）フィールドを持つモデルを検証した場合に、複数選択型のチェックボックスフィールドが null の場合に例外が発生することを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateStructuredFieldsWhenCheckBoxIsNullThrowsException() {
        var model = new FakeModelWithStructuredFields {
            AttachedFiles = new List<KintoneFile>(),
            CheckValues = null
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateStructuredFields(model)
        );

        Assert.Contains("複数選択型", ex.Message);
    }

    /// <summary>
    /// ファイル、複数選択型（チェックボックス、ドロップダウン、カテゴリ）フィールドを持つモデルを検証した場合に、複数選択型のドロップダウンフィールドが正しくない型の場合に例外が発生することを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateStructuredFieldsWhenMultiSelectTypeIsIncorrectThrowsException() {
        var model = new FakeModelWithStructuredFields {
            AttachedFiles = new List<KintoneFile>(),
            SelectValues = null
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateStructuredFields(model)
        );

        Assert.Contains("IList<string>", ex.Message);
    }

    /// <summary>
    /// ファイル、複数選択型（チェックボックス、ドロップダウン、カテゴリ）フィールドを持つモデルを検証した場合に、複数選択型のカテゴリフィールドが null の場合に例外が発生することを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateStructuredFieldsWhenCategoryIsNullThrowsException() {
        var model = new FakeModelWithStructuredFields {
            AttachedFiles = new List<KintoneFile>(),
            CategoryValues = null
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateStructuredFields(model)
        );

        Assert.Contains("複数選択型", ex.Message);
    }

    /// <summary>
    /// ファイル、複数選択型（チェックボックス、ドロップダウン、カテゴリ）フィールドを持つモデルを検証した場合に、すべてのフィールドが正しい場合は例外が発生しないことを確認するテスト。
    /// </summary>
    [Fact]
    public void ValidateStructuredFieldsWhenAllStructuredFieldsAreValidDoesNotThrow() {
        var model = new FakeModelWithStructuredFields {
            AttachedFiles = new List<KintoneFile>(),
            CheckValues = ["A"],
            SelectValues = [],
            CategoryValues = ["X"]
        };

        KintoneModelValidator.ValidateStructuredFields(model);
    }

    /// <summary>
    /// 複数の検証ルールを組み合わせたモデルを検証した場合に、すべての検証がパスする場合は true が返され、エラーリストが空であることを確認するテスト。
    /// </summary>
    [Fact]
    public void TryValidateModelStructureReturnsTrueWhenAllValidationsPass() {
        var model = new CompositeTestModel {
            KeyCode = "A001",
            Files = new List<KintoneFile>()
        };
        var result = KintoneModelValidator.TryValidateModelStructure(model, out var errors);
        Assert.True(result);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 複数の検証ルールを組み合わせたモデルを検証した場合に、更新キーが未設定の場合は false が返され、エラーリストに該当のエラーが含まれることを確認するテスト。
    /// </summary>
    [Fact]
    public void TryValidateModelStructureReturnsFalseWhenUpdateKeyIsMissing() {
        var model = new CompositeTestModel {
            KeyCode = null,
            Files = new List<KintoneFile>()
        };
        var result = KintoneModelValidator.TryValidateModelStructure(model, out var errors);
        Assert.False(result);
        Assert.Contains("値が未設定", errors.First());
    }

    /// <summary>
    /// 複数の検証ルールを組み合わせたモデルを検証した場合に、ファイルフィールドが null の場合は false が返され、エラーリストに該当のエラーが含まれることを確認するテスト。
    /// </summary>
    [Fact]
    public void TryValidateModelStructureReturnsFalseWhenStructuredFieldIsInvalid() {
        var model = new CompositeTestModel {
            KeyCode = "A001",
            Files = null
        };
        var result = KintoneModelValidator.TryValidateModelStructure(model, out var errors);
        Assert.False(result);
        Assert.Contains("File型フィールド", errors.First());
    }

    /// <summary>
    /// 複数の検証ルールを組み合わせたモデルを検証した場合に、同じキー値が複数存在する場合は false が返され、エラーリストに該当のエラーが含まれることを確認するテスト。
    /// </summary>
    [Fact]
    public void TryValidateModelStructureReturnsFalseWhenDuplicateKeyValueExists() {
        var models = new List<CompositeTestModel> {
            new() { KeyCode = "DUP", Files = new List<KintoneFile>() },
            new() { KeyCode = "DUP", Files = new List<KintoneFile>() }
        };
        var result = KintoneModelValidator.TryValidateModelStructure(models[0], out var errors, models);
        Assert.False(result);
        Assert.Contains("同じキー値", errors.First());
    }

    /// <summary>
    /// 複数の検証ルールを組み合わせたモデルを検証した場合に、複数のエラーが存在する場合は false が返され、エラーリストにすべての該当エラーが含まれることを確認するテスト。
    /// </summary>
    [Fact]
    public void TryValidateModelStructureReturnsFalseWhenMultipleErrorsExist() {
        var models = new List<CompositeTestModel> {
            new() { KeyCode = null, Files = null }, // 両方 invalid
            new() { KeyCode = null, Files = null }
        };
        var result = KintoneModelValidator.TryValidateModelStructure(models[0], out var errors, models);
        Assert.False(result);
        Assert.True(errors.Count >= 2);
        Assert.Contains(errors, e => e.Contains("値が未設定"));
        Assert.Contains(errors, e => e.Contains("File型フィールド"));
    }
    #endregion
}
