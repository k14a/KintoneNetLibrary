using System;
using KintoneNetLibrary.Infrastructure.Helpers;
using KintoneNetLibrary.Domain.Entities;
using Xunit;
using DocumentFormat.OpenXml.Wordprocessing;

namespace KintoneNetLibrary.Tests.Helpers;

public class KintoneModelValidatorTests {
    #region <<Test classes>>
    public class FakeModelWithMultipleKeys : KintoneModelBase<FakeModelWithMultipleKeys> {
        public override int AppID => 8888;
        [KintoneItem(IsKey = true)]
        public string CodeA { get; set; } = "A001";
        [KintoneItem(IsKey = true)]
        public string CodeB { get; set; } = "B001";
    }
    public class FakeModelWithMissingKey : KintoneModelBase<FakeModelWithMissingKey> {
        public override int AppID => 7777;
        [KintoneItem(IsKey = true)]
        public string? KeyCode { get; set; } = null;
    }
    public class FakeModelWithKey : KintoneModelBase<FakeModelWithKey> {
        public override int AppID => 6666;
        [KintoneItem(IsKey = true)]
        public string? Code { get; set; }
    }
    public class FakeModelWithLinks : KintoneModelBase<FakeModelWithLinks> {
        public override int AppID => 5555;
        [KintoneItem(FieldType = KintoneFieldType.LinkUrl)]
        public string? Website { get; set; }

        [KintoneItem(FieldType = KintoneFieldType.LinkTelephone)]
        public string? Phone { get; set; }

        [KintoneItem(FieldType = KintoneFieldType.LinkEmail)]
        public string? Email { get; set; }
    }
    public class FakeSubRow : KintoneSubTableBase {
        [KintoneItem(FieldCode = "Text")]
        public string Text { get; set; } = string.Empty;
    }
    public class FakeModelWithInvalidSubTable : KintoneModelBase<FakeModelWithInvalidSubTable> {
        public override int AppID => 5555;
        [KintoneItem(fieldType: KintoneFieldType.SubTable)]
        public string NotAList { get; set; } = "invalid";
    }
    public class FakeModelWithValidSubTable : KintoneModelBase<FakeModelWithValidSubTable> {
        public override int AppID => 5555;
        [KintoneItem(fieldType: KintoneFieldType.SubTable)]
        public List<FakeSubRow> SubRows { get; set; } = [];
    }
    public class FakeModelWithStructuredFields : KintoneModelBase<FakeModelWithStructuredFields> {
        public override int AppID => 4444;
        [KintoneItem(FieldType = KintoneFieldType.File)]
        public object? AttachedFiles { get; set; }

        [KintoneItem(FieldType = KintoneFieldType.CheckBox)]
        public IList<string>? CheckValues { get; set; } = [];

        [KintoneItem(FieldType = KintoneFieldType.MultiSelect)]
        public IList<string>? SelectValues { get; set; } = [];

        [KintoneItem(FieldType = KintoneFieldType.Category)]
        public IList<string>? CategoryValues { get; set; } = [];
    }
    public class CompositeTestModel : KintoneModelBase<CompositeTestModel> {
        public override int AppID => 3333;
        [KintoneItem(IsKey = true)]
        public string? KeyCode { get; set; }
        [KintoneItem(FieldType = KintoneFieldType.File)]
        public object? Files { get; set; }
    }

    #endregion

