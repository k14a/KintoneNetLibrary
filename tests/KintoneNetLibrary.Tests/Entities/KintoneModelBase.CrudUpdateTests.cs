using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Entities;

public class KintoneModelBase_UpdateTests {
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
    public async Task UpdateAsync_Success_ReturnsExpectedResult() {
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
    [Fact]
    public async Task UpdateAsync_Failure_ReturnsFailedResult() {
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
    [Fact]
    public async Task UpdateAsync_WithRetryFlagTrue_PassesFlagToService() {
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
    [Fact]
    public async Task UpdateBulkAsync_AllSuccess_ReturnsAllSucceeded() {
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
    [Fact]
    public async Task UpdateBulkAsync_PartialFailure_ReturnsCorrectSucceededAndFailed() {
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
    [Fact]
    public async Task UpdateBulkAsync_WithRetryFlagTrue_PassesFlagToService() {
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
    [Fact]
    public async Task UpdateBulkAsync_EmptyList_ReturnsEmptyResult() {
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
    [Fact]
    public async Task UpdateSingleAsync_AllSuccess_ReturnsAllSucceeded() {
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
    [Fact]
    public async Task UpdateSingleAsync_PartialFailure_ReturnsCorrectSucceededAndFailed() {
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
    [Fact]
    public async Task UpdateSingleAsync_WithRetryFlagTrue_CallsServiceWithRetry() {
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
    [Fact]
    public async Task UpdateSingleAsync_EmptyList_ReturnsEmptyResult() {
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
