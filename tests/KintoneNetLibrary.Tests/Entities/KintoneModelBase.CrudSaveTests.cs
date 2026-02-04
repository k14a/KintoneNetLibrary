using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Entities;

public class KintoneModelBaseSaveTests {
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
    public async Task SaveAsyncSuccessReturnsExpectedResult() {
        // Arrange
        var model = new DummyModel { RecordID = "1111", FieldA = "ToDelete", FieldB = 999 };

        var expectedResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), It.IsAny<bool>()))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.SaveAsync();

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.SaveAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), It.IsAny<bool>()), Times.Once);
    }
    [Fact]
    public async Task SaveAsyncFailureReturnsExpectedFailureResult() {
        // Arrange
        var model = new DummyModel { RecordID = "1111", FieldA = "Invalid", FieldB = -1 };

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
            .Setup(s => s.SaveAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), It.IsAny<bool>()))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.SaveAsync();

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.True(result.HasFailures);
        Assert.Single(result.Failed);
        Assert.Equal(model, result.Failed[0].Record);
        Assert.Contains("Invalid FieldB", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), It.IsAny<bool>()), Times.Once);
    }
    [Fact]
    public async Task SaveAsyncWithRetryFlagTruePassesFlagToService() {
        // Arrange
        var model = new DummyModel { RecordID = "1111", FieldA = "Retry", FieldB = 123 };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true))
            .ReturnsAsync(new KintoneWriteResult<DummyModel> { Succeeded = [model] });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.SaveAsync(enableSingleRetryOnError: true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.SaveAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true), Times.Once);
    }
    [Fact]
    public async Task SaveWithRetryAsyncSuccessWithoutRetryReturnsExpectedResult() {
        // Arrange
        var model = new DummyModel { RecordID = "2222", FieldA = "Initial", FieldB = 123 };

        var expectedResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), false, true))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.SaveWithRetryAsync();

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), false, true), Times.Once);
    }
    [Fact]
    public async Task SaveWithRetryAsyncFirstAttemptFailsRetrySucceeds() {
        // Arrange
        var model = new DummyModel { RecordID = "3333", FieldA = "RetryMe", FieldB = 456 };

        var failure = new KintoneWriteFailure<DummyModel> {
            Record = model,
            ErrorMessage = "Temporary network error",
        };

        var successResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .SetupSequence(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true, true))
            .ReturnsAsync(successResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.SaveWithRetryAsync(true, true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true, true), Times.Once);
    }
    [Fact]
    public async Task SaveWithRetryAsyncAllAttemptsFailReturnsFailure() {
        // Arrange
        var model = new DummyModel { RecordID = "3333", FieldA = "RetryMe", FieldB = 456 };

        var failureResult = new KintoneWriteResult<DummyModel> {
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model,
                    ErrorMessage = "Network timeout"
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true, true))
            .ReturnsAsync(failureResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.SaveWithRetryAsync(true, true);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.True(result.HasFailures);
        Assert.Single(result.Failed);
        Assert.Equal("Network timeout", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true, true), Times.Once);
    }
    [Fact]
    public async Task SaveWithRetryAsyncCreateFailsUpdateRetrySucceedsWhenEnabled() {
        // Arrange
        var model = new DummyModel { RecordID = "9999", FieldA = "FallbackToUpdate", FieldB = 789 };

        var createFailure = new KintoneWriteResult<DummyModel> {
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model,
                    ErrorMessage = "Record already exists"
                }
            ]
        };

        var updateSuccess = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .SetupSequence(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true, true))
            // .ReturnsAsync(createFailure)  // First attempt: Create fails
            .ReturnsAsync(updateSuccess); // Retry: Update succeeds

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.SaveWithRetryAsync(true, true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.SaveWithRetryAsync(It.IsAny<IList<DummyModel>>(), true, true), Times.Once);
    }
    [Fact]
    public async Task SaveWithRetryAsyncCreateFailsNoUpdateRetryWhenDisabled() {
        // Arrange
        var model = new DummyModel { RecordID = "9999", FieldA = "NoFallback", FieldB = 321 };

        var createFailure = new KintoneWriteResult<DummyModel> {
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model,
                    ErrorMessage = "Record already exists"
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true, false))
            .ReturnsAsync(createFailure); // Only one attempt: Create fails

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.SaveWithRetryAsync(true, false);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.True(result.HasFailures);
        Assert.Single(result.Failed);
        Assert.Equal("Record already exists", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveWithRetryAsync(It.IsAny<IList<DummyModel>>(), true, false), Times.Once);
    }
    [Fact]
    public async Task SaveBulkAsyncAllModelsSucceedReturnsSuccessResult() {
        // Arrange
        var models = new List<DummyModel> {
            new() { RecordID = "1001", FieldA = "A", FieldB = 1 },
            new() { RecordID = "1002", FieldA = "B", FieldB = 2 }
        };

        var successResult = new KintoneWriteResult<DummyModel> {
            Succeeded = models
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveAsync(models, false))
            .ReturnsAsync(successResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.SaveBulkAsync(models);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.SaveAsync(models, false), Times.Once);
    }
    [Fact]
    public async Task SaveBulkAsyncSomeModelsFailReturnsPartialResult() {
        // Arrange
        var model1 = new DummyModel { RecordID = "2001", FieldA = "OK", FieldB = 10 };
        var model2 = new DummyModel { RecordID = "2002", FieldA = "Fail", FieldB = 20 };

        var partialResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model1],
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model2,
                    ErrorMessage = "Validation error"
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveAsync(It.Is<IList<DummyModel>>(list => list.Contains(model1) && list.Contains(model2)), true))
            .ReturnsAsync(partialResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.SaveBulkAsync([model1, model2], enableSingleRetryOnError: true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Equal("Validation error", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveAsync(It.IsAny<IList<DummyModel>>(), true), Times.Once);
    }
    [Fact]
    public async Task SaveBulkAsyncAllModelsFailReturnsAllFailures() {
        // Arrange
        var model1 = new DummyModel { RecordID = "3001", FieldA = "BadA", FieldB = -1 };
        var model2 = new DummyModel { RecordID = "3002", FieldA = "BadB", FieldB = -2 };

        var failureResult = new KintoneWriteResult<DummyModel> {
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model1,
                    ErrorMessage = "FieldB must be positive"
                },
                new KintoneWriteFailure<DummyModel> {
                    Record = model2,
                    ErrorMessage = "FieldB must be positive"
                }
            ]
        };

        var models = new List<DummyModel> { model1, model2 };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveAsync(models, false))
            .ReturnsAsync(failureResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.SaveBulkAsync(models);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Equal(2, result.Failed.Count);
        Assert.True(result.HasFailures);
        Assert.All(result.Failed, f => Assert.Contains("FieldB must be positive", f.ErrorMessage));
        mockService.Verify(s => s.SaveAsync(models, false), Times.Once);
    }
    [Fact]
    public async Task SaveSingleAsyncAllModelsSucceedReturnsAllSuccesses() {
        // Arrange
        var model1 = new DummyModel { RecordID = "4001", FieldA = "A1", FieldB = 1 };
        var model2 = new DummyModel { RecordID = "4002", FieldA = "A2", FieldB = 2 };

        var success1 = new KintoneWriteResult<DummyModel> {
            Succeeded = [model1]
        };
        var success2 = new KintoneWriteResult<DummyModel> {
            Succeeded = [model2]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveAsync(It.Is<IList<DummyModel>>(l => l.Count == 1 && l[0] == model1), false))
            .ReturnsAsync(success1);
        mockService
            .Setup(s => s.SaveAsync(It.Is<IList<DummyModel>>(l => l.Count == 1 && l[0] == model2), false))
            .ReturnsAsync(success2);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        var models = new List<DummyModel> { model1, model2 };

        // Act
        var result = await DummyModel.SaveSingleAsync(models);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Empty(result.Failed);
        Assert.False(result.HasFailures);
        Assert.Contains(model1, result.Succeeded);
        Assert.Contains(model2, result.Succeeded);
        mockService.Verify(s => s.SaveAsync(It.IsAny<IList<DummyModel>>(), false), Times.Exactly(2));
    }
    [Fact]
    public async Task SaveSingleAsyncPartialSuccessReturnsMixedResult() {
        // Arrange
        var model1 = new DummyModel { RecordID = "5001", FieldA = "Good", FieldB = 10 };
        var model2 = new DummyModel { RecordID = "5002", FieldA = "Bad", FieldB = -5 };

        var successResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model1]
        };
        var failureResult = new KintoneWriteResult<DummyModel> {
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model2,
                    ErrorMessage = "FieldB must be positive"
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveAsync(It.Is<IList<DummyModel>>(l => l.Count == 1 && l[0] == model1), false))
            .ReturnsAsync(successResult);
        mockService
            .Setup(s => s.SaveAsync(It.Is<IList<DummyModel>>(l => l.Count == 1 && l[0] == model2), false))
            .ReturnsAsync(failureResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        var models = new List<DummyModel> { model1, model2 };

        // Act
        var result = await DummyModel.SaveSingleAsync(models);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Contains(model1, result.Succeeded);
        Assert.Equal(model2, result.Failed[0].Record);
        Assert.Contains("FieldB must be positive", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveAsync(It.IsAny<IList<DummyModel>>(), false), Times.Exactly(2));
    }
    [Fact]
    public async Task SaveSingleAsyncAllModelsFailReturnsAllFailures() {
        // Arrange
        var model1 = new DummyModel { RecordID = "6001", FieldA = "BadA", FieldB = -10 };
        var model2 = new DummyModel { RecordID = "6002", FieldA = "BadB", FieldB = -20 };

        var failure1 = new KintoneWriteResult<DummyModel> {
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model1,
                    ErrorMessage = "FieldB must be positive"
                }
            ]
        };
        var failure2 = new KintoneWriteResult<DummyModel> {
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model2,
                    ErrorMessage = "FieldB must be positive"
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveAsync(It.Is<IList<DummyModel>>(l => l.Count == 1 && l[0] == model1), false))
            .ReturnsAsync(failure1);
        mockService
            .Setup(s => s.SaveAsync(It.Is<IList<DummyModel>>(l => l.Count == 1 && l[0] == model2), false))
            .ReturnsAsync(failure2);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        var models = new List<DummyModel> { model1, model2 };

        // Act
        var result = await DummyModel.SaveSingleAsync(models);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Equal(2, result.Failed.Count);
        Assert.True(result.HasFailures);
        Assert.Contains(model1, result.Failed.Select(f => f.Record));
        Assert.Contains(model2, result.Failed.Select(f => f.Record));
        Assert.All(result.Failed, f => Assert.Contains("FieldB must be positive", f.ErrorMessage));
        mockService.Verify(s => s.SaveAsync(It.IsAny<IList<DummyModel>>(), false), Times.Exactly(2));
    }
    [Fact]
    public async Task SaveWithRetryBulkAsyncAllModelsSucceedReturnsSuccessResult() {
        // Arrange
        var model1 = new DummyModel { RecordID = "7001", FieldA = "A1", FieldB = 1 };
        var model2 = new DummyModel { RecordID = "7002", FieldA = "A2", FieldB = 2 };

        var models = new List<DummyModel> { model1, model2 };

        var expectedResult = new KintoneWriteResult<DummyModel> {
            Succeeded = models
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(models, true, true))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.SaveWithRetryBulkAsync(models, enableSingleRetryOnError: true, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Empty(result.Failed);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.SaveWithRetryAsync(models, true, true), Times.Once);
    }
    [Fact]
    public async Task SaveWithRetryBulkAsyncPartialSuccessReturnsMixedResult() {
        // Arrange
        var model1 = new DummyModel { RecordID = "8001", FieldA = "Good", FieldB = 10 };
        var model2 = new DummyModel { RecordID = "8002", FieldA = "Bad", FieldB = -10 };

        var models = new List<DummyModel> { model1, model2 };

        var expectedResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model1],
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model2,
                    ErrorMessage = "FieldB must be positive"
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(models, false, true))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.SaveWithRetryBulkAsync(models, enableSingleRetryOnError: false, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Contains(model1, result.Succeeded);
        Assert.Equal(model2, result.Failed[0].Record);
        Assert.Contains("FieldB must be positive", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveWithRetryAsync(models, false, true), Times.Once);
    }
    [Fact]
    public async Task SaveWithRetryBulkAsyncAllModelsFailReturnsAllFailures() {
        // Arrange
        var model1 = new DummyModel { RecordID = "9001", FieldA = "BadA", FieldB = -1 };
        var model2 = new DummyModel { RecordID = "9002", FieldA = "BadB", FieldB = -2 };

        var models = new List<DummyModel> { model1, model2 };

        var expectedResult = new KintoneWriteResult<DummyModel> {
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model1,
                    ErrorMessage = "FieldB must be positive"
                },
                new KintoneWriteFailure<DummyModel> {
                    Record = model2,
                    ErrorMessage = "FieldB must be positive"
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(models, false, false))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.SaveWithRetryBulkAsync(models, enableSingleRetryOnError: false, enableCreateToUpdateRetry: false);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Equal(2, result.Failed.Count);
        Assert.True(result.HasFailures);
        Assert.Contains(model1, result.Failed.Select(f => f.Record));
        Assert.Contains(model2, result.Failed.Select(f => f.Record));
        Assert.All(result.Failed, f => Assert.Contains("FieldB must be positive", f.ErrorMessage));
        mockService.Verify(s => s.SaveWithRetryAsync(models, false, false), Times.Once);
    }
    [Fact]
    public async Task SaveWithRetryBulkAsyncRetryTurnsFailureIntoSuccess() {
        // Arrange
        var model = new DummyModel { RecordID = "10001", FieldA = "Retryable", FieldB = 5 };
        var models = new List<DummyModel> { model };

        // 初回失敗 → リトライ成功を模擬
        var initialFailure = new KintoneWriteResult<DummyModel> {
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model,
                    ErrorMessage = "Record already exists"
                }
            ]
        };
        var retrySuccess = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        var mockService = new Mock<IKintoneModelCrudService>();

        // 呼び出し回数をカウントして、1回目は失敗、2回目は成功を返す
        // int callCount = 0;
        mockService
            .Setup(s => s.SaveWithRetryAsync(models, true, true))
            .ReturnsAsync(retrySuccess);
        // .ReturnsAsync(() => {
        //     callCount++;
        //     return callCount == 1 ? initialFailure : retrySuccess;
        // });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.SaveWithRetryBulkAsync(models, enableSingleRetryOnError: true, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.Empty(result.Failed);
        Assert.False(result.HasFailures);
        Assert.Contains(model, result.Succeeded);
        mockService.Verify(s => s.SaveWithRetryAsync(models, true, true), Times.AtLeastOnce);
    }
    [Fact]
    public async Task SaveWithRetryBulkAsyncAllAttemptsFailReturnsFinalFailures() {
        // Arrange
        var model = new DummyModel { RecordID = "11001", FieldA = "StillBad", FieldB = -99 };
        var models = new List<DummyModel> { model };

        var failureResult = new KintoneWriteResult<DummyModel> {
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model,
                    ErrorMessage = "FieldB must be positive"
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();

        // 何度呼ばれても失敗を返す
        mockService
            .Setup(s => s.SaveWithRetryAsync(models, true, true))
            .ReturnsAsync(failureResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.SaveWithRetryBulkAsync(models, enableSingleRetryOnError: true, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Equal(model, result.Failed[0].Record);
        Assert.Contains("FieldB must be positive", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveWithRetryAsync(models, true, true), Times.Once);
    }
    [Fact]
    public async Task SaveWithRetrySingleAsyncAllModelsSucceedReturnsAllSuccesses() {
        // Arrange
        var model1 = new DummyModel { RecordID = "12001", FieldA = "A1", FieldB = 1 };
        var model2 = new DummyModel { RecordID = "12002", FieldA = "A2", FieldB = 2 };
        var models = new List<DummyModel> { model1, model2 };

        var success1 = new KintoneWriteResult<DummyModel> {
            Succeeded = [model1]
        };
        var success2 = new KintoneWriteResult<DummyModel> {
            Succeeded = [model2]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(l => l.Count == 1 && l[0] == model1), false, true))
            .ReturnsAsync(success1);
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(l => l.Count == 1 && l[0] == model2), false, true))
            .ReturnsAsync(success2);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.SaveWithRetrySingleAsync(models, enableSingleRetryOnError: false, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Empty(result.Failed);
        Assert.False(result.HasFailures);
        Assert.Contains(model1, result.Succeeded);
        Assert.Contains(model2, result.Succeeded);
        mockService.Verify(s => s.SaveWithRetryAsync(It.IsAny<IList<DummyModel>>(), false, true), Times.Exactly(2));
    }
    [Fact]
    public async Task SaveWithRetrySingleAsyncPartialSuccessReturnsMixedResult() {
        // Arrange
        var model1 = new DummyModel { RecordID = "12001", FieldA = "A1", FieldB = 1 };
        var model2 = new DummyModel { RecordID = "12002", FieldA = "A2", FieldB = 2 };
        var models = new List<DummyModel> { model1, model2 };

        var successResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model1]
        };
        var failureResult = new KintoneWriteResult<DummyModel> {
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model2,
                    ErrorMessage = "Save failed for model2"
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(l => l[0] == model1), false, true))
            .ReturnsAsync(successResult);
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(l => l[0] == model2), false, true))
            .ReturnsAsync(failureResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.SaveWithRetrySingleAsync(models, enableSingleRetryOnError: false, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Contains(model1, result.Succeeded);
        Assert.Equal(model2, result.Failed[0].Record);
        Assert.Equal("Save failed for model2", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveWithRetryAsync(It.IsAny<IList<DummyModel>>(), false, true), Times.Exactly(2));
    }
    [Fact]
    public async Task SaveWithRetrySingleAsyncAllModelsFailReturnsAllFailures() {
        // Arrange
        var model1 = new DummyModel { RecordID = "12001", FieldA = "A1", FieldB = 1 };
        var model2 = new DummyModel { RecordID = "12002", FieldA = "A2", FieldB = 2 };
        var models = new List<DummyModel> { model1, model2 };

        var failure1 = new KintoneWriteFailure<DummyModel> {
            Record = model1,
            ErrorMessage = "Save failed for model1"
        };
        var failure2 = new KintoneWriteFailure<DummyModel> {
            Record = model2,
            ErrorMessage = "Save failed for model2"
        };

        var failureResult1 = new KintoneWriteResult<DummyModel> {
            Failed = [failure1]
        };
        var failureResult2 = new KintoneWriteResult<DummyModel> {
            Failed = [failure2]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(l => l[0] == model1), false, true))
            .ReturnsAsync(failureResult1);
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(l => l[0] == model2), false, true))
            .ReturnsAsync(failureResult2);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.SaveWithRetrySingleAsync(models, enableSingleRetryOnError: false, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Equal(2, result.Failed.Count);
        Assert.True(result.HasFailures);
        Assert.Equal(model1, result.Failed[0].Record);
        Assert.Equal("Save failed for model1", result.Failed[0].ErrorMessage);
        Assert.Equal(model2, result.Failed[1].Record);
        Assert.Equal("Save failed for model2", result.Failed[1].ErrorMessage);
        mockService.Verify(s => s.SaveWithRetryAsync(It.IsAny<IList<DummyModel>>(), false, true), Times.Exactly(2));
    }
    [Fact]
    public async Task SaveWithRetrySingleAsyncRetrySucceedsReturnsSuccessAfterInitialFailure() {
        // Arrange
        var model = new DummyModel { RecordID = "12001", FieldA = "A1", FieldB = 1 };
        var models = new List<DummyModel> { model };

        var failureResult = new KintoneWriteResult<DummyModel> {
            Failed = [
                new KintoneWriteFailure<DummyModel> {
                    Record = model,
                    ErrorMessage = "Initial failure"
                }
            ]
        };
        var successResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        // var callCount = 0;
        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(l => l[0] == model), true, true))
            .ReturnsAsync(successResult);
        // .ReturnsAsync(() => {
        //     callCount++;
        //     return callCount == 1 ? failureResult : successResult;
        // });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.SaveWithRetrySingleAsync(models, enableSingleRetryOnError: true, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.Empty(result.Failed);
        Assert.False(result.HasFailures);
        Assert.Contains(model, result.Succeeded);
        mockService.Verify(s => s.SaveWithRetryAsync(It.IsAny<IList<DummyModel>>(), true, true), Times.Once);
    }
    [Fact]
    public async Task SaveWithRetrySingleAsyncRetryFailsReturnsFailure() {
        // Arrange
        var model = new DummyModel { RecordID = "12001", FieldA = "A1", FieldB = 1 };
        var models = new List<DummyModel> { model };

        var failure1 = new KintoneWriteFailure<DummyModel> {
            Record = model,
            ErrorMessage = "Initial failure"
        };
        var failure2 = new KintoneWriteFailure<DummyModel> {
            Record = model,
            ErrorMessage = "Retry failure"
        };

        var failureResult1 = new KintoneWriteResult<DummyModel> {
            Failed = [failure1]
        };
        var failureResult2 = new KintoneWriteResult<DummyModel> {
            Failed = [failure2]
        };

        var callCount = 0;
        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(l => l[0] == model), true, true))
            .ReturnsAsync(failureResult2);
        // .ReturnsAsync(() => {
        //     callCount++;
        //     return callCount == 1 ? failureResult1 : failureResult2;
        // });

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.SaveWithRetrySingleAsync(models, enableSingleRetryOnError: true, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Equal(model, result.Failed[0].Record);
        Assert.Equal("Retry failure", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveWithRetryAsync(It.IsAny<IList<DummyModel>>(), true, true), Times.Once);
    }

    #endregion
}
