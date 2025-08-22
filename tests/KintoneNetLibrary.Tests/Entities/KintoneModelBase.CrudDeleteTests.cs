using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Entities;

public class KintoneModelBase_DeleteAsyncTests {
    public class DummyModel : KintoneModelBase<DummyModel> {
        public override int AppID { get; init; }
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(fieldCode: "FieldA", fieldType: KintoneFieldType.SingleLineText)]
        public string FieldA { get; set; } = string.Empty;
    }

    #region <<Test methods>>
    [Fact]
    public async Task DeleteAsync_ValidModel_CallsServiceAndReturnsResult() {
        // Arrange
        var model = new DummyModel { RecordID = "1000", FieldA = "ToDelete" };

        var expectedResult = new KintoneDeleteResult {
            Succeeded = [model.RecordID]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync(It.Is<IList<DummyModel>>(list => list.Count == 1 && list[0] == model), true))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.DeleteAsync();

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.DeleteAsync(It.Is<IList<DummyModel>>(list => list.Count == 1), true), Times.Once);
    }
    [Fact]
    public async Task DeleteAsync_WithValidateExistenceFalse_CallsServiceWithFlag() {
        // Arrange
        var model = new DummyModel { RecordID = "1001", FieldA = "ToDelete" };

        var expectedResult = new KintoneDeleteResult {
            Succeeded = [model.RecordID]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync(It.Is<IList<DummyModel>>(list => list.Count == 1 && list[0] == model), false))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.DeleteAsync(validateExistence: false);

        // Assert
        Assert.Single(result.Succeeded);
        mockService.Verify(s => s.DeleteAsync(It.Is<IList<DummyModel>>(list => list.Count == 1), false), Times.Once);
    }
    [Fact]
    public async Task DeleteAsync_DeleteFails_ReturnsFailureResult() {
        // Arrange
        var model = new DummyModel { RecordID = "1002", FieldA = "NG" };

        var expectedResult = new KintoneDeleteResult {
            Failed = [
                new KintoneDeleteFailure {
                    ID = model.RecordID,
                    ErrorMessage = "Record not found"
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync(It.Is<IList<DummyModel>>(list => list.Count == 1 && list[0] == model), true))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.DeleteAsync();

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.Equal("Record not found", result.Failed.First().ErrorMessage);
        Assert.True(result.HasFailures);
    }
    [Fact]
    public async Task DeleteBulkAsync_WithTwoValidModels_ReturnsSucceededResult() {
        // Arrange
        var models = new List<DummyModel> {
            new() { RecordID = "1003", FieldA = "Retry1" },
            new() { RecordID = "1004", FieldA = "Retry2" }
        };

        var expectedResult = new KintoneDeleteResult {
            Succeeded = models.Select(m => m.RecordID).ToList()
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync(It.IsAny<IList<DummyModel>>(), It.IsAny<bool>()))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Reset();
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.DeleteBulkAsync(models, false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);

        // Verify: モックが1回呼ばれたことを確認（同じ一致条件を使う）
        mockService.Verify(s => s.DeleteAsync(It.IsAny<IList<DummyModel>>(), It.IsAny<bool>()), Times.Once);
    }
    [Fact]
    public async Task DeleteBulkAsync_WithThreeModels_TwoSucceeded_OneFailed_ReturnsPartialResult() {
        // Arrange
        var models = new List<DummyModel> {
            new() { RecordID = "1003", FieldA = "Retry1" },
            new() { RecordID = "1004", FieldA = "Retry2" },
            new() { RecordID = "9999", FieldA = "NonExistent" } // 存在しないID
        };

        var expectedResult = new KintoneDeleteResult {
            Succeeded = ["1003", "1004"],
            Failed = [new() { ID = "9999", ErrorMessage = "NonExistent", Reason = KintoneDeleteFailureReason.RecordNotFound }]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync(It.IsAny<IList<DummyModel>>(), It.IsAny<bool>()))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Reset();
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.DeleteBulkAsync(models, validateExistence: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures); // 部分失敗を検出

        // Verify: モックが1回呼ばれたことを確認
        mockService.Verify(s => s.DeleteAsync(It.IsAny<IList<DummyModel>>(), true), Times.Once);
    }
    [Fact]
    public async Task DeleteBulkAsync_WithTwoValidIds_ReturnsSucceededResult() {
        // Arrange
        var ids = new List<string> { "1003", "1004" };

        var expectedResult = new KintoneDeleteResult {
            Succeeded = ids
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(ids, false))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Reset();
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.DeleteBulkAsync(ids, validateExistence: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);

        // Verify: モックが正しく呼ばれたことを確認
        mockService.Verify(s => s.DeleteAsync<DummyModel>(ids, false), Times.Once);
    }
    [Fact]
    public async Task DeleteBulkAsync_WithThreeIds_TwoSucceeded_OneFailed_ReturnsPartialResult() {
        // Arrange
        var ids = new List<string> { "1003", "1004", "9999" }; // "9999" は存在しないID

        var expectedResult = new KintoneDeleteResult {
            Succeeded = ["1003", "1004"],
            Failed = [
                new() {
                    ID = "9999",
                    ErrorMessage = "NonExistent",
                    Reason = KintoneDeleteFailureReason.RecordNotFound
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(ids, true))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Reset();
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.DeleteBulkAsync(ids, validateExistence: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures); // 部分失敗を検出

        // Verify: モックが1回呼ばれたことを確認
        mockService.Verify(s => s.DeleteAsync<DummyModel>(ids, true), Times.Once);
    }
    [Fact]
    public async Task DeleteSingleAsync_WithTwoValidIds_ReturnsSucceededResult() {
        // Arrange
        var ids = new List<string> { "1003", "1004" };

        var mockService = new Mock<IKintoneModelCrudService>();

        // 各IDに対して個別に成功レスポンスを返すよう設定
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1003" })), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1003"] });
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1004" })), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1004"] });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Reset();
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.DeleteSingleAsync(ids, validateExistence: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);

        // Verify: 各IDに対して1回ずつ呼ばれたことを確認
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1003" })), true), Times.Once);
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1004" })), true), Times.Once);
    }
    [Fact]
    public async Task DeleteSingleAsync_WithThreeModels_TwoSucceeded_OneFailed_ReturnsPartialResult() {
        // Arrange
        var models = new List<DummyModel> {
            new() { RecordID = "1003", FieldA = "Retry1" },
            new() { RecordID = "1004", FieldA = "Retry2" },
            new() { RecordID = "9999", FieldA = "NonExistent" } // 存在しないID
        };

        var mockService = new Mock<IKintoneModelCrudService>();

        // 成功レスポンス設定
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1003"), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1003"] });
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1004"), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1004"] });
        // 失敗レスポンス設定
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "9999"), true))
            .ReturnsAsync(new KintoneDeleteResult {
                Failed = [
                    new() {
                        ID = "9999",
                        ErrorMessage = "Record not found",
                        Reason = KintoneDeleteFailureReason.RecordNotFound
                    }
                ]
            });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Reset();
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.DeleteSingleAsync(models, validateExistence: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);

        // Verify: 各モデルに対して1回ずつ呼ばれたことを確認
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1003"), true), Times.Once);
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1004"), true), Times.Once);
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "9999"), true), Times.Once);
    }
    [Fact]
    public async Task DeleteSingleAsync_WithTwoValidModels_ReturnsSucceededResult() {
        // Arrange
        var models = new List<DummyModel> {
            new() { RecordID = "1003", FieldA = "Retry1" },
            new() { RecordID = "1004", FieldA = "Retry2" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();

        // 各モデルに対して個別に成功レスポンスを返すよう設定
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1003"), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1003"] });
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1004"), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1004"] });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Reset();
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.DeleteSingleAsync(models, validateExistence: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);

        // Verify: 各モデルに対して1回ずつ呼ばれたことを確認
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1003"), true), Times.Once);
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1004"), true), Times.Once);
    }
    [Fact]
    public async Task DeleteSingleAsync_WithThreeIds_TwoSucceeded_OneFailed_ReturnsPartialResult() {
        // Arrange
        var ids = new List<string> { "1003", "1004", "9999" }; // "9999" は存在しないID

        var mockService = new Mock<IKintoneModelCrudService>();

        // 成功レスポンス設定
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1003" })), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1003"] });
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1004" })), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1004"] });
        // 失敗レスポンス設定
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "9999" })), true))
            .ReturnsAsync(new KintoneDeleteResult {
                Failed = [
                    new() {
                        ID = "9999",
                        ErrorMessage = "Record not found",
                        Reason = KintoneDeleteFailureReason.RecordNotFound
                    }
                ]
            });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Reset();
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.DeleteSingleAsync(ids, validateExistence: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);

        // Verify: 各IDに対して1回ずつ呼ばれたことを確認
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1003" })), true), Times.Once);
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1004" })), true), Times.Once);
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "9999" })), true), Times.Once);
    }

    #endregion
}
