using System.Text.Json;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Converters;
using KintoneNetLibrary.Infrastructure.Helpers;
using KintoneNetLibrary.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Services;

/// <summary>
/// KintoneModelCrudService の FindAsync メソッドに関するユニットテストクラスです。
/// </summary>
public class KintoneModelCrudServiceFindTests {
    #region <<Test methods>>
    /// <summary>
    /// FindAsync メソッドに単一のレコードIDを渡した場合、そのIDに対応するレコードが正しく返されることをテストします。
    /// </summary>
    [Fact]
    public async Task FindAsyncWithSingleIDReturnsSingleRecord() {
        // Arrange
        var testId = "123";
        var testModel = new SampleModel { RecordID = testId, FieldA = "TestValue", FieldB = 456 };
        var wrappedJson = JsonSerializer.Serialize(new {
            record = new Dictionary<string, object> {
                ["$id"] = new { type = "__ID__", value = testModel.RecordID },
                ["FieldA"] = new { type = "SINGLE_LINE_TEXT", value = testModel.FieldA },
                ["FieldB"] = new { type = "NUMBER", value = testModel.FieldB.ToString() },
            }
        });

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo
            .Setup(r => r.FindByIDAsync<SampleModel>(It.IsAny<SampleModel>(), testId))
            .ReturnsAsync(wrappedJson);

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            new JsonSerializerOptions(),
            NullLogger<KintoneTypedCrudService<SampleModel>>.Instance
        );

        // Act
        var result = await service.FindAsync(ids: [testId]);

