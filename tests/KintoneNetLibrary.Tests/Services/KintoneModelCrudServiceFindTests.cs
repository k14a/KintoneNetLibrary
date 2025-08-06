using System.Text.Json;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
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

public class KintoneModelCrudServiceFindTests {
    #region <<Test methods>>
    [Fact]
    public async Task FindAsync_WithSingleID_ReturnsSingleRecord() {
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

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            new JsonSerializerOptions(),
            NullLogger<KintoneModelCrudService>.Instance
        );

        // Act
        var result = await service.FindAsync<SampleModel>(ids: [testId]);

        // Assert
        var single = Assert.Single(result);
        Assert.Equal(testId, single.RecordID);
        Assert.Equal("TestValue", single.FieldA);
        Assert.Equal(456, single.FieldB);
    }
    [Fact]
    public async Task FindAsync_WithMultipleIDs_ReturnsMultipleRecords() {
        // Arrange
        var testIds = new[] { "123", "456" };
        var expectedModels = new List<SampleModel> {
        new() { RecordID = "123", FieldA = "ValueA1", FieldB = 100 },
        new() { RecordID = "456", FieldA = "ValueA2", FieldB = 200 }
    };

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

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            new JsonSerializerOptions(),
            NullLogger<KintoneModelCrudService>.Instance
        );

        // Act
        var result = await service.FindAsync<SampleModel>(ids: testIds);

        // Assert
        Assert.Equal(2, result.Count());

        foreach (var expected in expectedModels) {
            var actual = result.Single(r => r.RecordID == expected.RecordID);
            Assert.Equal(expected.FieldA, actual.FieldA);
            Assert.Equal(expected.FieldB, actual.FieldB);
        }
    }
    [Fact]
    public async Task FindAsync_WithQuery_ReturnsMatchingRecords() {
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

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            NullLogger<KintoneModelCrudService>.Instance
        );

        // Act
        var result = await service.FindAsync<SampleModel>(query: query);

        // Assert
        var single = Assert.Single(result);
        Assert.Equal("12407", single.RecordID);
        Assert.Equal("Test Book", single.FieldA);
        Assert.Equal(1000, single.FieldB);
    }
    [Fact]
    public async Task FindAsync_WithQueryButNoMatches_ReturnsEmptyList() {
        // Arrange
        var query = "Title = \"NonExistent Book\"";

        var emptyJson = JsonSerializer.Serialize(new {
            records = Array.Empty<object>()
        });

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo
            .Setup(r => r.FindByQueryAsync<SampleModel>(It.IsAny<SampleModel>(), query))
            .ReturnsAsync(emptyJson);

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            NullLogger<KintoneModelCrudService>.Instance
        );

        // Act
        var result = await service.FindAsync<SampleModel>(query: query);

        // Assert
        Assert.Empty(result);
    }
    [Fact]
    public async Task FindAsync_WithQueryMatchingMultipleRecords_ReturnsAllRecords() {
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

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            NullLogger<KintoneModelCrudService>.Instance
        );

        // Act
        var result = await service.FindAsync<SampleModel3>(query: query);

        // Assert
        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, r => r.Title == "C# Clean Architecture");
        Assert.Contains(list, r => r.Title == "Docker Testing Strategies");
    }
    [Fact]
    public async Task FindAsync_WithoutIdsOrQuery_ReturnsAllRecords() {
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

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            NullLogger<KintoneModelCrudService>.Instance
        );

        // Act
        var result = await service.FindAsync<SampleModel3>();

        // Assert
        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, r => r.Title == "Refactoring Legacy Code");
        Assert.Contains(list, r => r.Title == "Kintone API Integration Tips");
    }
    [Fact]
    public async Task FindAsync_WhenJsonExceptionThrown_LogsErrorAndThrowsKintoneException() {
        // Arrange
        var loggerMock = new Mock<ILogger<KintoneModelCrudService>>();
        var faultyJson = "{ invalid json }"; // 故意に壊れたJSON

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo
            .Setup(r => r.FindByQueryAsync<SampleModel>(It.IsAny<SampleModel>(), It.IsAny<string>()))
            .ReturnsAsync(faultyJson);

        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            loggerMock.Object
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(async () =>
            await service.FindAsync<SampleModel>(query: "Title = \"Invalid\"")
        );

        Assert.IsAssignableFrom<JsonException>(ex.InnerException);
        Assert.Equal("Failed to parse Kintone JSON response.", ex.Message);

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v != null &&
                    v.ToString().Contains("JSON deserialization failed") &&
                    v.ToString().Contains(nameof(SampleModel))),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);

        // loggerMock.Verify(l => l.LogInformation("FindAsync() - Start"), Times.Once);
        TestLogHelper.VerifyLog(loggerMock, LogLevel.Information, "FindAsync() - Start", Times.Once());
        TestLogHelper.VerifyLog(loggerMock, LogLevel.Information, "FindAsync() - Finish", Times.Once());
    }
    [Fact]
    public async Task FindAsync_WhenUnexpectedExceptionThrown_LogsErrorAndThrowsKintoneException() {
        // Arrange
        var loggerMock = new Mock<ILogger<KintoneModelCrudService>>();

        // 例外を強制的に発生させるためのパラメータを注入
        var ids = new List<string> { "1", "2" };
        var query = "force-exception";

        var mockRepo = new Mock<IKintoneRepository>();
        mockRepo
            .Setup(r => r.FindByIDsAsync<SampleModel>(It.IsAny<SampleModel>(), ids, null))
            .Throws(new InvalidOperationException("Simulated unexpected failure"));

        // service にモック注入
        var service = new KintoneModelCrudService(
            mockRepo.Object,
            Options.Create(new KintoneExecutionOptions { MaxConcurrency = 2 }),
            KintoneJsonOptions.Default,
            loggerMock.Object
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() =>
            service.FindAsync<SampleModel>(ids, query));

        Assert.Equal("An unexpected error occurred while retrieving Kintone records.", ex.Message);
        Assert.IsType<InvalidOperationException>(ex.InnerException);

        // Log の検証
        TestLogHelper.VerifyLog<KintoneModelCrudService>(loggerMock, LogLevel.Error, $"Unexpected error occurred in FindAsync<{nameof(SampleModel)}>", Times.Once());
    }

    #endregion
}

internal class SampleModel3 : KintoneModelBase<SampleModel3> {
    public override int AppID => 7778;
    public override KintoneAccessBase? Access { get; set; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

    [KintoneItem(fieldCode: "Title", fieldType: KintoneFieldType.SingleLineText)]
    public string Title { get; set; } = string.Empty;
    [KintoneItem(fieldCode: "Category", fieldType: KintoneFieldType.SingleLineText)]
    public string Category { get; set; } = string.Empty;
}