    #region <<Test methods>>
    [Fact]
    public void ValidateUniqueKeyProperty_WhenMultipleKeysExist_ThrowsException() {
        var model = new FakeModelWithMultipleKeys();

        var ex = Assert.Throws<InvalidOperationException>(() => KintoneModelValidator.ValidateKeyIntegrity(model));

        Assert.Contains("IsKey が複数", ex.Message);
        Assert.Contains("CodeA", ex.Message);
        Assert.Contains("CodeB", ex.Message);
    }
    [Fact]
    public void ValidateUpdateKey_WhenKeyPropertyIsNullOrEmpty_ThrowsException() {
        var model = new FakeModelWithMissingKey(); // KeyCode は null

        var ex = Assert.Throws<InvalidOperationException>(() => KintoneModelValidator.ValidateKeyIntegrity(model));

        Assert.Contains("KeyCode", ex.Message);
        Assert.Contains("値が未設定", ex.Message);

        // 空文字列でも例外になることを確認
        model.KeyCode = "";
        var ex2 = Assert.Throws<InvalidOperationException>(() => KintoneModelValidator.ValidateKeyIntegrity(model));

        Assert.Contains("KeyCode", ex2.Message);
    }
    [Fact]
    public void ValidateDuplicateKeyValues_WhenDuplicateKeyValuesExist_ThrowsException() {
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
    [Fact]
    public void ValidateLinkFields_WhenUrlIsInvalid_ThrowsException() {
        var model = new FakeModelWithLinks {
            Website = "ftp://invalid.com"
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateLinkFields(model)
        );

        Assert.Contains("Website", ex.Message);
        Assert.Contains("URL", ex.Message);
    }

    [Fact]
    public void ValidateLinkFields_WhenPhoneIsInvalid_ThrowsException() {
        var model = new FakeModelWithLinks {
            Phone = "ABC-DEF-GHIJ"
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateLinkFields(model)
        );

        Assert.Contains("Phone", ex.Message);
        Assert.Contains("電話番号", ex.Message);
    }

    [Fact]
    public void ValidateLinkFields_WhenEmailIsInvalid_ThrowsException() {
        var model = new FakeModelWithLinks {
            Email = "abc@" // 不完全なメール形式
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateLinkFields(model)
        );

        Assert.Contains("Email", ex.Message);
        Assert.Contains("メールアドレス", ex.Message);
    }
    [Fact]
    public void ValidateLinkFields_WhenAllLinksAreValid_DoesNotThrow() {
        var model = new FakeModelWithLinks {
            Website = "https://valid.com",
            Phone = "+81-90-1234-5678",
            Email = "test@example.com"
        };

        KintoneModelValidator.ValidateLinkFields(model); // 例外が発生しなければ OK
    }
    [Fact]
    public void ValidateSubTableProperties_WhenTypeIsNotList_ThrowsException() {
        var model = new FakeModelWithInvalidSubTable();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateStructuredFields(model)
        );

        Assert.Contains("NotAList", ex.Message);
        Assert.Contains("List<T> 型", ex.Message);
    }
    [Fact]
    public void ValidateSubTableProperties_WhenTypeIsValidList_DoesNotThrow() {
        var model = new FakeModelWithValidSubTable();
        // 例外が発生しなければ OK
        KintoneModelValidator.ValidateStructuredFields(model);
    }
    [Fact]
    public void ValidateStructuredFields_WhenFileIsNull_ThrowsException() {
        var model = new FakeModelWithStructuredFields {
            AttachedFiles = null
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateStructuredFields(model)
        );

        Assert.Contains("File型", ex.Message);
    }
    [Fact]
    public void ValidateStructuredFields_WhenFileTypeIsIncorrect_ThrowsException() {
        var model = new FakeModelWithStructuredFields {
            AttachedFiles = new List<string> { "wrong.pdf" }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateStructuredFields(model)
        );

        Assert.Contains("File型", ex.Message);
    }
    [Fact]
    public void ValidateStructuredFields_WhenCheckBoxIsNull_ThrowsException() {
        var model = new FakeModelWithStructuredFields {
            AttachedFiles = new List<KintoneFile>(),
            CheckValues = null
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateStructuredFields(model)
        );

        Assert.Contains("複数選択型", ex.Message);
    }
    [Fact]
    public void ValidateStructuredFields_WhenMultiSelectTypeIsIncorrect_ThrowsException() {
        var model = new FakeModelWithStructuredFields {
            AttachedFiles = new List<KintoneFile>(),
            SelectValues = null
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateStructuredFields(model)
        );

        Assert.Contains("IList<string>", ex.Message);
    }
    [Fact]
    public void ValidateStructuredFields_WhenCategoryIsNull_ThrowsException() {
        var model = new FakeModelWithStructuredFields {
            AttachedFiles = new List<KintoneFile>(),
            CategoryValues = null
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            KintoneModelValidator.ValidateStructuredFields(model)
        );

        Assert.Contains("複数選択型", ex.Message);
    }
    [Fact]
    public void ValidateStructuredFields_WhenAllStructuredFieldsAreValid_DoesNotThrow() {
        var model = new FakeModelWithStructuredFields {
            AttachedFiles = new List<KintoneFile>(),
            CheckValues = ["A"],
            SelectValues = [],
            CategoryValues = ["X"]
        };

        KintoneModelValidator.ValidateStructuredFields(model);
    }
    [Fact]
    public void TryValidateModelStructure_ReturnsTrue_WhenAllValidationsPass() {
        var model = new CompositeTestModel {
            KeyCode = "A001",
            Files = new List<KintoneFile>()
        };
        var result = KintoneModelValidator.TryValidateModelStructure(model, out var errors);
        Assert.True(result);
        Assert.Empty(errors);
    }

    [Fact]
    public void TryValidateModelStructure_ReturnsFalse_WhenUpdateKeyIsMissing() {
        var model = new CompositeTestModel {
            KeyCode = null,
            Files = new List<KintoneFile>()
        };
        var result = KintoneModelValidator.TryValidateModelStructure(model, out var errors);
        Assert.False(result);
        Assert.Contains("値が未設定", errors.First());
    }

    [Fact]
    public void TryValidateModelStructure_ReturnsFalse_WhenStructuredFieldIsInvalid() {
        var model = new CompositeTestModel {
            KeyCode = "A001",
            Files = null
        };
        var result = KintoneModelValidator.TryValidateModelStructure(model, out var errors);
        Assert.False(result);
        Assert.Contains("File型フィールド", errors.First());
    }

    [Fact]
    public void TryValidateModelStructure_ReturnsFalse_WhenDuplicateKeyValueExists() {
        var models = new List<CompositeTestModel> {
            new() { KeyCode = "DUP", Files = new List<KintoneFile>() },
            new() { KeyCode = "DUP", Files = new List<KintoneFile>() }
        };
        var result = KintoneModelValidator.TryValidateModelStructure(models[0], out var errors, models);
        Assert.False(result);
        Assert.Contains("同じキー値", errors.First());
    }

    [Fact]
    public void TryValidateModelStructure_ReturnsFalse_WhenMultipleErrorsExist() {
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
