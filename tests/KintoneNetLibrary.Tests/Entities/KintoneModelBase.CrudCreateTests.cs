using Castle.Core.Logging;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Entities;

public class KintoneModelBase_CreateTests {
    public class DummyModel : KintoneModelBase<DummyModel> {
        public override int AppID => throw new NotImplementedException();
        public override KintoneAccessBase? Access { get; set; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(fieldCode: "FieldA", fieldType: KintoneFieldType.SingleLineText)]
        public string FieldA { get; set; } = string.Empty;
        [KintoneItem(fieldCode: "FieldB", fieldType: KintoneFieldType.Number)]
        public int FieldB { get; set; }
    }

    #region <<Test methods>>
    [Fact]
    public async Task CreateAsync_CallsServiceWithCorrectArguments_ReturnsExpectedResult() {
        // Arrange
        var model = new DummyModel { FieldA = "DummyText", FieldB = 100 };
        var expectedResult = new KintoneWriteResult<DummyModel> {
            Succeeded = [model]
        };

        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1), false))
            .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        // Act
        var result = await model.CreateAsync();

        // Assert
        Assert.Single(result.Succeeded);
        Assert.False(result.HasFailures);
        mockService.Verify(s => s.CreateAsync(It.Is<IList<DummyModel>>(arr => arr.Count == 1 && arr[0] == model), false), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithRetryFlagTrue_PassesFlagToService() {
        // Arrange
        var mockService = new Mock<IKintoneModelCrudService>();
        mockService
            .Setup(s => s.CreateAsync(It.IsAny<IList<DummyModel>>(), true))
            .ReturnsAsync(new KintoneWriteResult<DummyModel>());

        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        KintoneServiceLocator.Initialize(services.BuildServiceProvider());

        var model = new DummyModel();

        // Act
        var result = await model.CreateAsync(enableSingleRetryOnError: true);

        // Assert
        mockService.Verify(s => s.CreateAsync(It.IsAny<IList<DummyModel>>(), true), Times.Once);
    }
    #endregion
}