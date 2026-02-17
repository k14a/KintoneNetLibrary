using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Entities;

/// <summary>
/// KintoneModelBaseのUpdateAsync、UpdateBulkAsync、UpdateSingleAsyncメソッドのテストクラス。
/// </summary>
public class KintoneModelBaseUpdateTests {
    /// <summary>
    /// テスト用のダミーモデルクラス。KintoneModelBaseを継承し、必要なプロパティとフィールドを定義しています。
    /// </summary>
    public class DummyModel : KintoneModelBase<DummyModel> {
        public override int AppID { get; init; }
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(fieldCode: "FieldA", fieldType: KintoneFieldType.SingleLineText)]
        public string FieldA { get; set; } = string.Empty;
        [KintoneItem(fieldCode: "FieldB", fieldType: KintoneFieldType.Number)]
        public int FieldB { get; set; }
    }

    #region <<Test methods>>
    /// <summary>
    /// UpdateAsyncが成功した場合、期待される結果を返すことをテストします。モックサービスを使用して、UpdateAsyncが正しい引数で呼び出され、成功の結果が返されることを検証します。
    /// </summary>
    [Fact]
    public async Task UpdateAsyncSuccessReturnsExpectedResult() {
        // Arrange
        var model = new DummyModel { FieldA = "Updated", FieldB = 100 };

        var expectedResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), false))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.UpdateAsync();

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), false), Times.Once);
    }

    /// <summary>
    /// UpdateAsyncが失敗した場合、期待される失敗の結果を返すことをテストします。モックサービスを使用して、UpdateAsyncが正しい引数で呼び出され、失敗の結果が返されることを検証します。
    /// </summary>
    [Fact]
    public async Task UpdateAsyncFailureReturnsFailedResult() {
        // Arrange
        var model = new DummyModel { FieldA = "NG", FieldB = -999 };

        var expectedResult = new KintoneWriteResult<DummyModel> {
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model,
                    ErrorMessage = "Invalid FieldB"
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), false))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.UpdateAsync();

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Equal("Invalid FieldB", result.Failed.First().ErrorMessage);
        mockService.Verify(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), false), Times.Once);
    }

    /// <summary>
    /// UpdateAsyncでenableSingleRetryOnErrorフラグがtrueの場合、サービスに正しい引数で呼び出されることをテストします。モックサービスを使用して、UpdateAsyncがenableSingleRetryOnErrorフラグをtrueで呼び出されることを検証します。
    /// </summary>
    [Fact]
    public async Task UpdateAsyncWithRetryFlagTruePassesFlagToService() {
        // Arrange
        var model = new DummyModel { FieldA = "Retry", FieldB = 123 };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true))
            .ReturnsAsync(new KintoneWriteResult<DummyModel> { Succeeded = [model] });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.UpdateAsync(enableSingleRetryOnError: true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true), Times.Once);
    }

    /// <summary>
    /// UpdateBulkAsyncがすべて成功した場合、期待される成功の結果を返すことをテストします。モックサービスを使用して、UpdateAsyncが正しい引数で呼び出され、すべて成功の結果が返されることを検証します。
    /// </summary>
    [Fact]
    public async Task UpdateBulkAsyncAllSuccessReturnsAllSucceeded() {
        // Arrange
        var model1 = new DummyModel { FieldA = "Update1", FieldB = 1 };
        var model2 = new DummyModel { FieldA = "Update2", FieldB = 2 };
        var models = new List<DummyModel> { model1, model2 };

        var expectedResult = new KintoneWriteResult<DummyModel> {
            Succeeded = models
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.SequenceEqual(models)), false))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.UpdateBulkAsync(models);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.SequenceEqual(models)), false), Times.Once);
    }

    /// <summary>
    /// UpdateBulkAsyncで一部のレコードが失敗した場合、期待される成功と失敗の結果を返すことをテストします。モックサービスを使用して、UpdateAsyncが正しい引数で呼び出され、一部成功と一部失敗の結果が返されることを検証します。
    /// </summary>
    [Fact]
    public async Task UpdateBulkAsyncPartialFailureReturnsCorrectSucceededAndFailed() {
        // Arrange
        var model1 = new DummyModel { FieldA = "OK1", FieldB = 1 };
        var model2 = new DummyModel { FieldA = "NG", FieldB = -999 };
        var model3 = new DummyModel { FieldA = "OK2", FieldB = 2 };
        var models = new List<DummyModel> { model1, model2, model3 };

        var expectedResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model1, model3],
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model2,
                    ErrorMessage = "Invalid FieldB"
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.SequenceEqual(models)), false))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.UpdateBulkAsync(models);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Contains(model2, result.Failed.Select(f => f.Record));
        Assert.Equal("Invalid FieldB", result.Failed.First().ErrorMessage);

        mockService.Verify(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.SequenceEqual(models)), false), Times.Once);
    }

    /// <summary>
    /// UpdateBulkAsyncでenableSingleRetryOnErrorフラグがtrueの場合、サービスに正しい引数で呼び出されることをテストします。モックサービスを使用して、UpdateAsyncがenableSingleRetryOnErrorフラグをtrueで呼び出されることを検証します。
    /// </summary>
    [Fact]
    public async Task UpdateBulkAsyncWithRetryFlagTruePassesFlagToService() {
        // Arrange
        var models = new List<DummyModel> {
            new() { FieldA = "Retry1", FieldB = 10 },
            new() { FieldA = "Retry2", FieldB = 20 }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.SequenceEqual(models)), true))
            .ReturnsAsync(new KintoneWriteResult<DummyModel> { Succeeded = models });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.UpdateBulkAsync(models, enableSingleRetryOnError: true);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.SequenceEqual(models)), true), Times.Once);
    }

    /// <summary>
    /// UpdateBulkAsyncで空のリストを渡した場合、空の結果を返すことをテストします。モックサービスを使用して、UpdateAsyncが空のリストで呼び出されることを検証し、結果が空であることを確認します。
    /// </summary>
    [Fact]
    public async Task UpdateBulkAsyncEmptyListReturnsEmptyResult() {
        // Arrange
        var models = new List<DummyModel>();

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 0), false))
            .ReturnsAsync(new KintoneWriteResult<DummyModel>());

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.UpdateBulkAsync(models);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 0), false), Times.Once);
    }

    /// <summary>
    /// UpdateSingleAsyncがすべて成功した場合、期待される成功の結果を返すことをテストします。モックサービスを使用して、UpdateAsyncが正しい引数で呼び出され、すべて成功の結果が返されることを検証します。
    /// </summary>
    [Fact]
    public async Task UpdateSingleAsyncAllSuccessReturnsAllSucceeded() {
        // Arrange
        var model1 = new DummyModel { FieldA = "Update1", FieldB = 1 };
        var model2 = new DummyModel { FieldA = "Update2", FieldB = 2 };
        var models = new List<DummyModel> { model1, model2 };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model1), false))
            .ReturnsAsync(new KintoneWriteResult<DummyModel> { Succeeded = [model1] });
        mockService
            .Setup(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model2), false))
            .ReturnsAsync(new KintoneWriteResult<DummyModel> { Succeeded = [model2] });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.UpdateSingleAsync(models);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1), false), Times.Exactly(2));
    }

    /// <summary>
    /// UpdateSingleAsyncで一部のレコードが失敗した場合、期待される成功と失敗の結果を返すことをテストします。モックサービスを使用して、UpdateAsyncが正しい引数で呼び出され、一部成功と一部失敗の結果が返されることを検証します。
    /// </summary>
    [Fact]
    public async Task UpdateSingleAsyncPartialFailureReturnsCorrectSucceededAndFailed() {
        // Arrange
        var model1 = new DummyModel { FieldA = "OK1", FieldB = 1 };
        var model2 = new DummyModel { FieldA = "NG", FieldB = -999 };
        var model3 = new DummyModel { FieldA = "OK2", FieldB = 2 };
        var models = new List<DummyModel> { model1, model2, model3 };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model1), false))
            .ReturnsAsync(new KintoneWriteResult<DummyModel> { Succeeded = [model1] });
        mockService
            .Setup(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model2), false))
            .ReturnsAsync(new KintoneWriteResult<DummyModel> {
                Failed = [
                    new KintoneWriteFailure<DummyModel> {
                        Record = model2,
                        ErrorMessage = "Invalid FieldB"
                    }
                ]
            });
        mockService
            .Setup(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model3), false))
            .ReturnsAsync(new KintoneWriteResult<DummyModel> { Succeeded = [model3] });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.UpdateSingleAsync(models);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Contains(model2, result.Failed.Select(f => f.Record));
        Assert.Equal("Invalid FieldB", result.Failed.First().ErrorMessage);

        mockService.Verify(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1), false), Times.Exactly(3));
    }

    /// <summary>
    /// UpdateSingleAsyncでenableSingleRetryOnErrorフラグがtrueの場合、サービスに正しい引数で呼び出されることをテストします。モックサービスを使用して、UpdateAsyncがenableSingleRetryOnErrorフラグをtrueで呼び出されることを検証します。
    /// </summary>
    [Fact]
    public async Task UpdateSingleAsyncWithRetryFlagTrueCallsServiceWithRetry() {
        // Arrange
        var model1 = new DummyModel { FieldA = "Retry1", FieldB = 10 };
        var model2 = new DummyModel { FieldA = "Retry2", FieldB = 20 };
        var models = new List<DummyModel> { model1, model2 };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1), true))
            .ReturnsAsync((IList<DummyModel> arr, bool _) => new KintoneWriteResult<DummyModel> { Succeeded = [arr[0]] });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.UpdateSingleAsync(models, enableSingleRetryOnError: true);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.UpdateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1), true), Times.Exactly(2));
    }

    /// <summary>
    /// UpdateSingleAsyncで空のリストを渡した場合、空の結果を返すことをテストします。モックサービスを使用して、UpdateAsyncが空のリストで呼び出されることを検証し、結果が空であることを確認します。1j
    /// </summary>
    [Fact]
    public async Task UpdateSingleAsyncEmptyListReturnsEmptyResult() {
        // Arrange
        var models = new List<DummyModel>();

        var mockService = new Mock<IKintoneModelCrudService>();

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.UpdateSingleAsync(models);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.UpdateAsync(It.IsAny<IList<DummyModel>>(), It.IsAny<bool>()), Times.Never);
    }
    #endregion
}
