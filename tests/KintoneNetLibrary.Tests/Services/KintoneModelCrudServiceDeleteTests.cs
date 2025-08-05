using System.Text.Json;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Converters;
using KintoneNetLibrary.Infrastructure.Helpers;
using KintoneNetLibrary.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Services;

public class KintoneModelCrudServiceDeleteTests {
    #region <<Test methods>>
    [Fact]
    public async Task DeleteAsync_ShouldReturnEmptyResult_WhenModelsIsEmpty() {
        // Arrange
        var mockRepo = new Mock<IKintoneRepository>();
        var loggerMock = new Mock<ILogger<KintoneModelCrudService>>();

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            loggerMock.Object
        );

        var model = new SampleModel { RecordID = "123" };
        // Act
        var result = await service.DeleteAsync<SampleModel>([]);

        // Assert
        Assert.Empty(result.DeletedIDs);
        Assert.Empty(result.FailedIDs);

        // Hookや削除処理は呼ばれない
        mockRepo.Verify(x => x.DeleteRecordsAsync(It.IsAny<IList<KintoneModelBase>>()), Times.Never);
        loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o != null && o.ToString().Contains("Start")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o != null && o.ToString().Contains("Finish")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        TestLogHelper.VerifyLog(loggerMock, LogLevel.Information, "DeleteAsync() - Start", Times.Once());
        TestLogHelper.VerifyLog(loggerMock, LogLevel.Information, "DeleteAsync() - Finish", Times.Once());
    }
    [Fact]
    public async Task DeleteAsync_ShouldDeleteOneModel_WhenOneModelProvided() {
        // Arrange
        var mockRepo = new Mock<IKintoneRepository>();
        var loggerMock = new Mock<ILogger<KintoneModelCrudService>>();

        var model = new SampleModel { RecordID = "123" };
        var models = new List<SampleModel> { model };

        mockRepo
            .Setup(x => x.DeleteRecordsAsync(It.IsAny<IList<KintoneModelBase>>()))
            .ReturnsAsync("");

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            loggerMock.Object
        );

        // Act
        var result = await service.DeleteAsync(models, false);

        // Assert
        Assert.Single(result.DeletedIDs);
        Assert.Contains("123", result.DeletedIDs);
        Assert.Empty(result.FailedIDs);

        mockRepo.Verify(x => x.DeleteRecordsAsync<SampleModel>(
            It.Is<IList<SampleModel>>(list => list.Count == 1 && list[0].RecordID == "123")
        ), Times.Once);

        TestLogHelper.VerifyLog(loggerMock, LogLevel.Information, "DeleteAsync() - Start", Times.Once());
        TestLogHelper.VerifyLog(loggerMock, LogLevel.Information, "DeleteAsync() - Finish", Times.Once());
    }
    [Fact]
    public async Task DeleteAsync_ShouldDeleteMultipleModels_WhenMultipleModelsProvided() {
        // Arrange
        var models = new List<SampleModel> {
            new() { RecordID = "101" },
            new() { RecordID = "102" },
            new() { RecordID = "103" }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        var loggerMock = new Mock<ILogger<KintoneModelCrudService>>();
        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            loggerMock.Object
        );

        // Act
        await service.DeleteAsync(models, false);

        // Assert
        mockRepo.Verify(x => x.DeleteRecordsAsync<SampleModel>(
            It.Is<IList<SampleModel>>(list =>
                list.Count == 3 &&
                list.Any(m => m.RecordID == "101") &&
                list.Any(m => m.RecordID == "102") &&
                list.Any(m => m.RecordID == "103")
            )
        ), Times.Once);
    }
    [Fact]
    public async Task DeleteAsync_ShouldDeleteOnlyValidModels_WhenSomeModelsAreInvalid() {
        // Arrange
        var models = new List<SampleModel>
        {
        new() { RecordID = "201" },
        new() { RecordID = null },  // 無効モデル
        new() { RecordID = "202" }
    };

        var mockRepo = new Mock<IKintoneRepository>();
        var loggerMock = new Mock<ILogger<KintoneModelCrudService>>();
        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            loggerMock.Object
        );

        // Act
        await service.DeleteAsync(models, false);

        // Assert
        mockRepo.Verify(x => x.DeleteRecordsAsync<SampleModel>(
            It.Is<IList<SampleModel>>(list =>
                list.Count == 2 &&
                list.All(m => m.RecordID != null) &&
                list.Any(m => m.RecordID == "201") &&
                list.Any(m => m.RecordID == "202")
            )
        ), Times.Once);
    }
    [Fact]
    public async Task DeleteAsync_ShouldHandleNotFoundRecord_WhenRecordIDIsInvalid() {
        // Arrange
        var models = new List<SampleModel> {
            new() { RecordID = "9999" } // ← 存在しないIDと仮定
        };

        var mockRepo = new Mock<IKintoneRepository>();

        // モックで NotFound を模擬
        mockRepo
            .Setup(x => x.DeleteRecordsAsync<SampleModel>(
                It.Is<IList<SampleModel>>(list =>
                    list.Count == 1 &&
                    list[0].RecordID == "9999")
            ))
            .ThrowsAsync(new KintoneException("Record not found"));

        // var service = new KintoneModelCrudService<SampleModel>(mockRepo.Object);
        var loggerMock = new Mock<ILogger<KintoneModelCrudService>>();
        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            loggerMock.Object
        );

        // Act & Assert
        var result = await service.DeleteAsync(models);
        Assert.Empty(result.DeletedIDs);
        Assert.Single(result.FailedIDs);
        Assert.True(result.HasFailures);
    }
    [Fact]
    public async Task DeleteAsync_ShouldSeparateDeletedAndFailedRecords_WhenPartiallyFound() {
        // Arrange
        var validId1 = "1001";
        var validId2 = "1002";
        var invalidId = "9999"; // ← 存在しないIDと仮定

        var models = new List<SampleModel> {
            new() { RecordID = validId1 },
            new() { RecordID = validId2 },
            new() { RecordID = invalidId }
        };

        var foundModels = new List<SampleModel> {
            new() { RecordID = validId1 },
            new() { RecordID = validId2 }
        };

        var foundJson = JsonSerializer.Serialize(new {
            records = foundModels.Select(m =>
                new Dictionary<string, object> {
                    ["$id"] = new { type = "__ID__", value = m.RecordID },
                }).ToList()
        });

        var mockRepo = new Mock<IKintoneRepository>();

        // FindAsync() で存在する2件だけを返すように設定
        mockRepo.Setup(x => x.FindByIDsAsync<SampleModel>(
            It.IsAny<SampleModel>(),
            It.Is<IList<string>>(ids => ids.Count == 3),
            It.IsAny<IList<string>>()
        )).ReturnsAsync(foundJson);

        var expectedIds = foundModels.Select(f => f.RecordID).ToHashSet();
        mockRepo.Setup(x => x.DeleteRecordsAsync<SampleModel>(
            It.Is<IList<SampleModel>>(list =>
                list.All(m => expectedIds.Contains(m.RecordID))
            )
        )).ReturnsAsync(KintoneRequestBuilder.BuildDeleteJson(new List<SampleModel> { new() { RecordID = invalidId } }));


        var loggerMock = new Mock<ILogger<KintoneModelCrudService>>();
        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            loggerMock.Object
        );

        // Act
        var result = await service.DeleteAsync(models);

        // Assert
        Assert.Equal(2, result.DeletedIDs.Count);
        Assert.Contains(validId1, result.DeletedIDs);
        Assert.Contains(validId2, result.DeletedIDs);

        Assert.Single(result.FailedIDs);
        Assert.Equal(invalidId, result.FailedIDs[0].ID);
        Assert.Equal("Record is not found.", result.FailedIDs[0].ErrorMessage);

        Assert.True(result.HasFailures);
    }

    #endregion
}