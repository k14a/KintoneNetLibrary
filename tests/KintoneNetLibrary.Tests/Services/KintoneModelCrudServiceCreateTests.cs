using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Converters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Services;

public class KintoneModelCrudServiceCreateTests {
    #region <<Test methods>>
    [Fact]
    public async Task CreateAsync_WithValidRecords_ReturnsSucceededResult() {
        // Arrange
        var testRecords = Enumerable.Range(1, 10)
            .Select(i => new SampleModel { FieldA = $"Value{i}" })
            .ToList();

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo
            .Setup(r => r.CreateRecordsAsync(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync((IList<SampleModel> chunk) => {
                var ids = chunk.Select((_, i) => (1000 + i).ToString()).ToList();
                var revisions = Enumerable.Repeat("1", chunk.Count).ToList();
                return JsonSerializer.Serialize(new { ids, revisions });
            });

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            new JsonSerializerOptions(),
            NullLogger<KintoneModelCrudService>.Instance
        );

        // Act
        var result = await service.CreateAsync(testRecords);

        // Assert
        Assert.Equal(10, result.Succeeded.Count);
        Assert.Empty(result.Failed);
    }
    [Fact]
    public async Task CreateAsync_WhenRepositoryThrowsException_AddsToFailed() {
        // Arrange
        var testRecords = new List<SampleModel> {
            new() { FieldA = "A" },
            new() { FieldA = "B" }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.CreateRecordsAsync(It.IsAny<IList<SampleModel>>()))
            .ThrowsAsync(new Exception("Simulated failure"));

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 1 }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        // Act
        var result = await service.CreateAsync(testRecords);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Equal(2, result.Failed.Count); // 全件失敗としてカウント
    }
    [Fact]
    public async Task CreateAsync_WhenKintoneExceptionOccursAndNoRetry_AddsAllToFailed() {
        var testRecords = Enumerable.Range(1, 2)
            .Select(i => new SampleModel { FieldA = $"Value{i}" }).ToList();

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.CreateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ThrowsAsync(new KintoneException("Bulk error occurred") {
                Error = new KintoneError { Code = "KINTONE_ERROR", Message = "Invalid data" }
            });

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 1 }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        var result = await service.CreateAsync(testRecords, enableSingleRetryOnError: false);

