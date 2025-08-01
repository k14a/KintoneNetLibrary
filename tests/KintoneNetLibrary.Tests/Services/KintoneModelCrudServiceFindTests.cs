using System.Text.Json;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
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

    #endregion
}
