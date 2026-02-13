using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests;

public class TypeMapperTests {
    private readonly CSharpTypeMapper _mapper = new();

    [Fact]
    public void MapSingleLineTextReturnsString() {
        var field = new KintoneFieldMetadata { FieldType = KintoneFieldType.SingleLineText };
        var result = this._mapper.Map(field);
        Assert.Equal("string", result);
    }

    [Fact]
    public void MapNumberReturnsDecimalNullable() {
        var field = new KintoneFieldMetadata { FieldType = KintoneFieldType.Number };
        var result = this._mapper.Map(field);
        Assert.Equal("decimal?", result);
    }

    [Fact]
    public void MapDateReturnsKintoneDateTime() {
        var field = new KintoneFieldMetadata { FieldType = KintoneFieldType.Date };
        var result = this._mapper.Map(field);
        Assert.Equal("KintoneDateOnly", result);
    }

    [Fact]
    public void MapDateTimeReturnsKintoneDateTime() {
        var field = new KintoneFieldMetadata { FieldType = KintoneFieldType.DateTime };
        var result = this._mapper.Map(field);
        Assert.Equal("KintoneDateTime", result);
    }

    [Fact]
    public void MapTimeReturnsKintoneTimeOnly() {
        var field = new KintoneFieldMetadata { FieldType = KintoneFieldType.Time };
        var result = this._mapper.Map(field);
        Assert.Equal("KintoneTimeOnly", result);
    }

    [Fact]
    public void MapCheckBoxReturnsListOfString() {
        var field = new KintoneFieldMetadata { FieldType = KintoneFieldType.CheckBox };
        var result = this._mapper.Map(field);
        Assert.Equal("List<string>", result);
    }

    [Fact]
    public void MapMultiSelectReturnsListOfString() {
        var field = new KintoneFieldMetadata { FieldType = KintoneFieldType.MultiSelect };
        var result = this._mapper.Map(field);
        Assert.Equal("List<string>", result);
    }

    [Fact]
    public void MapFileReturnsListOfKintoneFile() {
        var field = new KintoneFieldMetadata { FieldType = KintoneFieldType.File };
        var result = this._mapper.Map(field);
        Assert.Equal("List<KintoneFile>", result);
    }

    [Fact]
    public void MapUserSelectReturnsListOfKintoneUser() {
        var field = new KintoneFieldMetadata { FieldType = KintoneFieldType.UserSelect };
        var result = this._mapper.Map(field);
        Assert.Equal("List<KintoneUser>", result);
    }

    [Fact]
    public void MapGroupSelectReturnsListOfKintoneGroup() {
        var field = new KintoneFieldMetadata { FieldType = KintoneFieldType.GroupSelect };
        var result = this._mapper.Map(field);
        Assert.Equal("List<KintoneGroup>", result);
    }

    [Fact]
    public void MapOrganizationSelectReturnsListOfKintoneOrganization() {
        var field = new KintoneFieldMetadata { FieldType = KintoneFieldType.OrganizationSelect };
        var result = this._mapper.Map(field);
        Assert.Equal("List<KintoneOrganization>", result);
    }
}