        // Assert
        var single = Assert.Single(result);
        Assert.Equal(testId, single.RecordID);
        Assert.Equal("TestValue", single.FieldA);
        Assert.Equal(456, single.FieldB);
    }

    /// <summary>
    /// FindAsync メソッドに複数のレコードIDを渡した場合、それらのIDに対応するレコードが正しく返されることをテストします。
    /// </summary>
    [Fact]
    public async Task FindAsyncWithMultipleIDsReturnsMultipleRecords() {
        // Arrange
        var testIds = new[] { "123", "456" };
        var expectedModels = new List<SampleModel> {
            new() { RecordID = "123", FieldA = "ValueA1", FieldB = 100 },
            new() { RecordID = "456", FieldA = "ValueA2", FieldB = 200 } };

        var wrappedJson = JsonSerializer.Serialize(new {
            records = expectedModels.Select(m => new Dictionary<string, object> {
                ["$id"] = new { type = "__ID__", value = m.RecordID },
                ["FieldA"] = new { type = "SINGLE_LINE_TEXT", value = m.FieldA },
                ["FieldB"] = new { type = "NUMBER", value = m.FieldB.ToString() },
            }).ToList()
        });

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo
            .Setup(r => r.FindByIDsAsync<SampleModel>(It.IsAny<SampleModel>(), testIds, null))
            .ReturnsAsync(wrappedJson);

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            new JsonSerializerOptions(),
            NullLogger<KintoneTypedCrudService<SampleModel>>.Instance
        );

        // Act
        var result = await service.FindAsync(ids: testIds);

        // Assert
        Assert.Equal(2, result.Count());

        foreach (var expected in expectedModels) {
            var actual = result.Single(r => r.RecordID == expected.RecordID);
            Assert.Equal(expected.FieldA, actual.FieldA);
            Assert.Equal(expected.FieldB, actual.FieldB);
        }
    }

    /// <summary>
    /// FindAsync メソッドにクエリを渡した場合、そのクエリにマッチするレコードが正しく返されることをテストします。
    /// </summary>
    [Fact]
    public async Task FindAsyncWithQueryReturnsMatchingRecords() {
        // Arrange
        var query = "Title = \"Test Book\"";
        var expectedRecord = new SampleModel {
            RecordID = "12407",
            FieldA = "Test Book",
            FieldB = 1000
        };

        var wrappedJson = JsonSerializer.Serialize(new {
            records = new[] {
            new Dictionary<string, object> {
                ["$id"] = new { type = "__ID__", value = expectedRecord.RecordID },
                ["FieldA"] = new { type = "SINGLE_LINE_TEXT", value = expectedRecord.FieldA },
                ["FieldB"] = new { type = "NUMBER", value = expectedRecord.FieldB }
            }
        }
        });

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo
            .Setup(r => r.FindByQueryAsync<SampleModel>(It.IsAny<SampleModel>(), query))
            .ReturnsAsync(wrappedJson);

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            NullLogger<KintoneTypedCrudService<SampleModel>>.Instance
        );

        // Act
        var result = await service.FindAsync(query: query);

        // Assert
        var single = Assert.Single(result);
        Assert.Equal("12407", single.RecordID);
        Assert.Equal("Test Book", single.FieldA);
        Assert.Equal(1000, single.FieldB);
    }

    /// <summary>
    /// FindAsync メソッドにクエリを渡した場合、そのクエリにマッチするレコードが存在しないとき、空のリストが返されることをテストします。
    /// </summary>
    [Fact]
    public async Task FindAsyncWithQueryButNoMatchesReturnsEmptyList() {
        // Arrange
        var query = "Title = \"NonExistent Book\"";

        var emptyJson = JsonSerializer.Serialize(new {
            records = Array.Empty<object>()
        });

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo
            .Setup(r => r.FindByQueryAsync<SampleModel>(It.IsAny<SampleModel>(), query))
            .ReturnsAsync(emptyJson);

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            NullLogger<KintoneTypedCrudService<SampleModel>>.Instance
        );

        // Act
        var result = await service.FindAsync(query: query);
        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// FindAsync メソッドにクエリを渡した場合、そのクエリにマッチする複数のレコードが正しく返されることをテストします。
    /// </summary>
    [Fact]
    public async Task FindAsyncWithQueryMatchingMultipleRecordsReturnsAllRecords() {
        // Arrange
        var query = "Category = \"Tech\"";

        var sampleJson = JsonSerializer.Serialize(new {
            records = new[] {
                new {
                    id = "1",
                    Title = new { value = "C# Clean Architecture" },
                    Category = new { value = "Tech" }
                },
                new {
                    id = "2",
                    Title = new { value = "Docker Testing Strategies" },
                    Category = new { value = "Tech" }
                }
            }
        });

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo
            .Setup(r => r.FindByQueryAsync<SampleModel3>(It.IsAny<SampleModel3>(), query))
            .ReturnsAsync(sampleJson);

        var service = new KintoneTypedCrudService<SampleModel3>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            NullLogger<KintoneTypedCrudService<SampleModel3>>.Instance
        );

        // Act
        var result = await service.FindAsync(query: query);
        // Assert
        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, r => r.Title == "C# Clean Architecture");
        Assert.Contains(list, r => r.Title == "Docker Testing Strategies");
    }

    /// <summary>
    /// FindAsync メソッドにレコードIDもクエリも渡さなかった場合、すべてのレコードが正しく返されることをテストします。
    /// </summary>
    [Fact]
    public async Task FindAsyncWithoutIdsOrQueryReturnsAllRecords() {
        // Arrange
        var allRecordsJson = JsonSerializer.Serialize(new {
            records = new[] {
                new {
                    id = "10",
                    Title = new { value = "Refactoring Legacy Code" }
                },
                new {
                    id = "11",
                    Title = new { value = "Kintone API Integration Tips" }
                }
            }
        });

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo
            .Setup(r => r.FindAllAsync<SampleModel3>(It.IsAny<SampleModel3>(), It.IsAny<IList<string>?>()))
            .ReturnsAsync(allRecordsJson);

        var service = new KintoneTypedCrudService<SampleModel3>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            NullLogger<KintoneTypedCrudService<SampleModel3>>.Instance
        );

        // Act
        var result = await service.FindAsync();
        // Assert
        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, r => r.Title == "Refactoring Legacy Code");
        Assert.Contains(list, r => r.Title == "Kintone API Integration Tips");
    }

    /// <summary>
    /// FindAsync メソッドにクエリを渡した場合、そのクエリにマッチするレコードが存在するが、Kintone からのレスポンスが不正な JSON であったとき、JSON のパースに失敗して KintoneException がスローされることをテストします。
    /// </summary>
    [Fact]
    public async Task FindAsyncWhenJsonExceptionThrownLogsErrorAndThrowsKintoneException() {
        // Arrange
        var loggerMock = new Mock<ILogger<KintoneTypedCrudService<SampleModel>>>();
        var faultyJson = "{ invalid json }"; // 故意に壊れたJSON

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo
            .Setup(r => r.FindByQueryAsync<SampleModel>(It.IsAny<SampleModel>(), It.IsAny<string>()))
            .ReturnsAsync(faultyJson);

        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            loggerMock.Object
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(async () =>
            await service.FindAsync(query: "Title = \"Invalid\"")
        );

        Assert.IsAssignableFrom<JsonException>(ex.InnerException);
        Assert.Equal("Failed to parse Kintone JSON response.", ex.Message);

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v != null &&
                    v.ToString()!.Contains("JSON deserialization failed") &&
                    v.ToString()!.Contains(nameof(SampleModel))),
                It.IsAny<Exception?>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);

        // loggerMock.Verify(l => l.LogInformation("FindAsync() - Start"), Times.Once);
        TestLogHelper.VerifyLog(loggerMock, LogLevel.Information, "FindAsync() - Start", Times.Once());
        TestLogHelper.VerifyLog(loggerMock, LogLevel.Information, "FindAsync() - Finish", Times.Once());
    }

    /// <summary>
    /// FindAsync メソッドにクエリを渡した場合、そのクエリにマッチするレコードが存在するが、Kintone からのレスポンスの処理中に予期しない例外が発生したとき、その例外が KintoneException にラップされてスローされることをテストします。
    /// </summary>
    [Fact]
    public async Task FindAsyncWhenUnexpectedExceptionThrownLogsErrorAndThrowsKintoneException() {
        // Arrange
        var loggerMock = new Mock<ILogger<KintoneTypedCrudService<SampleModel>>>();

        // 例外を強制的に発生させるためのパラメータを注入
        var ids = new List<string> { "1", "2" };
        var query = "force-exception";

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo
            .Setup(r => r.FindByIDsAsync<SampleModel>(It.IsAny<SampleModel>(), ids, null))
            .Throws(new InvalidOperationException("Simulated unexpected failure"));

        // service にモック注入
        var service = new KintoneTypedCrudService<SampleModel>(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            loggerMock.Object
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() =>
            service.FindAsync(ids, query));

        Assert.Equal("An unexpected error occurred while retrieving Kintone records.", ex.Message);
        Assert.IsType<InvalidOperationException>(ex.InnerException);

        // Log の検証
        TestLogHelper.VerifyLog<KintoneTypedCrudService<SampleModel>>(loggerMock, LogLevel.Error, $"Unexpected error occurred in FindAsync<{nameof(SampleModel)}>", Times.Once());
    }

    #endregion
}

/// <summary>
/// テスト用のサンプルモデルクラスです。AppID と Access はダミー値を設定しています。
/// </summary>
internal class SampleModel3 : KintoneModelBase<SampleModel3> {
    public override int AppID { get; init; } = 7778;
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("dummyDomain", "dummyApiToken");

    [KintoneItem(fieldCode: "Title", fieldType: KintoneFieldType.SingleLineText)]
    public string Title { get; set; } = string.Empty;
    [KintoneItem(fieldCode: "Category", fieldType: KintoneFieldType.SingleLineText)]
    public string Category { get; set; } = string.Empty;
}