        Assert.Empty(result.Succeeded);
        Assert.Equal(2, result.Failed.Count);
        Assert.All(result.Failed, f => Assert.Equal("Invalid data", f.Error?.Message));
    }
    [Fact]
    public async Task CreateAsync_WhenKintoneExceptionOccursAndRetrySucceeds_AddsToSucceeded() {
        var testRecords = new List<SampleModel> {
        new() { FieldA = "RetryMe" }
    };

        var mockRepo = new Mock<IKintoneRepository>();

        var callCount = 0;
        mockRepo.Setup(r => r.CreateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync((IList<SampleModel> chunk) => {
                callCount++;
                if (callCount == 1) {
                    throw new KintoneException("Bulk failed") {
                        Error = new KintoneError { Code = "KINTONE_ERROR", Message = "Invalid data" }
                    };
                }

                // リトライ時は成功レスポンスを返す
                var json = JsonSerializer.Serialize(new {
                    ids = new[] { "9999" },
                    revisions = new[] { "1" }
                });
                return json;
            });

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 1 }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        var result = await service.CreateAsync(testRecords, enableSingleRetryOnError: true);

        Assert.Single(result.Succeeded);
        Assert.Empty(result.Failed);
        Assert.Equal("9999", result.Succeeded[0].ID);
    }
    [Fact]
    public async Task CreateAsync_WhenBulkFailsAndSingleRetrySucceeds_AllRecordsAddedToSucceeded() {
        // Arrange
        var testRecords = new List<SampleModel> {
            new() { FieldA = "Record1" },
            new() { FieldA = "Record2" }
        };

        var mockRepo = new Mock<IKintoneRepository>();

        var callSequence = new Queue<string>();

        // 1回目の呼び出し（Bulk）では例外
        mockRepo.Setup(r => r.CreateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync((IList<SampleModel> chunk) => {
                if (chunk.Count == 2 && callSequence.Count == 0) {
                    callSequence.Enqueue("bulk");
                    throw new KintoneException("Bulk insert failed") {
                        Error = new KintoneError { Code = "KINTONE_ERROR", Message = "Invalid data" }
                    };
                }

                // シングルレコード時（再試行）は正常な JSON を返す
                var id = "999" + callSequence.Count;
                var json = JsonSerializer.Serialize(new {
                    ids = new[] { id },
                    revisions = new[] { "1" }
                });
                callSequence.Enqueue(id);
                return json;
            });

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 1 }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        // Act
        var result = await service.CreateAsync(testRecords, enableSingleRetryOnError: true);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.Empty(result.Failed);
        Assert.All(result.Succeeded, r => Assert.StartsWith("999", r.ID));
    }
    [Fact]
    public async Task CreateAsync_WhenResponseCountMismatch_ThrowsKintoneException() {
        var testRecords = new List<SampleModel> {
        new() { FieldA = "R1" },
        new() { FieldA = "R2" }
    };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.CreateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync(JsonSerializer.Serialize(new {
                ids = new[] { "1001" }, // 1件のみ
                revisions = new[] { "1" }
            }));

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 1 }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        // await Assert.ThrowsAsync<KintoneException>(async () => await service.CreateAsync(testRecords));
        var result = await service.CreateAsync(testRecords);
        Assert.True(result.HasFailures);
        Assert.Equal(testRecords.Count, result.Failed.Count);
        Assert.All(result.Failed, f => Assert.Equal("Mismatch between the number of request and response records.", f.ErrorMessage));
    }
    [Fact]
    public async Task CreateAsync_WhenRevisionIsInvalid_SetsDefaultRevision() {
        var testRecords = new List<SampleModel> {
            new() { FieldA = "R1" }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.CreateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync(JsonSerializer.Serialize(new {
                ids = new[] { "1234" },
                revisions = new[] { "revX" } // パース不能な文字列
            }));

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        var result = await service.CreateAsync(testRecords);

        Assert.Single(result.Succeeded);
        Assert.Equal("1234", result.Succeeded[0].ID);
        Assert.Equal(-1, result.Succeeded[0].Revision); // TryParse失敗時のデフォルト
    }
    [Fact]
    public async Task CreateAsync_WhenResponseHasNullOrEmptyIds_SetsEmptyStringToId() {
        // Arrange
        var testRecords = new List<SampleModel> {
        new() { FieldA = "R1" },
        new() { FieldA = "R2" },
        new() { FieldA = "R3" }
    };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.CreateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync(JsonSerializer.Serialize(new {
                ids = new string?[] { "123", null, "" },
                revisions = new[] { "1", "1", "1" }
            }));

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        // Act
        var result = await service.CreateAsync(testRecords);

        // Assert
        Assert.Equal(3, result.Succeeded.Count);
        Assert.Empty(result.Failed);

        Assert.Equal("123", result.Succeeded[0].ID);
        Assert.Equal(string.Empty, result.Succeeded[1].ID); // null → empty string
        Assert.Equal(string.Empty, result.Succeeded[2].ID); // "" → empty string

        Assert.All(result.Succeeded, r => Assert.Equal(1, r.Revision));
    }
    [Fact]
    public async Task CreateAsync_WhenCreateRecordsReturnsMalformedJson_AddsToFailed() {
        // Arrange
        var testRecords = new List<SampleModel> {
            new() { FieldA = "BadJson1" },
            new() { FieldA = "BadJson2" }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.CreateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ReturnsAsync("{\"ids\":"); // 壊れたJSON構造

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 1 }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        // Act
        var result = await service.CreateAsync(testRecords);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Equal(2, result.Failed.Count);
        Assert.All(result.Failed, f => Assert.Contains("BadJson", f.Record.FieldA));
    }
    [Fact]
    public async Task CreateAsync_WhenBulkAndRetryBothFail_AddsAllRecordsToFailed() {
        // Arrange
        var testRecords = new List<SampleModel> {
            new() { FieldA = "R1" },
            new() { FieldA = "R2" }
        };

        var mockRepo = new Mock<IKintoneRepository>();

        // Bulk呼び出し → 失敗
        // Single呼び出し → どちらも失敗
        mockRepo.Setup(r => r.CreateRecordsAsync<SampleModel>(It.Is<IList<SampleModel>>(chunk => chunk.Count == 2)))
            .ThrowsAsync(new KintoneException("Bulk failed") {
                Error = new KintoneError { Code = "ERROR_BULK", Message = "Invalid chunk" }
            });

        mockRepo.Setup(r => r.CreateRecordsAsync<SampleModel>(It.Is<IList<SampleModel>>(chunk => chunk.Count == 1)))
            .ThrowsAsync(new KintoneException("Retry failed") {
                Error = new KintoneError { Code = "ERROR_SINGLE", Message = "Invalid record" }
            });

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 1 }),
            null,
            NullLogger<KintoneModelCrudService>.Instance
        );

        // Act
        var result = await service.CreateAsync(testRecords, enableSingleRetryOnError: true);

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Equal(2, result.Failed.Count);

        Assert.All(result.Failed, f => {
            Assert.Equal("Invalid record", f.Error?.Message);
            Assert.Contains("R", f.Record.FieldA);
        });
    }
    [Fact]
    public async Task CreateAsync_WhenBulkFails_LogsWarningMessage() {
        var testRecords = new List<SampleModel> {
            new() { FieldA = "R1" }
        };

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo.Setup(r => r.CreateRecordsAsync<SampleModel>(It.IsAny<IList<SampleModel>>()))
            .ThrowsAsync(new KintoneException("Bulk insert error"));

        var mockLogger = new Mock<ILogger<KintoneModelCrudService>>();

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { }),
            null,
            mockLogger.Object
        );

        var result = await service.CreateAsync(testRecords);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Bulk insert failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    #endregion
}
internal class SampleModel : KintoneModelBase<SampleModel> {
    public override int AppID => 8888;
    public override KintoneAccessBase? Access { get; set; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

    [KintoneItem(fieldCode: "FieldA", fieldType: KintoneFieldType.SingleLineText)]
    public string FieldA { get; set; } = string.Empty;
    [KintoneItem(fieldCode: "FieldB", fieldType: KintoneFieldType.Number)]
    public int FieldB { get; set; }
}