using Castle.Core.Logging;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Entities;

/// <summary>
/// KintoneModelBase の CreateAsync および CreateBulkAsync メソッドに関するユニットテストクラス。
/// </summary>
public class KintoneModelBaseCreateTests {
    /// <summary>
    /// テスト用のダミーモデルクラス。KintoneModelBase を継承し、必要なプロパティとフィールドを定義している。
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
    /// CreateAsync メソッドが IKintoneModelCrudService の CreateAsync メソッドを正しい引数で呼び出し、期待される結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task CreateAsyncCallsServiceWithCorrectArgumentsReturnsExpectedResult() {
        // Arrange
        var model = new DummyModel { FieldA = "DummyText", FieldB = 100 };
        var expectedResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1), false))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.CreateAsync();

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), false), Times.Once);
    }

    /// <summary>
    /// CreateAsync メソッドに enableSingleRetryOnError フラグが true の場合、IKintoneModelCrudService の CreateAsync メソッドに正しい引数で呼び出されることをテストする。
    /// </summary>
    [Fact]
    public async Task CreateAsyncWithRetryFlagTruePassesFlagToService() {
        // Arrange
        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.CreateAsync(It.IsAny<IList<DummyModel>>(), true))
            .ReturnsAsync(new KintoneWriteResult<DummyModel>());

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        var model = new DummyModel();

        // Act
        var result = await model.CreateAsync(enableSingleRetryOnError: true);

        // Assert
        mockService.Verify(s => s.CreateAsync(It.IsAny<IList<DummyModel>>(), true), Times.Once);
    }

    /// <summary>
    /// CreateAsync メソッドに空のモデルリストを渡した場合、IKintoneModelCrudService の CreateAsync メソッドが呼び出されず、空の結果が返されることをテストする。
    /// </summary>
    [Fact]
    public async Task CreateBulkAsyncCallsServiceWithCorrectArgumentsReturnsExpectedResult() {
        // Arrange
        var models = new List<DummyModel> {
            new() { FieldA = "Text1", FieldB = 1 },
            new() { FieldA = "Text2", FieldB = 2 }
        };

        var expectedResult = new KintoneWriteResult<DummyModel> {
            Succeeded = models
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 2), false))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.CreateBulkAsync(models);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.SequenceEqual(models)), false), Times.Once);
    }

    /// <summary>
    /// CreateBulkAsync メソッドに enableSingleRetryOnError フラグが true の場合、IKintoneModelCrudService の CreateAsync メソッドに正しい引数で呼び出されることをテストする。
    /// </summary>
    [Fact]
    public async Task CreateBulkAsyncWithRetryFlagTruePassesFlagToService() {
        // Arrange
        var models = new List<DummyModel> {
            new() { FieldA = "RetryText", FieldB = 99 }
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.CreateAsync(It.IsAny<IList<DummyModel>>(), true))
            .ReturnsAsync(new KintoneWriteResult<DummyModel>());

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.CreateBulkAsync(models, enableSingleRetryOnError: true);

        // Assert
        mockService.Verify(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.SequenceEqual(models)), true), Times.Once);
    }

    /// <summary>
    /// CreateBulkAsync メソッドに空のモデルリストを渡した場合、IKintoneModelCrudService の CreateAsync メソッドが呼び出されず、空の結果が返されることをテストする。
    /// </summary>
    [Fact]
    public async Task CreateBulkAsyncWithEmptyListReturnsEmptyResult() {
        // Arrange
        var models = new List<DummyModel>();

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 0), false))
            .ReturnsAsync(new KintoneWriteResult<DummyModel>());

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.CreateBulkAsync(models);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 0), false), Times.Once);
    }

    /// <summary>
    /// CreateBulkAsync メソッドが部分的に成功した場合、正しい成功と失敗の結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task CreateBulkAsyncPartialSuccessReturnsCorrectSucceededAndFailedResults() {
        // Arrange
        var model1 = new DummyModel { FieldA = "OK1", FieldB = 1 };
        var model2 = new DummyModel { FieldA = "OK2", FieldB = 2 };
        var model3 = new DummyModel { FieldA = "NG", FieldB = -999 }; // 失敗を想定

        var models = new List<DummyModel> { model1, model2, model3 };

        var expectedResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model1, model2],
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model3,
                    ErrorMessage = "Invalid value for FieldB"
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 3), false))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.CreateBulkAsync(models);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Contains(model3, result.Failed.Select(f => f.Record));
        Assert.Equal("Invalid value for FieldB", result.Failed.First().ErrorMessage);

        mockService.Verify(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.SequenceEqual(models)), false), Times.Once);
    }

    /// <summary>
    /// CreateSingleAsync メソッドが複数のモデルを処理する際、すべてのモデルが成功した場合に正しい結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task CreateSingleAsyncMultipleModelsAllSuccessReturnsAllSucceeded() {
        // Arrange
        var model1 = new DummyModel { FieldA = "Text1", FieldB = 1 };
        var model2 = new DummyModel { FieldA = "Text2", FieldB = 2 };
        var models = new List<DummyModel> { model1, model2 };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model1), false))
            .ReturnsAsync(new KintoneWriteResult<DummyModel> { Succeeded = [model1] });
        mockService
            .Setup(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model2), false))
            .ReturnsAsync(new KintoneWriteResult<DummyModel> { Succeeded = [model2] });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.CreateSingleAsync(models);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model1), false), Times.Once);
        mockService.Verify(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model2), false), Times.Once);
    }

    /// <summary>
    /// CreateSingleAsync メソッドが複数のモデルを処理する際、部分的に成功した場合に正しい成功と失敗の結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task CreateSingleAsyncMultipleModelsPartialFailureReturnsCorrectSucceededAndFailed() {
        // Arrange
        var model1 = new DummyModel { FieldA = "OK1", FieldB = 1 };
        var model2 = new DummyModel { FieldA = "NG", FieldB = -999 };
        var model3 = new DummyModel { FieldA = "OK2", FieldB = 2 };
        var models = new List<DummyModel> { model1, model2, model3 };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model1), false))
            .ReturnsAsync(new KintoneWriteResult<DummyModel> { Succeeded = [model1] });
        mockService
            .Setup(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model2), false))
            .ReturnsAsync(new KintoneWriteResult<DummyModel> {
                Failed = [
                    new KintoneWriteFailure<DummyModel> {
                        Record = model2,
                        ErrorMessage = "Invalid value for FieldB"
                    }
                ]
            });
        mockService
            .Setup(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model3), false))
            .ReturnsAsync(new KintoneWriteResult<DummyModel> { Succeeded = [model3] });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.CreateSingleAsync(models);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Contains(model2, result.Failed.Select(f => f.Record));
        Assert.Equal("Invalid value for FieldB", result.Failed.First().ErrorMessage);

        mockService.Verify(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1), false), Times.Exactly(3));
    }

    /// <summary>
    /// CreateSingleAsync メソッドに enableSingleRetryOnError フラグが true の場合、失敗したモデルに対して再試行が行われ、最終的にすべてのモデルが成功することをテストする。
    /// </summary>
    [Fact]
    public async Task CreateSingleAsyncWithRetryFlagTrueCallsServiceWithRetry() {
        // Arrange
        var model1 = new DummyModel { FieldA = "Retry1", FieldB = 10 };
        var model2 = new DummyModel { FieldA = "Retry2", FieldB = 20 };
        var models = new List<DummyModel> { model1, model2 };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1), true))
            .ReturnsAsync((IList<DummyModel> arr, bool _) => new KintoneWriteResult<DummyModel> { Succeeded = [arr[0]] });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.CreateSingleAsync(models, enableSingleRetryOnError: true);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1), true), Times.Exactly(2));
    }

    /// <summary>
    /// CreateSingleAsync メソッドに空のモデルリストを渡した場合、IKintoneModelCrudService の CreateAsync メソッドが呼び出されず、空の結果が返されることをテストする。
    /// </summary>
    [Fact]
    public async Task CreateSingleAsyncEmptyListReturnsEmptyResult() {
        // Arrange
        var models = new List<DummyModel>();

        var mockService = new Mock<IKintoneModelCrudService>();

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.CreateSingleAsync(models);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.CreateAsync(It.IsAny<IList<DummyModel>>(), It.IsAny<bool>()), Times.Never);
    }
    #endregion
}