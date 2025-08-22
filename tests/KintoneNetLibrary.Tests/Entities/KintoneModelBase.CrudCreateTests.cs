using Castle.Core.Logging;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Entities;

public class KintoneModelBase_CreateTests {
    public class DummyModel : KintoneModelBase<DummyModel> {
        public override int AppID { get; init; }
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(fieldCode: "FieldA", fieldType: KintoneFieldType.SingleLineText)]
        public string FieldA { get; set; } = string.Empty;
        [KintoneItem(fieldCode: "FieldB", fieldType: KintoneFieldType.Number)]
        public int FieldB { get; set; }
    }

    #region <<Test methods>>
    [Fact]
    public async Task CreateAsync_CallsServiceWithCorrectArguments_ReturnsExpectedResult() {
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
    [Fact]
    public async Task CreateAsync_WithRetryFlagTrue_PassesFlagToService() {
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
    [Fact]
    public async Task CreateBulkAsync_CallsServiceWithCorrectArguments_ReturnsExpectedResult() {
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
    [Fact]
    public async Task CreateBulkAsync_WithRetryFlagTrue_PassesFlagToService() {
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
    [Fact]
    public async Task CreateBulkAsync_WithEmptyList_ReturnsEmptyResult() {
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
    [Fact]
    public async Task CreateBulkAsync_PartialSuccess_ReturnsCorrectSucceededAndFailedResults() {
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
    [Fact]
    public async Task CreateSingleAsync_MultipleModels_AllSuccess_ReturnsAllSucceeded() {
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
    [Fact]
    public async Task CreateSingleAsync_MultipleModels_PartialFailure_ReturnsCorrectSucceededAndFailed() {
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
    [Fact]
    public async Task CreateSingleAsync_WithRetryFlagTrue_CallsServiceWithRetry() {
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
    [Fact]
    public async Task CreateSingleAsync_EmptyList_ReturnsEmptyResult() {
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