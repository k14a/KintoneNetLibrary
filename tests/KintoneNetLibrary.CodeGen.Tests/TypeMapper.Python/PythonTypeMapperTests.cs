using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Enums;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.TypeMapper.Python;

public class PythonTypeMapperTests {
    private readonly PythonTypeMapper _mapper = new();

    // -----------------------------
    // 1. useTypeHint = false
    // -----------------------------
    [Fact]
    public void MapType_NoTypeHint_ReturnsStrAlways() {
        var field = new KintoneFieldSchema { FieldType = KintoneFieldType.Number };
        var result = _mapper.MapType(field, useTypeHint: false);
        Assert.Equal("str", result);
    }

    // -----------------------------
    // 2. useTypeHint = true
    // -----------------------------
    [Theory]
    [InlineData(KintoneFieldType.SingleLineText, "str")]
    [InlineData(KintoneFieldType.MultiLineText, "str")]
    [InlineData(KintoneFieldType.RichText, "str")]
    public void MapType_StringTypes(KintoneFieldType type, string expected) {
        var field = new KintoneFieldSchema { FieldType = type };
        Assert.Equal(expected, _mapper.MapType(field, true));
    }

    [Fact]
    public void MapType_Number_ReturnsIntOrNone() {
        var field = new KintoneFieldSchema { FieldType = KintoneFieldType.Number };
        Assert.Equal("int | None", _mapper.MapType(field, true));
    }

    [Theory]
    [InlineData(KintoneFieldType.Date, "datetime.date | None")]
    [InlineData(KintoneFieldType.Time, "datetime.time | None")]
    [InlineData(KintoneFieldType.DateTime, "datetime.datetime | None")]
    public void MapType_DateTimeTypes(KintoneFieldType type, string expected) {
        var field = new KintoneFieldSchema { FieldType = type };
        Assert.Equal(expected, _mapper.MapType(field, true));
    }

    [Theory]
    [InlineData(KintoneFieldType.CheckBox, "list[str]")]
    [InlineData(KintoneFieldType.MultiSelect, "list[str]")]
    public void MapType_MultiValueStringList(KintoneFieldType type, string expected) {
        var field = new KintoneFieldSchema { FieldType = type };
        Assert.Equal(expected, _mapper.MapType(field, true));
    }

    [Theory]
    [InlineData(KintoneFieldType.RadioButton, "str")]
    [InlineData(KintoneFieldType.DropDown, "str")]
    public void MapType_SingleChoiceString(KintoneFieldType type, string expected) {
        var field = new KintoneFieldSchema { FieldType = type };
        Assert.Equal(expected, _mapper.MapType(field, true));
    }

    [Fact]
    public void MapType_File_ReturnsListFile() {
        var field = new KintoneFieldSchema { FieldType = KintoneFieldType.File };
        Assert.Equal("list[File]", _mapper.MapType(field, true));
    }

    [Theory]
    [InlineData(KintoneFieldType.UserSelect, "list[User]")]
    [InlineData(KintoneFieldType.GroupSelect, "list[Group]")]
    [InlineData(KintoneFieldType.OrganizationSelect, "list[Organization]")]
    public void MapType_UserGroupOrg(KintoneFieldType type, string expected) {
        var field = new KintoneFieldSchema { FieldType = type };
        Assert.Equal(expected, _mapper.MapType(field, true));
    }

    [Theory]
    [InlineData(KintoneFieldType.LinkUrl, "str")]
    [InlineData(KintoneFieldType.LinkTelephone, "str")]
    [InlineData(KintoneFieldType.LinkEmail, "str")]
    public void MapType_LinkTypes(KintoneFieldType type, string expected) {
        var field = new KintoneFieldSchema { FieldType = type };
        Assert.Equal(expected, _mapper.MapType(field, true));
    }

    // -----------------------------
    // 3. SubTable
    // -----------------------------
    [Fact]
    public void MapType_SubTable_WithoutClassName_ReturnsListDict() {
        var field = new KintoneFieldSchema { FieldType = KintoneFieldType.SubTable };
        Assert.Equal("list[dict]", _mapper.MapType(field, true));
    }

    [Fact]
    public void MapType_SubTable_WithClassName_ReturnsListOfClass() {
        var field = new KintoneFieldSchema { FieldType = KintoneFieldType.SubTable };
        Assert.Equal("list[SubTableOrder]", _mapper.MapType(field, true, "SubTableOrder"));
    }

    // -----------------------------
    // 4. fallback
    // -----------------------------
    [Fact]
    public void MapType_Fallback_ReturnsStr() {
        var field = new KintoneFieldSchema { FieldType = (KintoneFieldType)999 };
        Assert.Equal("str", _mapper.MapType(field, true));
    }
}
