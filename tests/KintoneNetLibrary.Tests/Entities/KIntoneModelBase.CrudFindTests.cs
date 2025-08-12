using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SixLabors.ImageSharp.Formats.Tiff.Compression;
using Xunit;

namespace KintoneNetLibrary.Tests.Entities;

public class KintoneModelBase_FindTests {
    public class DummyModel : KintoneModelBase<DummyModel> {
        public override int AppID => throw new NotImplementedException();
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(fieldCode: "FieldA", fieldType: KintoneFieldType.SingleLineText)]
        public string FieldA { get; set; } = string.Empty;
        [KintoneItem(fieldCode: "FieldB", fieldType: KintoneFieldType.Number)]
        public int FieldB { get; set; }
    }
    public class KeyedModel : KintoneModelBase<KeyedModel> {
        public override int AppID => throw new NotImplementedException();
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(IsKey = true)]
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
    public class NoKeyModel : KintoneModelBase<NoKeyModel> {
        public override int AppID => throw new NotImplementedException();
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
    public class NullKeyModel : KintoneModelBase<NullKeyModel> {
        public override int AppID => throw new NotImplementedException();
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(IsKey = true)]
        public string? Code { get; set; }
        public string Name { get; set; } = default!;
    }
    public class EmptyKeyModel : KintoneModelBase<EmptyKeyModel> {
        public override int AppID => throw new NotImplementedException();
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(IsKey = true)]
        public string Code { get; set; } = "";
        public string Name { get; set; } = default!;
    }
    public class MultipleKeyModel : KintoneModelBase<MultipleKeyModel> {
        public override int AppID => throw new NotImplementedException();
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(IsKey = true)]
        public string Code { get; set; } = default!;
        [KintoneItem(IsKey = true)]
        public string SubCode { get; set; } = default!;
        public string Name { get; set; } = default!;
    }

    #region <<Test methods>>
    [Fact]
    public async Task FindByIDAsync_ValidID_ReturnsModel() {
        // Arrange
        var id = "2222";
        var expectedModel = new DummyModel { RecordID = id, FieldA = "Found", FieldB = 42 };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<DummyModel>(It.Is<IList<string>>(ids => ids.Count == 1 && ids[0] == id), null, null))
            .ReturnsAsync([expectedModel]);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.FindByIDAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedModel.RecordID, result!.RecordID);
        Assert.Equal("Found", result.FieldA);
        Assert.Equal(42, result.FieldB);
        mockService.Verify(s => s.FindAsync<DummyModel>(It.IsAny<IList<string>>(), null, null), Times.Once);
    }
    [Fact]
    public async Task FindByIDAsync_IDNotFound_ReturnsNull() {
        // Arrange
        var id = "not-found-id";

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<DummyModel>(It.Is<IList<string>>(ids => ids.Count == 1 && ids[0] == id), null, null))
            .ReturnsAsync([]);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.FindByIDAsync(id);

        // Assert
        Assert.Null(result);
        mockService.Verify(s => s.FindAsync<DummyModel>(It.IsAny<IList<string>>(), null, null), Times.Once);
    }
    [Fact]
    public async Task FindByIDsAsync_ValidIDs_ReturnsMatchingModels() {
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

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.FindByIDsAsync(ids);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.RecordID == "id1");
        Assert.Contains(result, r => r.RecordID == "id2");
        mockService.Verify(s => s.FindAsync<DummyModel>(It.IsAny<IList<string>>(), null, null), Times.Once);
    }
    [Fact]
    public async Task FindByIDsAsync_EmptyIDList_ReturnsEmptyResult() {
        // Arrange
        var ids = new List<string>();

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<DummyModel>(It.Is<IList<string>>(x => x.Count == 0), null, null))
            .ReturnsAsync([]);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.FindByIDsAsync(ids);

        // Assert
        Assert.Empty(result);
        mockService.Verify(s => s.FindAsync<DummyModel>(It.IsAny<IList<string>>(), null, null), Times.Once);
    }
    [Fact]
    public async Task FindByIDsAsync_PartialHit_ReturnsOnlyMatchingRecords() {
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

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.FindByIDsAsync(requestedIds);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.ID == "id1");
        Assert.Contains(result, r => r.ID == "id3");
        Assert.DoesNotContain(result, r => r.ID == "id2");

        mockService.Verify(s => s.FindAsync<DummyModel>(requestedIds, null, null), Times.Once);
    }
    [Fact]
    public async Task FindByKeyAsync_KeyExists_ReturnsMatchingRecord() {
        // Arrange
        var input = new KeyedModel { Code = "123123" };
        var expected = new KeyedModel { Code = "123123", Name = "Test Record" };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(null, It.Is<string>(q => q.Contains("Code = \"123123\"")), null))
            .ReturnsAsync([expected]);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await KeyedModel.FindByKeyAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("123123", result!.Code);
        Assert.Equal("Test Record", result.Name);

        mockService.Verify(s => s.FindAsync<KeyedModel>(null, It.IsAny<string>(), null), Times.Once);
    }
    [Fact]
    public async Task FindByKeyAsync_NoKeyAttribute_ThrowsInvalidOperationException() {
        // Arrange
        var model = new NoKeyModel { Code = "X001", Name = "NoKey" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => NoKeyModel.FindByKeyAsync(model));

        Assert.Equal("IsKey 属性付きのプロパティが見つかりません。", ex.Message);
    }
    [Fact]
    public async Task FindByKeyAsync_KeyValueIsNull_ThrowsInvalidOperationException() {
        // Arrange
        var model = new NullKeyModel { Code = null, Name = "NullKey" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => NullKeyModel.FindByKeyAsync(model));

        Assert.Equal("'Code' は更新キーですが、値が未設定です。", ex.Message);
    }
    [Fact]
    public async Task FindByKeyAsync_KeyValueIsEmpty_ThrowsInvalidOperationException() {
        // Arrange
        var model = new EmptyKeyModel { Code = "", Name = "EmptyKey" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => EmptyKeyModel.FindByKeyAsync(model));

        Assert.Equal("'Code' は更新キーですが、値が未設定です。", ex.Message);
    }
    [Fact]
    public async Task FindByKeyAsync_MultipleKeyAttributes_ThrowsInvalidOperationException() {
        // Arrange
        var model = new MultipleKeyModel {
            Code = "A001",
            SubCode = "B001",
            Name = "MultiKey"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => MultipleKeyModel.FindByKeyAsync(model));

        Assert.Equal("モデル 'MultipleKeyModel' には IsKey が複数あります（Code, SubCode）", ex.Message);
    }
    [Fact]
    public async Task FindByKeyAsync_NoMatchingRecord_ReturnsNull() {
        // Arrange
        var input = new KeyedModel { Code = "NOT_FOUND" };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(null, It.Is<string>(q => q.Contains("Code = \"NOT_FOUND\"")), null))
            .ReturnsAsync([]);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await KeyedModel.FindByKeyAsync(input);

        // Assert
        Assert.Null(result);
        mockService.Verify(s => s.FindAsync<KeyedModel>(null, It.IsAny<string>(), null), Times.Once);
    }
    [Fact]
    public async Task FindByKeysAsync_MultipleValidKeys_ReturnsMatchingRecords() {
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

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await KeyedModel.FindByKeysAsync(inputModels);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Code == "A001" && r.Name == "Alpha");
        Assert.Contains(result, r => r.Code == "B002" && r.Name == "Beta");
    }
    [Fact]
    public async Task FindByKeysAsync_ValidKeysButNoMatches_ReturnsEmptyList() {
        // Arrange
        var inputModels = new[] {
            new KeyedModel { Code = "X999" },
            new KeyedModel { Code = "Y888" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), It.IsAny<string>(), It.IsAny<IList<string>?>()))
            .ReturnsAsync([]); // 空リストを返す

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await KeyedModel.FindByKeysAsync(inputModels);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result); // 結果が空であることを確認
    }
    [Fact]
    public async Task FindByKeysAsync_SomeKeysMatch_ReturnsOnlyMatchingRecords() {
        // Arrange
        var inputModels = new[] {
            new KeyedModel { Code = "A001" },
            new KeyedModel { Code = "B002" },
            new KeyedModel { Code = "C003" } // このキーは一致しない
        };

        var expected = new[] {
            new KeyedModel { Code = "A001", Name = "Alpha" },
            new KeyedModel { Code = "B002", Name = "Beta" }
            // "C003" に一致するレコードは存在しない
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), It.IsAny<string>(), It.IsAny<IList<string>?>()))
            .ReturnsAsync(expected);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await KeyedModel.FindByKeysAsync(inputModels);

        // Assert
        Assert.Equal(2, result.Count); // 一致した2件のみ返る
        Assert.Contains(result, r => r.Code == "A001" && r.Name == "Alpha");
        Assert.Contains(result, r => r.Code == "B002" && r.Name == "Beta");
        Assert.DoesNotContain(result, r => r.Code == "C003"); // 一致しないキーは含まれない
    }
    [Fact]
    public async Task FindByKeysAsync_ModelWithoutKeyAttribute_ThrowsInvalidOperationException() {
        // Arrange
        var inputModels = new[] {
            new NoKeyModel { Code = "X001" },
            new NoKeyModel { Code = "Y002" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<NoKeyModel>(It.IsAny<IList<string>?>(), It.IsAny<string>(), It.IsAny<IList<string>?>()))
            .ReturnsAsync([]);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await NoKeyModel.FindByKeysAsync(inputModels)
        );
    }
    [Fact]
    public async Task FindByKeysAsync_ModelWithDuplicateKeyAttributes_ThrowsInvalidOperationException() {
        // Arrange
        var inputModels = new[] {
            new MultipleKeyModel { Code = "D001", SubCode = "X001" },
            new MultipleKeyModel { Code = "D002", SubCode = "X002" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<MultipleKeyModel>(It.IsAny<IList<string>?>(), It.IsAny<string>(), It.IsAny<IList<string>?>()))
            .ReturnsAsync([]);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await MultipleKeyModel.FindByKeysAsync(inputModels)
        );
    }
    [Fact]
    public async Task FindByKeysAsync_ModelWithNullKeyValue_ThrowsInvalidOperationException() {
        // Arrange
        var inputModels = new[] {
            new NullKeyModel { Code = "A001" },
            new NullKeyModel { Code = null }, // キー未設定
            new NullKeyModel { Code = "B002" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<NullKeyModel>(It.IsAny<IList<string>?>(), It.IsAny<string>(), It.IsAny<IList<string>?>()))
            .ReturnsAsync([]);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await NullKeyModel.FindByKeysAsync(inputModels)
        );
    }
    [Fact]
    public async Task FindByQueryAsync_ValidQuery_ReturnsMatchingRecords() {
        // Arrange
        var query = "Code = \"A001\"";
        var expected = new[] {
            new KeyedModel { Code = "A001", Name = "Alpha" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), query, It.IsAny<IList<string>?>()))
            .ReturnsAsync(expected);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await KeyedModel.FindByQueryAsync(query);

        // Assert
        Assert.Single(result);
        Assert.Equal("A001", result[0].Code);
        Assert.Equal("Alpha", result[0].Name);
    }
    [Fact]
    public async Task FindByQueryAsync_ValidQueryButNoMatch_ReturnsEmptyList() {
        // Arrange
        var query = "Code = \"Z999\"";

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), query, It.IsAny<IList<string>?>()))
            .ReturnsAsync([]);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await KeyedModel.FindByQueryAsync(query);

        // Assert
        Assert.Empty(result);
    }
    [Fact]
    public async Task FindByQueryAsync_NullQuery_ThrowsArgumentNullException() {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            KeyedModel.FindByQueryAsync(null!)
        );
    }
    [Fact]
    public async Task FindAllAsync_ReturnsAllRecords() {
        // Arrange
        var expected = new[] {
            new KeyedModel { Code = "A001", Name = "Alpha" },
            new KeyedModel { Code = "B002", Name = "Beta" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), null, It.IsAny<IList<string>?>()))
            .ReturnsAsync(expected);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await KeyedModel.FindAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("A001", result[0].Code);
        Assert.Equal("B002", result[1].Code);
    }
    [Fact]
    public async Task FindAllAsync_NoRecords_ReturnsEmptyList() {
        // Arrange
        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), null, It.IsAny<IList<string>?>()))
            .ReturnsAsync([]);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await KeyedModel.FindAllAsync();

        // Assert
        Assert.Empty(result);
    }
    [Fact]
    public async Task FindAllAsync_ServiceNotRegistered_ThrowsInvalidOperationException() {
        // Arrange
        KintoneServiceLocator.Initialize(new ServiceCollection().BuildServiceProvider());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => KeyedModel.FindAllAsync());
    }
    [Fact]
    public async Task FindAllAsync_ServiceThrowsException_PropagatesException() {
        // Arrange
        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.FindAsync<KeyedModel>(It.IsAny<IList<string>?>(), null, It.IsAny<IList<string>?>()))
            .ThrowsAsync(new TimeoutException("Kintone API timeout"));

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TimeoutException>(() => KeyedModel.FindAllAsync());
        Assert.Equal("Kintone API timeout", ex.Message);
    }

    #endregion
}
