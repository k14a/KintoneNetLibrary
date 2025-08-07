using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Entities;

public class KintoneModelBase_DeleteAsyncTests {
    public class DummyModel : KintoneModelBase<DummyModel> {
        public override int AppID => throw new NotImplementedException();
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(fieldCode: "FieldA", fieldType: KintoneFieldType.SingleLineText)]
        public string FieldA { get; set; } = string.Empty;
    }

    #region <<Test methods>>
    [Fact]
    public async Task DeleteAsync_ValidModel_CallsServiceAndReturnsResult() {
        // Arrange
        var model = new DummyModel { RecordID = "1000", FieldA = "ToDelete" };

        var expectedResult = new KintoneDeleteResult {
            Succeeded = [model.RecordID]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync(It.Is<IList<DummyModel>>(list => list.Count == 1 && list[0] == model), true))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.DeleteAsync();

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.DeleteAsync(It.Is<IList<DummyModel>>(list => list.Count == 1), true), Times.Once);
    }
    [Fact]
    public async Task DeleteAsync_WithValidateExistenceFalse_CallsServiceWithFlag() {
        // Arrange
        var model = new DummyModel { RecordID = "1001", FieldA = "ToDelete" };

        var expectedResult = new KintoneDeleteResult {
            Succeeded = [model.RecordID]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync(It.Is<IList<DummyModel>>(list => list.Count == 1 && list[0] == model), false))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.DeleteAsync(validateExistence: false);

        // Assert
        Assert.Single(result.Succeeded);
        mockService.Verify(s => s.DeleteAsync(It.Is<IList<DummyModel>>(list => list.Count == 1), false), Times.Once);
    }
    [Fact]
    public async Task DeleteAsync_DeleteFails_ReturnsFailureResult() {
        // Arrange
        var model = new DummyModel { RecordID = "1002", FieldA = "NG" };

        var expectedResult = new KintoneDeleteResult {
            Failed = [
                new KintoneDeleteFailure {
                    ID = model.RecordID,
                    ErrorMessage = "Record not found"
                }
            ]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync(It.Is<IList<DummyModel>>(list => list.Count == 1 && list[0] == model), true))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.DeleteAsync();

        // Assert
        Assert.Empty(result.Succeeded);
        Assert.Single(result.Failed);
        Assert.Equal("Record not found", result.Failed.First().ErrorMessage);
        Assert.True(result.HasFailures);
    }
    [Fact]
    public async Task DeleteBulkAsync_DeletesMultipleRecordsSuccessfully() {
        // Arrange
        var models = new List<DummyModel> {
            new() { RecordID = "1003", FieldA = "Retry1" },
            new() { RecordID = "1004", FieldA = "Retry2" }
        };

        var expectedResult = new KintoneDeleteResult {
            Succeeded = models.Select(m => m.RecordID).ToList()
        };          

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.DeleteAsync(It.Is<IList<DummyModel>>(arr => arr.SequenceEqual(models)), false))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await DummyModel.DeleteBulkAsync(models);

        // Assert
        Assert.Equal(2, result.Succeeded.Count);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.DeleteAsync(It.Is<IList<DummyModel>>(arr => arr.SequenceEqual(models)), false), Times.Once);
    }
    // [Fact]
    // public async Task DeleteBulkAsync_WithEmptyArray_ShouldNotThrow() {
    //     var model = new BookModel { Access = KintoneAccessFactory.Create("Book") };
    //     await model.DeleteBulkAsync(Array.Empty<int>());
    //     // 成功すればOK
    // }

    // [Fact]
    // public async Task DeleteBulkAsync_WithNonExistentIds_ShouldNotThrow() {
    //     var model = new BookModel { Access = KintoneAccessFactory.Create("Book") };
    //     await model.DeleteBulkAsync(new[] { 999999, 888888 }); // 存在しないID
    //     // 成功すればOK
    // }

    // [Fact]
    // public async Task DeleteBulkAsync_WithMixedValidAndInvalidIds_ShouldDeleteValidOnes() {
    //     var model = new BookModel { Access = KintoneAccessFactory.Create("Book") };
    //     var validIds = await model.AddDummyRecordsAsync(2);
    //     var mixedIds = validIds.Append(999999).ToArray();

    //     await model.DeleteBulkAsync(mixedIds);

    //     var remaining = await model.GetByIdsAsync(validIds);
    //     Assert.Empty(remaining);
    // }
    #endregion
}
