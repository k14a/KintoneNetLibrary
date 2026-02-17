using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Enums;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.TypeMapper.Python;

/// <summary>
/// PythonTypeMapper の単体テスト。
/// </summary>
public class PythonTypeMapperTests {
    private readonly PythonTypeMapper _mapper = new();

    // -----------------------------
    // 1. useTypeHint = false
    // -----------------------------
    /// <summary>
    /// MapType メソッドが、useTypeHint = false の場合に常に "str" を返すことを検証するテスト。
    /// </summary>
    [Fact]
    public void MapType_NoTypeHint_ReturnsStrAlways() {
        var field = new KintoneFieldSchema { FieldType = KintoneFieldType.Number };
        var result = _mapper.MapType(field, useTypeHint: false);
        Assert.Equal("str", result);
    }

    // -----------------------------
    // 2. useTypeHint = true
    // -----------------------------
    /// <summary>
    /// MapType メソッドが、KintoneFieldSchema の FieldType に基づいて、KintoneNetLibrary を使用する場合の期待される Python 型名を返すことを検証するテスト。
    /// </summary>
    /// <param name="type">Kintone フィールドの種類</param>
    /// <param name="expected">期待される Python 型名</param>
    [Theory]
    [InlineData(KintoneFieldType.SingleLineText, "str")]
    [InlineData(KintoneFieldType.MultiLineText, "str")]
    [InlineData(KintoneFieldType.RichText, "str")]
    public void MapType_StringTypes(KintoneFieldType type, string expected) {
        var field = new KintoneFieldSchema { FieldType = type };
        Assert.Equal(expected, _mapper.MapType(field, true));
    }

    /// <summary>
    /// MapType メソッドが、Number フィールドに対して、DecimalPlaces の値に基づいて "int | None" または "decimal | None" を返すことを検証するテスト。
    /// </summary>
    [Fact]
    public void MapType_Number_ReturnsIntOrNone() {
        var field = new KintoneFieldSchema { FieldType = KintoneFieldType.Number };
        Assert.Equal("int | None", _mapper.MapType(field, true));
    }

    /// <summary>
    /// MapType メソッドが、Date, Time, DateTime フィールドに対して、datetime モジュールの適切な型を "型 | None" 形式で返すことを検証するテスト。
    /// </summary>
    /// <param name="type">Kintone フィールドの種類</param>
    /// <param name="expected">期待される Python 型名</param>
    [Theory]
    [InlineData(KintoneFieldType.Date, "datetime.date | None")]
    [InlineData(KintoneFieldType.Time, "datetime.time | None")]
    [InlineData(KintoneFieldType.DateTime, "datetime.datetime | None")]
    public void MapType_DateTimeTypes(KintoneFieldType type, string expected) {
        var field = new KintoneFieldSchema { FieldType = type };
        Assert.Equal(expected, _mapper.MapType(field, true));
    }

    /// <summary>
    /// MapType メソッドが、UserSelect, GroupSelect, OrganizationSelect フィールドに対して、KintoneNetLibrary の User, Group, Organization クラスを "list[クラス] | None" 形式で返すことを検証するテスト。
    /// </summary>
    /// <param name="type">Kintone フィールドの種類</param>
    /// <param name="expected">期待される Python 型名</param>
    [Theory]
    [InlineData(KintoneFieldType.CheckBox, "list[str]")]
    [InlineData(KintoneFieldType.MultiSelect, "list[str]")]
    public void MapType_MultiValueStringList(KintoneFieldType type, string expected) {
        var field = new KintoneFieldSchema { FieldType = type };
        Assert.Equal(expected, _mapper.MapType(field, true));
    }

    /// <summary>
    /// MapType メソッドが、RadioButton, DropDown フィールドに対して "str | None" を返すことを検証するテスト。
    /// </summary>
    /// <param name="type">Kintone フィールドの種類</param>
    /// <param name="expected">期待される Python 型名</param>
    [Theory]
    [InlineData(KintoneFieldType.RadioButton, "str")]
    [InlineData(KintoneFieldType.DropDown, "str")]
    public void MapType_SingleChoiceString(KintoneFieldType type, string expected) {
        var field = new KintoneFieldSchema { FieldType = type };
        Assert.Equal(expected, _mapper.MapType(field, true));
    }

    /// <summary>
    /// MapType メソッドが、File フィールドに対して "list[File] | None" を返すことを検証するテスト。
    /// </summary>
    [Fact]
    public void MapType_File_ReturnsListFile() {
        var field = new KintoneFieldSchema { FieldType = KintoneFieldType.File };
        Assert.Equal("list[File]", _mapper.MapType(field, true));
    }

    /// <summary>
    /// MapType メソッドが、LinkUrl, LinkTelephone, LinkEmail フィールドに対して "str | None" を返すことを検証するテスト。
    /// </summary>
    /// <param name="type">Kintone フィールドの種類</param>
    /// <param name="expected">期待される Python 型名</param>
    [Theory]
    [InlineData(KintoneFieldType.UserSelect, "list[User]")]
    [InlineData(KintoneFieldType.GroupSelect, "list[Group]")]
    [InlineData(KintoneFieldType.OrganizationSelect, "list[Organization]")]
    public void MapType_UserGroupOrg(KintoneFieldType type, string expected) {
        var field = new KintoneFieldSchema { FieldType = type };
        Assert.Equal(expected, _mapper.MapType(field, true));
    }

    /// <summary>
    /// MapType メソッドが、CheckBox, MultiSelect フィールドに対して "list[str] | None" を返し、RadioButton, DropDown フィールドに対して "str | None" を返すことを検証するテスト。
    /// </summary>
    /// <param name="type">Kintone フィールドの種類</param>
    /// <param name="expected">期待される Python 型名</param>
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
    /// <summary>
    /// MapType メソッドが、SubTable フィールドに対して、クラス名が提供されない場合は "list[dict]" を返すことを検証するテスト。
    /// </summary>
    [Fact]
    public void MapType_SubTable_WithoutClassName_ReturnsListDict() {
        var field = new KintoneFieldSchema { FieldType = KintoneFieldType.SubTable };
        Assert.Equal("list[dict]", _mapper.MapType(field, true));
    }

    /// <summary>
    /// MapType メソッドが、SubTable フィールドに対して、提供されたクラス名を使用して "list[クラス名]" 形式の型名を返すことを検証するテスト。
    /// </summary>
    [Fact]
    public void MapType_SubTable_WithClassName_ReturnsListOfClass() {
        var field = new KintoneFieldSchema { FieldType = KintoneFieldType.SubTable };
        Assert.Equal("list[SubTableOrder]", _mapper.MapType(field, true, "SubTableOrder"));
    }

    // -----------------------------
    // 4. fallback
    // -----------------------------
    /// <summary>
    /// MapType メソッドが、未知の FieldType に対して "str" を返すことを検証するテスト。
    /// </summary>
    [Fact]
    public void MapType_Fallback_ReturnsStr() {
        var field = new KintoneFieldSchema { FieldType = (KintoneFieldType)999 };
        Assert.Equal("str", _mapper.MapType(field, true));
    }
}
