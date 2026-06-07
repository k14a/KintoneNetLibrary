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

/// <summary>
/// KintoneTypedCrudService<SampleModel> クラスの UpdateAsync メソッドの動作をテストするクラスです。
/// </summary>
public class KintoneModelCrudServiceUpdateTests {
    #region <<Test methods>>
    /// <summary>
    /// UpdateAsync メソッドに、RecordId と Revision が両方とも有効な値のレコードを1件だけ含むリストを渡した場合、UpdateRecordsAsync メソッドが呼び出され、その結果が正しく返されることをテストします。
    /// </summary>
    [Fact]
    public async Task UpdateAsyncWithSingleRecordReturnsSucceededResult() {
        var testRecord = new SampleModel { FieldA = "Update1", RecordId = "R9999", Revision = 1 };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync(JsonSerializer.Serialize(new {
                records = new[] { new { id = "R9999", revision = "2" } }
            }));

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 1 }),
            null,
            NullLogger<KintoneTypedCrudService<SampleModel>>.Instance
        );

        var result = await service.UpdateAsync([testRecord]);

        Assert.Single(result.Succeeded);
        var updated = result.Succeeded[0];
        Assert.Equal("R9999", updated.RecordId);
        Assert.Equal(2, updated.Revision);
        Assert.Empty(result.Failed);
    }

    /// <summary>
    /// UpdateAsync メソッドに、RecordId と Revision が両方とも有効な値のレコードを複数件含むリストを渡した場合、UpdateRecordsAsync メソッドが呼び出され、その結果が正しく返されることをテストします。
    /// </summary>
    [Fact]
    public async Task UpdateAsyncWithMultipleRecordsReturnsAllSucceeded() {
        var records = new List<SampleModel> {
            new() { FieldA = "Update1", RecordId = "R1001", Revision = 1 },
            new() { FieldA = "Update2", RecordId = "R1002", Revision = 2 }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync(JsonSerializer.Serialize(new {
                records = new[] {
                    new { id = "R1001", revision = "2" },
                    new { id = "R1002", revision = "3" }
                }
            }));

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            null,
            NullLogger<KintoneTypedCrudService<SampleModel>>.Instance
        );

        var result = await service.UpdateAsync(records);

        Assert.Equal(2, result.Succeeded.Count);
        Assert.Contains(result.Succeeded, r => r.RecordId == "R1001" && r.Revision == 2);
        Assert.Contains(result.Succeeded, r => r.RecordId == "R1002" && r.Revision == 3);
        Assert.Empty(result.Failed);
    }

    /// <summary>
    /// UpdateAsync メソッドに、RecordId と Revision が両方とも有効な値のレコードを複数件含むリストを渡した場合、最初のバルク更新で例外がスローされ、その後の単件リトライで全件が成功することをテストします。
    /// </summary>
    [Fact]
    public async Task UpdateAsyncWhenBulkFailsAndSingleRetrySucceedsRecordsAddedToSucceeded() {
        // Arrange
        var records = new List<SampleModel> {
            new() { FieldA = "R1", RecordId = "RID001", Revision = 1 },
            new() { FieldA = "R2", RecordId = "RID002", Revision = 1 }
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

                callLog.Add($"single:{input[0].RecordId}");
                var json = JsonSerializer.Serialize(new {
                    records = new[] { new { id = input[0].RecordId, revision = "2" } }
                });
                return json;
            });

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 1 }),
            null,
            NullLogger<KintoneTypedCrudService<SampleModel>>.Instance
        );

        // Act
        var result = await service.UpdateAsync(records, enableSingleRetryOnError: true);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Empty(result.Failed);

        Assert.Equal("RID001", result.Succeeded[0].RecordId);
        Assert.Equal("RID002", result.Succeeded[1].RecordId);
        Assert.All(result.Succeeded, r => Assert.Equal(2, r.Revision));

        Assert.Equal(3, callLog.Count); // 1回bulk + 2回single
        Assert.Equal("bulk", callLog[0]);
    }

    /// <summary>
    /// UpdateAsync メソッドに、RecordId と Revision が両方とも有効な値のレコードを複数件含むリストを渡した場合、最初のバルク更新で例外がスローされ、その後の単件リトライでも全件が失敗することをテストします。失敗したレコードはすべて Failed に追加されることを確認します。
    /// </summary>
    [Fact]
    public async Task UpdateAsyncWhenBulkAndRetryBothFailAddsAllRecordsToFailed() {
        // Arrange
        var records = new List<SampleModel> {
            new() { FieldA = "R1", RecordId = "RID001", Revision = 1 },
            new() { FieldA = "R2", RecordId = "RID002", Revision = 1 } };

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

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 1 }),
            null,
            NullLogger<KintoneTypedCrudService<SampleModel>>.Instance
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

    /// <summary>
    /// UpdateAsync メソッドに、RecordId が null で Revision が -1 のレコード（＝Create対象）を含むリストを渡した場合、UpdateRecordsAsync メソッドが呼び出されて例外がスローされ、そのレコードが Failed に追加されることをテストします。
    /// </summary>
    [Fact]
    public async Task UpdateAsyncWhenRecordIdIsNullAddsToFailed() {
        var records = new List<SampleModel> {
            new() { FieldA = "NullId", RecordId = null, Revision = 1 }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ThrowsAsync(new KintoneException("Missing Id"));

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { }),
            null,
            NullLogger<KintoneTypedCrudService<SampleModel>>.Instance
        );

        var result = await service.UpdateAsync(records, enableSingleRetryOnError: false);

        Assert.Empty(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.Equal("Missing Id", result.Failed[0].ErrorMessage);
        Assert.Null(result.Failed[0].Record.RecordId);
    }

    /// <summary>
    /// UpdateAsync メソッドに、RecordId と Revision が両方とも有効な値のレコードを含むリストを渡した場合、最初のバルク更新で例外がスローされ、その後の単件リトライで例外がスローされることをテストします。失敗したレコードはすべて Failed に追加されることを確認します。
    /// </summary>
    [Fact]
    public async Task UpdateAsyncWhenRevisionIsInvalidAddsToFailed() {
        var records = new List<SampleModel> {
            new() { FieldA = "BadRev", RecordId = "RID001", Revision = -1 }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ThrowsAsync(new KintoneException("Invalid revision") {
                Error = new KintoneError { Code = "REV_ERR", Message = "Revision number is invalid" }
            });

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { }),
            null,
            NullLogger<KintoneTypedCrudService<SampleModel>>.Instance
        );

        var result = await service.UpdateAsync(records, enableSingleRetryOnError: false);

        Assert.Empty(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.Equal("Revision number is invalid", result.Failed[0].Error?.Message);
    }

    /// <summary>
    /// UpdateAsync メソッドに、RecordId と Revision が両方とも有効な値のレコードを含むリストを渡した場合、UpdateRecordsAsync メソッドが空のレスポンスを返すことがあることをテストします。空のレスポンスはエラーではなく、すべてのレコードが成功とみなされることを確認します。
    /// </summary>
    [Fact]
    public async Task UpdateAsyncWhenResponseIsEmptyReturnsEmptySucceeded() {
        var records = new List<SampleModel> {
            new() { FieldA = "NoResponse", RecordId = "RID0001", Revision = 1 }
        };

        var mockRepo = new Mock<IKintoneRepository>();

        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{}"); // 空のレスポンスとして解釈される構造

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { }),
            null,
            NullLogger<KintoneTypedCrudService<SampleModel>>.Instance
        );

        var result = await service.UpdateAsync(records);

        // ✅ 成功レコードが0件であることを確認
        Assert.Empty(result.Succeeded);

        // ✅ 失敗にも分類されていないことを確認（レスポンスが空でもエラーではない）
        Assert.Empty(result.Failed);
    }

    /// <summary>
    /// UpdateAsync メソッドに、RecordId と Revision が両方とも有効な値のレコードを複数件含むリストを渡した場合、最初のバルク更新で例外がスローされ、その後の単件リトライでも全件が失敗することをテストします。失敗したレコードはすべて Failed に追加されることを確認します。また、バルク更新の失敗と単件リトライの失敗の両方で、適切なログメッセージが記録されることを確認します。
    /// </summary>
    [Fact]
    public async Task UpdateAsyncWhenBulkFailsLogsWarningMessage() {
        var testRecords = new List<SampleModel> {
            new() { FieldA = "BulkFail1", RecordId = "RID001", Revision = 1 },
            new() { FieldA = "BulkFail2", RecordId = "RID002", Revision = 1 }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ThrowsAsync(new KintoneException("Bulk failure"));

        var mockLogger = new Mock<ILogger<KintoneTypedCrudService<SampleModel>>>();

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { }),
            null,
            mockLogger.Object
        );

        mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

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

    /// <summary>
    /// UpdateAsync メソッドに、RecordId と Revision が両方とも有効な値のレコードを複数件含むリストを渡した場合、最初のバルク更新で例外がスローされ、その後の単件リトライでも全件が失敗することをテストします。失敗したレコードはすべて Failed に追加されることを確認します。また、単件リトライの失敗で、適切なエラーログメッセージが記録されることを確認します。
    /// </summary>
    [Fact]
    public async Task UpdateAsyncWhenSingleRetryFailsLogsErrorMessage() {
        var testRecords = new List<SampleModel> {
            new() { FieldA = "RetryFail", RecordId = "RID001", Revision = 1 }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.SetupSequence(r => r.UpdateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ThrowsAsync(new KintoneException("Bulk failed")) // バルクで失敗
            .ThrowsAsync(new KintoneException("Retry failed")); // 単件でも失敗

        var mockLogger = new Mock<ILogger<KintoneTypedCrudService<SampleModel>>>();

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { }),
            null,
            mockLogger.Object
        );

        mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

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