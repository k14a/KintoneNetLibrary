using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Entities;

/// <summary>
/// KintoneModelBase クラスの Find メソッドに関するテストクラス。FindByIDAsync、FindByIDsAsync、FindByKeyAsync、FindByKeysAsync、FindByQueryAsync、FindAllAsync メソッドの正常系と異常系の動作を検証する。
/// </summary>
public class KintoneModelBaseFindTests {
    /// <summary>
    /// テスト用のダミーモデルクラス。FieldA と FieldB というフィールドを持ち、FindByID メソッドのテストに使用される。
    /// </summary>
    public class DummyModel : KintoneModelBase<DummyModel> {
        public override int AppID { get; init; }
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(fieldCode: "FieldA", fieldType: KintoneFieldType.SingleLineText)]
        public string FieldA { get; set; } = string.Empty;
        [KintoneItem(fieldCode: "FieldB", fieldType: KintoneFieldType.Number)]
        public int FieldB { get; set; }
    }

    /// <summary>
    /// テスト用のモデルクラス。Code プロパティをキーとして FindByKeyAsync メソッドのテストに使用される。
    /// </summary>
    public class KeyedModel : KintoneModelBase<KeyedModel> {
        public override int AppID { get; init; }
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(IsKey = true)]
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
    }

    /// <summary>
    /// テスト用のモデルクラス。Code プロパティが null 許容であり、FindByKeyAsync の null キー値動作テストに使用される。
    /// </summary>
    public class NullKeyModel : KintoneModelBase<NullKeyModel> {
        public override int AppID { get; init; }
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        public string? Code { get; set; }
        public string Name { get; set; } = default!;
    }

    #region <<Test methods>>

    // ---- FindByIDAsync ----

    /// <summary>
    /// 有効なレコードIDを指定して FindByIDAsync を呼び出すと、対応するモデルが返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByIDAsyncValidIDReturnsModel() {
        // Arrange
        var id = "2222";
        var expectedModel = new DummyModel { RecordID = id, FieldA = "Found", FieldB = 42 };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<DummyModel>(It.Is<IList<string>>(ids => ids.Count == 1 && ids[0] == id), null, null))
            .ReturnsAsync([expectedModel]);

        // Act
        var result = await DummyModel.FindByIDAsync(mockService.Object, id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedModel.RecordID, result!.RecordID);
        Assert.Equal("Found", result.FieldA);
        Assert.Equal(42, result.FieldB);
        mockService.Verify(s => s.FindAsync<DummyModel>(It.IsAny<IList<string>>(), null, null), Times.Once);
    }

    /// <summary>
    /// 存在しないレコードIDを指定して FindByIDAsync を呼び出すと、null が返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByIDAsyncIDNotFoundReturnsNull() {
        // Arrange
        var id = "not-found-id";

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<DummyModel>(It.Is<IList<string>>(ids => ids.Count == 1 && ids[0] == id), null, null))
            .ReturnsAsync([]);

        // Act
        var result = await DummyModel.FindByIDAsync(mockService.Object, id);

        // Assert
        Assert.Null(result);
        mockService.Verify(s => s.FindAsync<DummyModel>(It.IsAny<IList<string>>(), null, null), Times.Once);
    }

    /// <summary>
    /// 複数の有効なレコードIDを指定して FindByIDsAsync を呼び出すと、対応するモデルのリストが返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByIDsAsyncValidIDsReturnsMatchingModels() {
        // Arrange
        var ids = new List<string> { "id1", "id2" };
        var expectedModels = new List<DummyModel> {
            new() { RecordID = "id1", FieldA = "A1", FieldB = 1 },
            new() { RecordID = "id2", FieldA = "A2", FieldB = 2 }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(ids)), null, null))
            .ReturnsAsync(expectedModels);

        // Act
        var result = await DummyModel.FindByIDsAsync(mockService.Object, ids);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.RecordID == "id1");
        Assert.Contains(result, r => r.RecordID == "id2");
        mockService.Verify(s => s.FindAsync<DummyModel>(It.IsAny<IList<string>>(), null, null), Times.Once);
    }

    /// <summary>
    /// 空のレコードIDリストを指定して FindByIDsAsync を呼び出すと、空のリストが返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByIDsAsyncEmptyIDListReturnsEmptyResult() {
        // Arrange
        var ids = new List<string>();

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<DummyModel>(It.Is<IList<string>>(x => x.Count == 0), null, null))
            .ReturnsAsync([]);

        // Act
        var result = await DummyModel.FindByIDsAsync(mockService.Object, ids);

        // Assert
        Assert.Empty(result);
        mockService.Verify(s => s.FindAsync<DummyModel>(It.IsAny<IList<string>>(), null, null), Times.Once);
    }

    /// <summary>
    /// 複数のレコードIDを指定して FindByIDsAsync を呼び出すと、一部のIDに対応するモデルが存在しない場合でも、存在するモデルのみが返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByIDsAsyncPartialHitReturnsOnlyMatchingRecords() {
        // Arrange
        var requestedIds = new List<string> { "id1", "id2", "id3" };
        var existingRecords = new List<DummyModel> {
            new() { ID = "id1", FieldA = "Record 1" },
            new() { ID = "id3", FieldA = "Record 3" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<DummyModel>(It.Is<IList<string>>(ids => ids.SequenceEqual(requestedIds)), null, null))
            .ReturnsAsync(existingRecords);

        // Act
        var result = await DummyModel.FindByIDsAsync(mockService.Object, requestedIds);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.ID == "id1");
        Assert.Contains(result, r => r.ID == "id3");
        Assert.DoesNotContain(result, r => r.ID == "id2");

        mockService.Verify(s => s.FindAsync<DummyModel>(requestedIds, null, null), Times.Once);
    }

    // ---- FindByKeyAsync ----

    /// <summary>
    /// 有効なキーを指定して FindByKeyAsync を呼び出すと、対応するモデルが返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByKeyAsyncKeyExistsReturnsMatchingRecord() {
        // Arrange
        var input = new KeyedModel { Code = "123123" };
        var expected = new KeyedModel { Code = "123123", Name = "Test Record" };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(null, It.Is<string>(q => q.Contains("Code = \"123123\"")), null))
            .ReturnsAsync([expected]);

        // Act
        var result = await KeyedModel.FindByKeyAsync(mockService.Object, input, x => x.Code);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("123123", result!.Code);
        Assert.Equal("Test Record", result.Name);

        mockService.Verify(s => s.FindAsync<KeyedModel>(null, It.IsAny<string>(), null), Times.Once);
    }

    /// <summary>
    /// キー値が null の場合に FindByKeyAsync を呼び出すと、null が返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByKeyAsyncNullKeyValueReturnsNull() {
        // Arrange
        var model = new NullKeyModel { Code = null, Name = "NullKey" };
        var mockService = new Mock<IKintoneModelCrudService>();

        // Act
        var result = await NullKeyModel.FindByKeyAsync(mockService.Object, model, x => x.Code);

        // Assert
        Assert.Null(result);
        mockService.Verify(s => s.FindAsync<NullKeyModel>(It.IsAny<IList<string>?>(), It.IsAny<string?>(), It.IsAny<IList<string>?>()), Times.Never);
    }

    /// <summary>
    /// 存在しないキーを指定して FindByKeyAsync を呼び出すと、null が返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByKeyAsyncNoMatchingRecordReturnsNull() {
        // Arrange
        var input = new KeyedModel { Code = "NOT_FOUND" };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(null, It.Is<string>(q => q.Contains("Code = \"NOT_FOUND\"")), null))
            .ReturnsAsync([]);

        // Act
        var result = await KeyedModel.FindByKeyAsync(mockService.Object, input, x => x.Code);

        // Assert
        Assert.Null(result);
        mockService.Verify(s => s.FindAsync<KeyedModel>(null, It.IsAny<string>(), null), Times.Once);
    }

    // ---- FindByKeysAsync ----

    /// <summary>
    /// 複数の有効なキーを指定して FindByKeysAsync を呼び出すと、対応するモデルのリストが返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByKeysAsyncMultipleValidKeysReturnsMatchingRecords() {
        // Arrange
        var inputModels = new[] {
            new KeyedModel { Code = "A001" },
            new KeyedModel { Code = "B002" }
        };

        var expected = new[] {
            new KeyedModel { Code = "A001", Name = "Alpha" },
            new KeyedModel { Code = "B002", Name = "Beta" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), It.IsAny<string>(), It.IsAny<IList<string>?>()))
            .ReturnsAsync(expected);

        // Act
        var result = await KeyedModel.FindByKeysAsync(mockService.Object, inputModels, x => x.Code);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Code == "A001" && r.Name == "Alpha");
        Assert.Contains(result, r => r.Code == "B002" && r.Name == "Beta");
    }

    /// <summary>
    /// 有効なキーを指定して FindByKeysAsync を呼び出すと、対応するモデルが存在しない場合に空のリストが返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByKeysAsyncValidKeysButNoMatchesReturnsEmptyList() {
        // Arrange
        var inputModels = new[] {
            new KeyedModel { Code = "X999" },
            new KeyedModel { Code = "Y888" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), It.IsAny<string>(), It.IsAny<IList<string>?>()))
            .ReturnsAsync([]);

        // Act
        var result = await KeyedModel.FindByKeysAsync(mockService.Object, inputModels, x => x.Code);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// 一部のキーに一致するレコードのみが返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByKeysAsyncSomeKeysMatchReturnsOnlyMatchingRecords() {
        // Arrange
        var inputModels = new[] {
            new KeyedModel { Code = "A001" },
            new KeyedModel { Code = "B002" },
            new KeyedModel { Code = "C003" }
        };

        var expected = new[] {
            new KeyedModel { Code = "A001", Name = "Alpha" },
            new KeyedModel { Code = "B002", Name = "Beta" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), It.IsAny<string>(), It.IsAny<IList<string>?>()))
            .ReturnsAsync(expected);

        // Act
        var result = await KeyedModel.FindByKeysAsync(mockService.Object, inputModels, x => x.Code);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Code == "A001" && r.Name == "Alpha");
        Assert.Contains(result, r => r.Code == "B002" && r.Name == "Beta");
        Assert.DoesNotContain(result, r => r.Code == "C003");
    }

    /// <summary>
    /// null キー値を含むモデルリストで FindByKeysAsync を呼び出すと、null 値がスキップされてクエリが実行されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByKeysAsyncNullKeyValuesAreFiltered() {
        // Arrange
        var inputModels = new[] {
            new NullKeyModel { Code = "A001" },
            new NullKeyModel { Code = null },
            new NullKeyModel { Code = "B002" }
        };

        var expected = new[] {
            new NullKeyModel { Code = "A001", Name = "Alpha" },
            new NullKeyModel { Code = "B002", Name = "Beta" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<NullKeyModel>(
                null,
                It.Is<string>(q => q.Contains("Code") && q.Contains("A001") && q.Contains("B002")),
                null))
            .ReturnsAsync(expected);

        // Act
        var result = await NullKeyModel.FindByKeysAsync(mockService.Object, inputModels, x => x.Code);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Code == "A001");
        Assert.Contains(result, r => r.Code == "B002");
        mockService.Verify(s => s.FindAsync<NullKeyModel>(null, It.IsAny<string>(), null), Times.Once);
    }

    /// <summary>
    /// すべてのキー値が null の場合 FindByKeysAsync は空リストを返し、サービスを呼び出さないことを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByKeysAsyncAllNullKeyValuesReturnsEmptyWithoutServiceCall() {
        // Arrange
        var inputModels = new[] {
            new NullKeyModel { Code = null },
            new NullKeyModel { Code = null }
        };

        var mockService = new Mock<IKintoneModelCrudService>();

        // Act
        var result = await NullKeyModel.FindByKeysAsync(mockService.Object, inputModels, x => x.Code);

        // Assert
        Assert.Empty(result);
        mockService.Verify(s => s.FindAsync<NullKeyModel>(It.IsAny<IList<string>?>(), It.IsAny<string?>(), It.IsAny<IList<string>?>()), Times.Never);
    }

    // ---- FindByQueryAsync ----

    /// <summary>
    /// 有効なクエリを指定して FindByQueryAsync を呼び出すと、対応するモデルのリストが返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByQueryAsyncValidQueryReturnsMatchingRecords() {
        // Arrange
        var query = "Code = \"A001\"";
        var expected = new[] {
            new KeyedModel { Code = "A001", Name = "Alpha" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), query, It.IsAny<IList<string>?>()))
            .ReturnsAsync(expected);

        // Act
        var result = await KeyedModel.FindByQueryAsync(mockService.Object, query);

        // Assert
        Assert.Single(result);
        Assert.Equal("A001", result[0].Code);
        Assert.Equal("Alpha", result[0].Name);
    }

    /// <summary>
    /// 有効なクエリを指定して FindByQueryAsync を呼び出すと、対応するモデルが存在しない場合に空のリストが返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindByQueryAsyncValidQueryButNoMatchReturnsEmptyList() {
        // Arrange
        var query = "Code = \"Z999\"";

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), query, It.IsAny<IList<string>?>()))
            .ReturnsAsync([]);

        // Act
        var result = await KeyedModel.FindByQueryAsync(mockService.Object, query);

        // Assert
        Assert.Empty(result);
    }

    // ---- FindAllAsync ----

    /// <summary>
    /// FindAllAsync を呼び出すと、すべてのレコードが返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindAllAsyncReturnsAllRecords() {
        // Arrange
        var expected = new[] {
            new KeyedModel { Code = "A001", Name = "Alpha" },
            new KeyedModel { Code = "B002", Name = "Beta" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), null, It.IsAny<IList<string>?>()))
            .ReturnsAsync(expected);

        // Act
        var result = await KeyedModel.FindAllAsync(mockService.Object);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("A001", result[0].Code);
        Assert.Equal("B002", result[1].Code);
    }

    /// <summary>
    /// FindAllAsync を呼び出すと、レコードが存在しない場合に空のリストが返されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindAllAsyncNoRecordsReturnsEmptyList() {
        // Arrange
        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), null, It.IsAny<IList<string>?>()))
            .ReturnsAsync([]);

        // Act
        var result = await KeyedModel.FindAllAsync(mockService.Object);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// FindAllAsync を呼び出すと、IKintoneModelCrudService の FindAsync メソッドが例外をスローした場合に、その例外が呼び出し元に伝播されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task FindAllAsyncServiceThrowsExceptionPropagatesException() {
        // Arrange
        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), null, It.IsAny<IList<string>?>()))
            .ThrowsAsync(new TimeoutException("Kintone API timeout"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TimeoutException>(() => KeyedModel.FindAllAsync(mockService.Object));
        Assert.Equal("Kintone API timeout", ex.Message);
    }
    #endregion
}
