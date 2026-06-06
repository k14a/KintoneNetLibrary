using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Entities;

/// <summary>
/// KintoneModelBase の DeleteAsync メソッドと関連する一括削除メソッドのテストクラス。
/// </summary>
public class KintoneModelBaseDeleteAsyncTests {
    /// <summary>
    /// テスト用のダミーモデルクラス。KintoneModelBase を継承し、必要なプロパティを実装している。
    /// </summary>
    public class DummyModel : KintoneModelBase<DummyModel> {
        public override int AppID { get; init; }
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(fieldCode: "FieldA", fieldType: KintoneFieldType.SingleLineText)]
        public string FieldA { get; set; } = string.Empty;
    }

    #region <<Test methods>>
    /// <summary>
    /// 有効なモデルを削除するテスト。DeleteAsync メソッドが IKintoneModelCrudService の DeleteAsync を正しく呼び出し、成功結果を返すことを検証する。
    /// </summary>
    [Fact]
    public async Task DeleteAsyncValidModelCallsServiceAndReturnsResult() {
        // Arrange
        var model = new DummyModel { RecordID = "1000", FieldA = "ToDelete" };

        var expectedResult = new KintoneDeleteResult {
            Succeeded = [model.RecordID]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync(It.Is<IList<DummyModel>>(list => list.Count == 1 && list[0] == model), true))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await model.DeleteAsync(mockService.Object);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.DeleteAsync(It.Is<IList<DummyModel>>(list => list.Count == 1), true), Times.Once);
    }

    /// <summary>
    /// 存在確認を行わずにモデルを削除するテスト。DeleteAsync メソッドが IKintoneModelCrudService の DeleteAsync を validateExistence フラグを false にして呼び出すことを検証する。
    /// </summary>
    [Fact]
    public async Task DeleteAsyncWithValidateExistenceFalseCallsServiceWithFlag() {
        // Arrange
        var model = new DummyModel { RecordID = "1001", FieldA = "ToDelete" };

        var expectedResult = new KintoneDeleteResult {
            Succeeded = [model.RecordID]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync(It.Is<IList<DummyModel>>(list => list.Count == 1 && list[0] == model), false))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await model.DeleteAsync(mockService.Object, validateExistence: false);

        // Assert
        Assert.Single(result.Succeeded);
        mockService.Verify(s => s.DeleteAsync(It.Is<IList<DummyModel>>(list => list.Count == 1), false), Times.Once);
    }

    /// <summary>
    /// 存在しないモデルを削除しようとした場合のテスト。DeleteAsync メソッドが IKintoneModelCrudService の DeleteAsync を呼び出し、失敗結果を返すことを検証する。
    /// </summary>
    [Fact]
    public async Task DeleteAsyncDeleteFailsReturnsFailureResult() {
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

        // Act
        var result = await model.DeleteAsync(mockService.Object);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.Equal("Record not found", result.Failed.First().ErrorMessage);
        Assert.True(result.HasFailures);
    }

    /// <summary>
    /// 複数の有効なモデルを削除するテスト。DeleteBulkAsync メソッドが IKintoneModelCrudService の DeleteAsync を正しく呼び出し、成功結果を返すことを検証する。
    /// </summary>
    [Fact]
    public async Task DeleteBulkAsyncWithTwoValidModelsReturnsSucceededResult() {
        // Arrange
        var models = new List<DummyModel> {
            new() { RecordID = "1003", FieldA = "Retry1" },
            new() { RecordID = "1004", FieldA = "Retry2" }
        };

        var expectedResult = new KintoneDeleteResult {
            Succeeded = models.Select(m => m.RecordID!).ToList()
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync(It.IsAny<IList<DummyModel>>(), It.IsAny<bool>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await DummyModel.DeleteBulkAsync(mockService.Object, models, false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);

        mockService.Verify(s => s.DeleteAsync(It.IsAny<IList<DummyModel>>(), It.IsAny<bool>()), Times.Once);
    }

    /// <summary>
    /// 複数のモデルを削除するテスト。DeleteBulkAsync メソッドが IKintoneModelCrudService の DeleteAsync を呼び出し、部分的に成功し部分的に失敗する結果を返すことを検証する。
    /// </summary>
    [Fact]
    public async Task DeleteBulkAsyncWithThreeModelsTwoSucceededOneFailedReturnsPartialResult() {
        // Arrange
        var models = new List<DummyModel> {
            new() { RecordID = "1003", FieldA = "Retry1" },
            new() { RecordID = "1004", FieldA = "Retry2" },
            new() { RecordID = "9999", FieldA = "NonExistent" }
        };

        var expectedResult = new KintoneDeleteResult {
            Succeeded = ["1003", "1004"],
            Failed = [new() { ID = "9999", ErrorMessage = "NonExistent", Reason = KintoneDeleteFailureReason.RecordNotFound }]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync(It.IsAny<IList<DummyModel>>(), It.IsAny<bool>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await DummyModel.DeleteBulkAsync(mockService.Object, models, validateExistence: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);

        mockService.Verify(s => s.DeleteAsync(It.IsAny<IList<DummyModel>>(), true), Times.Once);
    }

    /// <summary>
    /// 複数の有効なIDを削除するテスト。DeleteBulkAsync メソッドが IKintoneModelCrudService の DeleteAsync を正しく呼び出し、成功結果を返すことを検証する。
    /// </summary>
    [Fact]
    public async Task DeleteBulkAsyncWithTwoValidIdsReturnsSucceededResult() {
        // Arrange
        var ids = new List<string> { "1003", "1004" };

        var expectedResult = new KintoneDeleteResult {
            Succeeded = ids
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(ids, false))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await DummyModel.DeleteBulkAsync(mockService.Object, ids, validateExistence: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);

        mockService.Verify(s => s.DeleteAsync<DummyModel>(ids, false), Times.Once);
    }

    /// <summary>
    /// 複数のIDを削除するテスト。DeleteBulkAsync メソッドが IKintoneModelCrudService の DeleteAsync を呼び出し、部分的に成功し部分的に失敗する結果を返すことを検証する。
    /// </summary>
    [Fact]
    public async Task DeleteBulkAsyncWithThreeIdsTwoSucceededOneFailedReturnsPartialResult() {
        // Arrange
        var ids = new List<string> { "1003", "1004", "9999" };

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

        // Act
        var result = await DummyModel.DeleteBulkAsync(mockService.Object, ids, validateExistence: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);

        mockService.Verify(s => s.DeleteAsync<DummyModel>(ids, true), Times.Once);
    }

    /// <summary>
    /// 複数の有効なIDを削除するテスト。DeleteSingleAsync メソッドが IKintoneModelCrudService の DeleteAsync を正しく呼び出し、成功結果を返すことを検証する。
    /// </summary>
    [Fact]
    public async Task DeleteSingleAsyncWithTwoValidIdsReturnsSucceededResult() {
        // Arrange
        var ids = new List<string> { "1003", "1004" };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1003" })), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1003"] });
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1004" })), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1004"] });

        // Act
        var result = await DummyModel.DeleteSingleAsync(mockService.Object, ids, validateExistence: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);

        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1003" })), true), Times.Once);
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1004" })), true), Times.Once);
    }

    /// <summary>
    /// 複数のIDを削除するテスト。DeleteSingleAsync メソッドが IKintoneModelCrudService の DeleteAsync を呼び出し、部分的に成功し部分的に失敗する結果を返すことを検証する。
    /// </summary>
    [Fact]
    public async Task DeleteSingleAsyncWithThreeModelsTwoSucceededOneFailedReturnsPartialResult() {
        // Arrange
        var models = new List<DummyModel> {
            new() { RecordID = "1003", FieldA = "Retry1" },
            new() { RecordID = "1004", FieldA = "Retry2" },
            new() { RecordID = "9999", FieldA = "NonExistent" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1003"), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1003"] });
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1004"), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1004"] });
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

        // Act
        var result = await DummyModel.DeleteSingleAsync(mockService.Object, models, validateExistence: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);

        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1003"), true), Times.Once);
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1004"), true), Times.Once);
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "9999"), true), Times.Once);
    }

    /// <summary>
    /// 複数の有効なモデルを削除するテスト。DeleteSingleAsync メソッドが IKintoneModelCrudService の DeleteAsync を正しく呼び出し、成功結果を返すことを検証する。
    /// </summary>
    [Fact]
    public async Task DeleteSingleAsyncWithTwoValidModelsReturnsSucceededResult() {
        // Arrange
        var models = new List<DummyModel> {
            new() { RecordID = "1003", FieldA = "Retry1" },
            new() { RecordID = "1004", FieldA = "Retry2" }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1003"), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1003"] });
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1004"), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1004"] });

        // Act
        var result = await DummyModel.DeleteSingleAsync(mockService.Object, models, validateExistence: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);

        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1003"), true), Times.Once);
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<DummyModel>>(x => x.Count == 1 && x[0].RecordID == "1004"), true), Times.Once);
    }

    /// <summary>
    /// 複数のIDを削除するテスト。DeleteSingleAsync メソッドが IKintoneModelCrudService の DeleteAsync を呼び出し、部分的に成功し部分的に失敗する結果を返すことを検証する。
    /// </summary>
    [Fact]
    public async Task DeleteSingleAsyncWithThreeIdsTwoSucceededOneFailedReturnsPartialResult() {
        // Arrange
        var ids = new List<string> { "1003", "1004", "9999" };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1003" })), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1003"] });
        mockService
            .Setup(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1004" })), true))
            .ReturnsAsync(new KintoneDeleteResult { Succeeded = ["1004"] });
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

        // Act
        var result = await DummyModel.DeleteSingleAsync(mockService.Object, ids, validateExistence: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);

        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1003" })), true), Times.Once);
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "1004" })), true), Times.Once);
        mockService.Verify(s => s.DeleteAsync<DummyModel>(It.Is<IList<string>>(x => x.SequenceEqual(new[] { "9999" })), true), Times.Once);
    }

    #endregion
}
