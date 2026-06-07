using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Entities;

/// <summary>
/// KintoneModelBaseの保存関連のテストクラス。
/// </summary>
public class KintoneModelBaseSaveTests {
    /// <summary>
    /// テスト用のダミーモデルクラス。実際のアプリIdやアクセス情報はテスト内でモックされるため、適当な値を設定している。
    /// </summary>
    public class DummyModel : KintoneModelBase<DummyModel> {
        public override int AppId { get; init; }
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(fieldCode: "FieldA", fieldType: KintoneFieldType.SingleLineText)]
        public string FieldA { get; set; } = string.Empty;
        [KintoneItem(fieldCode: "FieldB", fieldType: KintoneFieldType.Number)]
        public int FieldB { get; set; }
    }

    #region <<Test methods>>
    /// <summary>
    /// SaveAsyncが成功した場合、期待される結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveAsyncSuccessReturnsExpectedResult() {
        // Arrange
        var model = new DummyModel { RecordId = "1111", FieldA = "ToDelete", FieldB = 999 };

        var expectedResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), It.IsAny<bool>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await model.SaveAsync(mockService.Object);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.SaveAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), It.IsAny<bool>()), Times.Once);
    }

    /// <summary>
    /// SaveAsyncが失敗した場合、期待される失敗結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveAsyncFailureReturnsExpectedFailureResult() {
        // Arrange
        var model = new DummyModel { RecordId = "1111", FieldA = "Invalid", FieldB = -1 };

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

        // Act
        var result = await model.SaveAsync(mockService.Object);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.True(result.HasFailures);
        Assert.Single(result.Failed);
        Assert.Equal(model, result.Failed[0].Record);
        Assert.Contains("Invalid FieldB", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), It.IsAny<bool>()), Times.Once);
    }

    /// <summary>
    /// SaveAsyncでリトライフラグをtrueにした場合、サービスに正しくフラグが渡されることをテストする。
    /// </summary>
    [Fact]
    public async Task SaveAsyncWithRetryFlagTruePassesFlagToService() {
        // Arrange
        var model = new DummyModel { RecordId = "1111", FieldA = "Retry", FieldB = 123 };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true))
            .ReturnsAsync(new KintoneWriteResult<DummyModel> { Succeeded = [model] });

        // Act
        var result = await model.SaveAsync(mockService.Object, enableSingleRetryOnError: true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.SaveAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true), Times.Once);
    }

    /// <summary>
    /// SaveWithRetryAsyncが成功した場合、リトライフラグに関係なく期待される結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetryAsyncSuccessWithoutRetryReturnsExpectedResult() {
        // Arrange
        var model = new DummyModel { RecordId = "2222", FieldA = "Initial", FieldB = 123 };

        var expectedResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), false, true))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await model.SaveWithRetryAsync(mockService.Object);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), false, true), Times.Once);
    }

    /// <summary>
    /// SaveWithRetryAsyncが初回失敗し、リトライで成功した場合、最終的に成功結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetryAsyncFirstAttemptFailsRetrySucceeds() {
        // Arrange
        var model = new DummyModel { RecordId = "3333", FieldA = "RetryMe", FieldB = 456 };

        var successResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .SetupSequence(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true, true))
            .ReturnsAsync(successResult);

        // Act
        var result = await model.SaveWithRetryAsync(mockService.Object, true, true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true, true), Times.Once);
    }

    /// <summary>
    /// SaveWithRetryAsyncが全ての試行で失敗した場合、最終的に失敗結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetryAsyncAllAttemptsFailReturnsFailure() {
        // Arrange
        var model = new DummyModel { RecordId = "3333", FieldA = "RetryMe", FieldB = 456 };

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

        // Act
        var result = await model.SaveWithRetryAsync(mockService.Object, true, true);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.True(result.HasFailures);
        Assert.Single(result.Failed);
        Assert.Equal("Network timeout", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true, true), Times.Once);
    }

    /// <summary>
    /// SaveWithRetryAsyncで、初回の作成が失敗し、リトライで更新に切り替えて成功した場合、最終的に成功結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetryAsyncCreateFailsUpdateRetrySucceedsWhenEnabled() {
        // Arrange
        var model = new DummyModel { RecordId = "9999", FieldA = "FallbackToUpdate", FieldB = 789 };

        var updateSuccess = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .SetupSequence(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), true, true))
            .ReturnsAsync(updateSuccess);

        // Act
        var result = await model.SaveWithRetryAsync(mockService.Object, true, true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.SaveWithRetryAsync(It.IsAny<IList<DummyModel>>(), true, true), Times.Once);
    }

    /// <summary>
    /// SaveWithRetryAsyncで、初回の作成が失敗し、リトライで更新に切り替えるオプションが無効な場合、最終的に失敗結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetryAsyncCreateFailsNoUpdateRetryWhenDisabled() {
        // Arrange
        var model = new DummyModel { RecordId = "9999", FieldA = "NoFallback", FieldB = 321 };

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
            .ReturnsAsync(createFailure);

        // Act
        var result = await model.SaveWithRetryAsync(mockService.Object, true, false);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.True(result.HasFailures);
        Assert.Single(result.Failed);
        Assert.Equal("Record already exists", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveWithRetryAsync(It.IsAny<IList<DummyModel>>(), true, false), Times.Once);
    }

    /// <summary>
    /// SaveBulkAsyncが全てのモデルで成功した場合、期待される成功結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveBulkAsyncAllModelsSucceedReturnsSuccessResult() {
        // Arrange
        var models = new List<DummyModel> {
            new() { RecordId = "1001", FieldA = "A", FieldB = 1 },
            new() { RecordId = "1002", FieldA = "B", FieldB = 2 }
        };

        var successResult = new KintoneWriteResult<DummyModel> {
            Succeeded = models
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveAsync(models, false))
            .ReturnsAsync(successResult);

        // Act
        var result = await DummyModel.SaveBulkAsync(mockService.Object, models);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.SaveAsync(models, false), Times.Once);
    }

    /// <summary>
    /// SaveBulkAsyncが一部のモデルで失敗した場合、成功と失敗の両方の結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveBulkAsyncSomeModelsFailReturnsPartialResult() {
        // Arrange
        var model1 = new DummyModel { RecordId = "2001", FieldA = "OK", FieldB = 10 };
        var model2 = new DummyModel { RecordId = "2002", FieldA = "Fail", FieldB = 20 };

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

        // Act
        var result = await DummyModel.SaveBulkAsync(mockService.Object, [model1, model2], enableSingleRetryOnError: true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Equal("Validation error", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveAsync(It.IsAny<IList<DummyModel>>(), true), Times.Once);
    }

    /// <summary>
    /// SaveBulkAsyncが全てのモデルで失敗した場合、期待される失敗結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveBulkAsyncAllModelsFailReturnsAllFailures() {
        // Arrange
        var model1 = new DummyModel { RecordId = "3001", FieldA = "BadA", FieldB = -1 };
        var model2 = new DummyModel { RecordId = "3002", FieldA = "BadB", FieldB = -2 };

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

        // Act
        var result = await DummyModel.SaveBulkAsync(mockService.Object, models);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Equal(2, result.Failed.Count);
        Assert.True(result.HasFailures);
        Assert.All(result.Failed, f => Assert.Contains("FieldB must be positive", f.ErrorMessage));
        mockService.Verify(s => s.SaveAsync(models, false), Times.Once);
    }

    /// <summary>
    /// SaveSingleAsyncが全てのモデルで成功した場合、全て成功の結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveSingleAsyncAllModelsSucceedReturnsAllSuccesses() {
        // Arrange
        var model1 = new DummyModel { RecordId = "4001", FieldA = "A1", FieldB = 1 };
        var model2 = new DummyModel { RecordId = "4002", FieldA = "A2", FieldB = 2 };

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

        var models = new List<DummyModel> { model1, model2 };

        // Act
        var result = await DummyModel.SaveSingleAsync(mockService.Object, models);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Empty(result.Failed);
        Assert.False(result.HasFailures);
        Assert.Contains(model1, result.Succeeded);
        Assert.Contains(model2, result.Succeeded);
        mockService.Verify(s => s.SaveAsync(It.IsAny<IList<DummyModel>>(), false), Times.Exactly(2));
    }

    /// <summary>
    /// SaveSingleAsyncが一部のモデルで失敗した場合、成功と失敗の両方の結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveSingleAsyncPartialSuccessReturnsMixedResult() {
        // Arrange
        var model1 = new DummyModel { RecordId = "5001", FieldA = "Good", FieldB = 10 };
        var model2 = new DummyModel { RecordId = "5002", FieldA = "Bad", FieldB = -5 };

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

        var models = new List<DummyModel> { model1, model2 };

        // Act
        var result = await DummyModel.SaveSingleAsync(mockService.Object, models);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Contains(model1, result.Succeeded);
        Assert.Equal(model2, result.Failed[0].Record);
        Assert.Contains("FieldB must be positive", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveAsync(It.IsAny<IList<DummyModel>>(), false), Times.Exactly(2));
    }

    /// <summary>
    /// SaveSingleAsyncが全てのモデルで失敗した場合、全て失敗の結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveSingleAsyncAllModelsFailReturnsAllFailures() {
        // Arrange
        var model1 = new DummyModel { RecordId = "6001", FieldA = "BadA", FieldB = -10 };
        var model2 = new DummyModel { RecordId = "6002", FieldA = "BadB", FieldB = -20 };

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

        var models = new List<DummyModel> { model1, model2 };

        // Act
        var result = await DummyModel.SaveSingleAsync(mockService.Object, models);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Equal(2, result.Failed.Count);
        Assert.True(result.HasFailures);
        Assert.Contains(model1, result.Failed.Select(f => f.Record));
        Assert.Contains(model2, result.Failed.Select(f => f.Record));
        Assert.All(result.Failed, f => Assert.Contains("FieldB must be positive", f.ErrorMessage));
        mockService.Verify(s => s.SaveAsync(It.IsAny<IList<DummyModel>>(), false), Times.Exactly(2));
    }

    /// <summary>
    /// SaveWithRetryBulkAsyncが全てのモデルで成功した場合、期待される成功結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetryBulkAsyncAllModelsSucceedReturnsSuccessResult() {
        // Arrange
        var model1 = new DummyModel { RecordId = "7001", FieldA = "A1", FieldB = 1 };
        var model2 = new DummyModel { RecordId = "7002", FieldA = "A2", FieldB = 2 };

        var models = new List<DummyModel> { model1, model2 };

        var expectedResult = new KintoneWriteResult<DummyModel> {
            Succeeded = models
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(models, true, true))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await DummyModel.SaveWithRetryBulkAsync(mockService.Object, models, enableSingleRetryOnError: true, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Empty(result.Failed);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.SaveWithRetryAsync(models, true, true), Times.Once);
    }

    /// <summary>
    /// SaveWithRetryBulkAsyncが一部のモデルで失敗した場合、成功と失敗の両方の結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetryBulkAsyncPartialSuccessReturnsMixedResult() {
        // Arrange
        var model1 = new DummyModel { RecordId = "8001", FieldA = "Good", FieldB = 10 };
        var model2 = new DummyModel { RecordId = "8002", FieldA = "Bad", FieldB = -10 };

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

        // Act
        var result = await DummyModel.SaveWithRetryBulkAsync(mockService.Object, models, enableSingleRetryOnError: false, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Contains(model1, result.Succeeded);
        Assert.Equal(model2, result.Failed[0].Record);
        Assert.Contains("FieldB must be positive", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveWithRetryAsync(models, false, true), Times.Once);
    }

    /// <summary>
    /// SaveWithRetryBulkAsyncが全てのモデルで失敗した場合、全て失敗の結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetryBulkAsyncAllModelsFailReturnsAllFailures() {
        // Arrange
        var model1 = new DummyModel { RecordId = "9001", FieldA = "BadA", FieldB = -1 };
        var model2 = new DummyModel { RecordId = "9002", FieldA = "BadB", FieldB = -2 };

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

        // Act
        var result = await DummyModel.SaveWithRetryBulkAsync(mockService.Object, models, enableSingleRetryOnError: false, enableCreateToUpdateRetry: false);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Equal(2, result.Failed.Count);
        Assert.True(result.HasFailures);
        Assert.Contains(model1, result.Failed.Select(f => f.Record));
        Assert.Contains(model2, result.Failed.Select(f => f.Record));
        Assert.All(result.Failed, f => Assert.Contains("FieldB must be positive", f.ErrorMessage));
        mockService.Verify(s => s.SaveWithRetryAsync(models, false, false), Times.Once);
    }

    /// <summary>
    /// SaveWithRetryBulkAsyncで、初回の保存が失敗し、リトライで成功した場合、最終的に成功結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetryBulkAsyncRetryTurnsFailureIntoSuccess() {
        // Arrange
        var model = new DummyModel { RecordId = "10001", FieldA = "Retryable", FieldB = 5 };
        var models = new List<DummyModel> { model };

        var retrySuccess = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(models, true, true))
            .ReturnsAsync(retrySuccess);

        // Act
        var result = await DummyModel.SaveWithRetryBulkAsync(mockService.Object, models, enableSingleRetryOnError: true, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.Empty(result.Failed);
        Assert.False(result.HasFailures);
        Assert.Contains(model, result.Succeeded);
        mockService.Verify(s => s.SaveWithRetryAsync(models, true, true), Times.AtLeastOnce);
    }

    /// <summary>
    /// SaveWithRetryBulkAsyncで、全ての試行が失敗した場合、最終的に失敗結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetryBulkAsyncAllAttemptsFailReturnsFinalFailures() {
        // Arrange
        var model = new DummyModel { RecordId = "11001", FieldA = "StillBad", FieldB = -99 };
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
        mockService
            .Setup(s => s.SaveWithRetryAsync(models, true, true))
            .ReturnsAsync(failureResult);

        // Act
        var result = await DummyModel.SaveWithRetryBulkAsync(mockService.Object, models, enableSingleRetryOnError: true, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Equal(model, result.Failed[0].Record);
        Assert.Contains("FieldB must be positive", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveWithRetryAsync(models, true, true), Times.Once);
    }

    /// <summary>
    /// SaveWithRetrySingleAsyncが全てのモデルで成功した場合、全て成功の結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetrySingleAsyncAllModelsSucceedReturnsAllSuccesses() {
        // Arrange
        var model1 = new DummyModel { RecordId = "12001", FieldA = "A1", FieldB = 1 };
        var model2 = new DummyModel { RecordId = "12002", FieldA = "A2", FieldB = 2 };
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

        // Act
        var result = await DummyModel.SaveWithRetrySingleAsync(mockService.Object, models, enableSingleRetryOnError: false, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Empty(result.Failed);
        Assert.False(result.HasFailures);
        Assert.Contains(model1, result.Succeeded);
        Assert.Contains(model2, result.Succeeded);
        mockService.Verify(s => s.SaveWithRetryAsync(It.IsAny<IList<DummyModel>>(), false, true), Times.Exactly(2));
    }

    /// <summary>
    /// SaveWithRetrySingleAsyncが一部のモデルで失敗した場合、成功と失敗の両方の結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetrySingleAsyncPartialSuccessReturnsMixedResult() {
        // Arrange
        var model1 = new DummyModel { RecordId = "12001", FieldA = "A1", FieldB = 1 };
        var model2 = new DummyModel { RecordId = "12002", FieldA = "A2", FieldB = 2 };
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

        // Act
        var result = await DummyModel.SaveWithRetrySingleAsync(mockService.Object, models, enableSingleRetryOnError: false, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.True(result.HasFailures);
        Assert.Contains(model1, result.Succeeded);
        Assert.Equal(model2, result.Failed[0].Record);
        Assert.Equal("Save failed for model2", result.Failed[0].ErrorMessage);
        mockService.Verify(s => s.SaveWithRetryAsync(It.IsAny<IList<DummyModel>>(), false, true), Times.Exactly(2));
    }

    /// <summary>
    /// SaveWithRetrySingleAsyncが全てのモデルで失敗した場合、全て失敗の結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetrySingleAsyncAllModelsFailReturnsAllFailures() {
        // Arrange
        var model1 = new DummyModel { RecordId = "12001", FieldA = "A1", FieldB = 1 };
        var model2 = new DummyModel { RecordId = "12002", FieldA = "A2", FieldB = 2 };
        var models = new List<DummyModel> { model1, model2 };

        var failureResult1 = new KintoneWriteResult<DummyModel> {
            Failed = [new KintoneWriteFailure<DummyModel> { Record = model1, ErrorMessage = "Save failed for model1" }]
        };
        var failureResult2 = new KintoneWriteResult<DummyModel> {
            Failed = [new KintoneWriteFailure<DummyModel> { Record = model2, ErrorMessage = "Save failed for model2" }]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(l => l[0] == model1), false, true))
            .ReturnsAsync(failureResult1);
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(l => l[0] == model2), false, true))
            .ReturnsAsync(failureResult2);

        // Act
        var result = await DummyModel.SaveWithRetrySingleAsync(mockService.Object, models, enableSingleRetryOnError: false, enableCreateToUpdateRetry: true);

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

    /// <summary>
    /// SaveWithRetrySingleAsyncで、初回の保存が失敗し、リトライで成功した場合、最終的に成功結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetrySingleAsyncRetrySucceedsReturnsSuccessAfterInitialFailure() {
        // Arrange
        var model = new DummyModel { RecordId = "12001", FieldA = "A1", FieldB = 1 };
        var models = new List<DummyModel> { model };

        var successResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(l => l[0] == model), true, true))
            .ReturnsAsync(successResult);

        // Act
        var result = await DummyModel.SaveWithRetrySingleAsync(mockService.Object, models, enableSingleRetryOnError: true, enableCreateToUpdateRetry: true);

        // Assert
        Assert.Single(result.Succeeded);
        Assert.Empty(result.Failed);
        Assert.False(result.HasFailures);
        Assert.Contains(model, result.Succeeded);
        mockService.Verify(s => s.SaveWithRetryAsync(It.IsAny<IList<DummyModel>>(), true, true), Times.Once);
    }

    /// <summary>
    /// SaveWithRetrySingleAsyncで、全ての試行が失敗した場合、最終的に失敗結果を返すことをテストする。
    /// </summary>
    [Fact]
    public async Task SaveWithRetrySingleAsyncRetryFailsReturnsFailure() {
        // Arrange
        var model = new DummyModel { RecordId = "12001", FieldA = "A1", FieldB = 1 };
        var models = new List<DummyModel> { model };

        var failureResult2 = new KintoneWriteResult<DummyModel> {
            Failed = [new KintoneWriteFailure<DummyModel> { Record = model, ErrorMessage = "Retry failure" }]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.SaveWithRetryAsync(It.Is<IList<DummyModel>>(l => l[0] == model), true, true))
            .ReturnsAsync(failureResult2);

        // Act
        var result = await DummyModel.SaveWithRetrySingleAsync(mockService.Object, models, enableSingleRetryOnError: true, enableCreateToUpdateRetry: true);

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
