using System.Text.Json;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Services;

public class KintoneModelCrudServiceUpdateTests {
    #region <<Test methods>>
    [Fact]
    public async Task UpdateAsync_WithSingleRecord_ReturnsSucceededResult() {
        var testRecord = new SampleModel { FieldA = "Update1", RecordID = "R9999", Revision = 1 };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync(JsonSerializer.Serialize(new {
                ids = new[] { "R9999" },
                revisions = new[] { "2" }
            }));

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 1 }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        var result = await service.UpdateAsync([testRecord]);

        Assert.Single(result.Succeeded);
        var updated = result.Succeeded[0];
        Assert.Equal("R9999", updated.RecordID);
        Assert.Equal(2, updated.Revision);
        Assert.Empty(result.Failed);
    }
    [Fact]
    public async Task UpdateAsync_WithMultipleRecords_ReturnsAllSucceeded() {
        var records = new List<SampleModel> {
            new() { FieldA = "Update1", RecordID = "R1001", Revision = 1 },
            new() { FieldA = "Update2", RecordID = "R1002", Revision = 2 }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync(JsonSerializer.Serialize(new {
                ids = new[] { "R1001", "R1002" },
                revisions = new[] { "2", "3" }
            }));

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        var result = await service.UpdateAsync(records);

        Assert.Equal(2, result.Succeeded.Count);
        Assert.Contains(result.Succeeded, r => r.RecordID == "R1001" && r.Revision == 2);
        Assert.Contains(result.Succeeded, r => r.RecordID == "R1002" && r.Revision == 3);
        Assert.Empty(result.Failed);
    }
    [Fact]
    public async Task UpdateAsync_WhenBulkFailsAndSingleRetrySucceeds_RecordsAddedToSucceeded() {
        // Arrange
        var records = new List<SampleModel> {
            new() { FieldA = "R1", RecordID = "RID001", Revision = 1 },
            new() { FieldA = "R2", RecordID = "RID002", Revision = 1 }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        var callLog = new List<string>();

        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync((IList<SampleModel> input) => {
                if (input.Count == 2) {
                    callLog.Add("bulk");
                    throw new KintoneException("Bulk update failed") {
                        Error = new KintoneError { Code = "BULK_FAIL", Message = "Invalid chunk" }
                    };
                }

                callLog.Add($"single:{input[0].RecordID}");
                var json = JsonSerializer.Serialize(new {
                    ids = new[] { input[0].RecordID },
                    revisions = new[] { "2" }
                });
                return json;
            });

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 1 }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        // Act
        var result = await service.UpdateAsync(records, enableSingleRetryOnError: true);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Empty(result.Failed);

        Assert.Equal("RID001", result.Succeeded[0].RecordID);
        Assert.Equal("RID002", result.Succeeded[1].RecordID);
        Assert.All(result.Succeeded, r => Assert.Equal(2, r.Revision));

        Assert.Equal(3, callLog.Count); // 1回bulk + 2回single
        Assert.Equal("bulk", callLog[0]);
    }
    [Fact]
    public async Task UpdateAsync_WhenBulkAndRetryBothFail_AddsAllRecordsToFailed() {
        // Arrange
        var records = new List<SampleModel> {
        new() { FieldA = "R1", RecordID = "RID001", Revision = 1 },
        new() { FieldA = "R2", RecordID = "RID002", Revision = 1 }
    };

        var mockRepo = new Mock<IKintoneRepository>();

        // バルク更新（チャンク）を失敗させる
        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.Is<IList<SampleModel>>(chunk => chunk.Count == 2)))
            .ThrowsAsync(new KintoneException("Bulk failure") {
                Error = new KintoneError { Code = "BULK_ERR", Message = "Invalid bulk payload" }
            });

        // 単件リトライもすべて失敗
        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.Is<IList<SampleModel>>(chunk => chunk.Count == 1)))
            .ThrowsAsync(new KintoneException("Retry failure") {
                Error = new KintoneError { Code = "SINGLE_ERR", Message = "Invalid single payload" }
            });

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 1 }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        // Act
        var result = await service.UpdateAsync(records, enableSingleRetryOnError: true);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Equal(2, result.Failed.Count);

        Assert.All(result.Failed, failure => {
            Assert.Contains("R", failure.Record.FieldA);
            Assert.Equal("Invalid single payload", failure.ErrorMessage);
            Assert.Equal("SINGLE_ERR", failure.Error?.Code);
        });
    }
    [Fact]
    public async Task UpdateAsync_WhenRecordIdIsNull_AddsToFailed() {
        var records = new List<SampleModel> {
            new() { FieldA = "NullId", RecordID = null, Revision = 1 }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ThrowsAsync(new KintoneException("Missing ID"));

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        var result = await service.UpdateAsync(records, enableSingleRetryOnError: false);

        Assert.Empty(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.Equal("Missing ID", result.Failed[0].ErrorMessage);
        Assert.Null(result.Failed[0].Record.RecordID);
    }
    [Fact]
    public async Task UpdateAsync_WhenRevisionIsInvalid_AddsToFailed() {
        var records = new List<SampleModel> {
            new() { FieldA = "BadRev", RecordID = "RID001", Revision = -1 }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ThrowsAsync(new KintoneException("Invalid revision") {
                Error = new KintoneError { Code = "REV_ERR", Message = "Revision number is invalid" }
            });

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        var result = await service.UpdateAsync(records, enableSingleRetryOnError: false);

        Assert.Empty(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.Equal("Revision number is invalid", result.Failed[0].Error?.Message);
    }
    [Fact]
    public async Task UpdateAsync_WhenResponseIsEmpty_ReturnsEmptySucceeded() {
        var records = new List<SampleModel> {
            new() { FieldA = "NoResponse", RecordID = "RID0001", Revision = 1 }
        };

        var mockRepo = new Mock<IKintoneRepository>();

        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{}"); // 空のレスポンスとして解釈される構造

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        var result = await service.UpdateAsync(records);

        // ✅ 成功レコードが0件であることを確認
        Assert.Empty(result.Succeeded);

        // ✅ 失敗にも分類されていないことを確認（レスポンスが空でもエラーではない）
        Assert.Empty(result.Failed);
    }
    [Fact]
    public async Task UpdateAsync_WhenBulkFails_LogsWarningMessage() {
        var testRecords = new List<SampleModel> {
            new() { FieldA = "BulkFail1", RecordID = "RID001", Revision = 1 },
            new() { FieldA = "BulkFail2", RecordID = "RID002", Revision = 1 }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ThrowsAsync(new KintoneException("Bulk failure"));

        var mockLogger = new Mock<ILogger<KintoneModelCrudService>>();

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { }),
            null,
            mockLogger.Object
        );

        var result = await service.UpdateAsync(testRecords, enableSingleRetryOnError: false);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulk update failed")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }
    [Fact]
    public async Task UpdateAsync_WhenSingleRetryFails_LogsErrorMessage() {
        var testRecords = new List<SampleModel> {
            new() { FieldA = "RetryFail", RecordID = "RID001", Revision = 1 }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.SetupSequence(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ThrowsAsync(new KintoneException("Bulk failed")) // バルクで失敗
            .ThrowsAsync(new KintoneException("Retry failed")); // 単件でも失敗

        var mockLogger = new Mock<ILogger<KintoneModelCrudService>>();

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { }),
            null,
            mockLogger.Object
        );

        var result = await service.UpdateAsync(testRecords, enableSingleRetryOnError: true);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Single update failed")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }
    #endregion
}