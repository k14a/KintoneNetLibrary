using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.TypeMapper.CSharp;

/// <summary>
/// CSharpTypeMapper の単体テスト。
/// </summary>
public class CSharpTypeMapperTests {
    private readonly CSharpTypeMapper _mapper = new();

    // -----------------------------
    // 1. Map(KintoneFieldMetadata)
    // -----------------------------
    /// <summary>
    /// Map メソッドが、KintoneFieldMetadata の FieldType に基づいて期待される C# 型名を返すことを検証するテスト。
    /// </summary>
    /// <param name="type">Kintone フィールドの種類</param>
    /// <param name="expected">期待される C# 型名</param>
    [Theory]
    [InlineData(KintoneFieldType.SingleLineText, "string")]
    [InlineData(KintoneFieldType.MultiLineText, "string")]
    [InlineData(KintoneFieldType.RichText, "string")]
    [InlineData(KintoneFieldType.Number, "decimal?")]
    [InlineData(KintoneFieldType.Date, "KintoneDateOnly")]
    [InlineData(KintoneFieldType.DateTime, "KintoneDateTime")]
    [InlineData(KintoneFieldType.Time, "KintoneTimeOnly")]
    [InlineData(KintoneFieldType.CheckBox, "List<string>")]
    [InlineData(KintoneFieldType.MultiSelect, "List<string>")]
    [InlineData(KintoneFieldType.RadioButton, "string")]
    [InlineData(KintoneFieldType.DropDown, "string")]
    [InlineData(KintoneFieldType.File, "List<KintoneFile>")]
    [InlineData(KintoneFieldType.UserSelect, "List<KintoneUser>")]
    [InlineData(KintoneFieldType.GroupSelect, "List<KintoneGroup>")]
    [InlineData(KintoneFieldType.OrganizationSelect, "List<KintoneOrganization>")]
    public void Map_Metadata_ReturnsExpected(KintoneFieldType type, string expected) {
        var field = new KintoneFieldMetadata { FieldType = type, FieldCode = "dummy" };
        var result = _mapper.Map(field);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Map メソッドが、SubTable フィールドに対して "List<フィールドコード>" 形式の型名を返すことを検証するテスト。
    /// </summary>
    [Fact]
    public void Map_Metadata_SubTable_ReturnsListOfFieldCode() {
        var field = new KintoneFieldMetadata { FieldType = KintoneFieldType.SubTable, FieldCode = "order_items" };
        var result = _mapper.Map(field);
        Assert.Equal("List<order_items>", result);
    }

    // -----------------------------
    // 2. MapType (SubTable)
    // -----------------------------
    /// <summary>
    /// MapType メソッドが、SubTable フィールドに対して、提供されたクラス名を使用して "List<クラス名>" 形式の型名を返すことを検証するテスト。
    /// </summary>
    [Fact]
    public void MapType_SubTable_UsesProvidedClassName() {
        var field = new KintoneFieldSchema { FieldType = KintoneFieldType.SubTable };
        var result = _mapper.MapType(field, useKintoneNetLibrary: false, subTableClassName: "SubTableOrderItems");
        Assert.Equal("List<SubTableOrderItems>", result);
    }

    // -----------------------------
    // 3. MapType (Library mode)
    // -----------------------------
    /// <summary>
    /// MapType メソッドが、KintoneFieldSchema の FieldType に基づいて、KintoneNetLibrary を使用する場合の期待される C# 型名を返すことを検証するテスト。
    /// </summary>
    /// <param name="type">Kintone フィールドの種類</param>
    /// <param name="expected">期待される C# 型名</param>
    [Theory]
    [InlineData(KintoneFieldType.SingleLineText, "string")]
    [InlineData(KintoneFieldType.MultiLineText, "string")]
    [InlineData(KintoneFieldType.RichText, "string")]
    [InlineData(KintoneFieldType.Calc, "string")]
    [InlineData(KintoneFieldType.Date, "KintoneDateOnly")]
    [InlineData(KintoneFieldType.DateTime, "KintoneDateTime")]
    [InlineData(KintoneFieldType.Time, "KintoneTimeOnly")]
    [InlineData(KintoneFieldType.CheckBox, "List<string>")]
    [InlineData(KintoneFieldType.MultiSelect, "List<string>")]
    [InlineData(KintoneFieldType.RadioButton, "string")]
    [InlineData(KintoneFieldType.DropDown, "string")]
    [InlineData(KintoneFieldType.File, "List<KintoneFile>")]
    [InlineData(KintoneFieldType.LinkUrl, "string")]
    [InlineData(KintoneFieldType.LinkTelephone, "string")]
    [InlineData(KintoneFieldType.LinkEmail, "string")]
    public void MapType_LibraryMode_ReturnsExpected(KintoneFieldType type, string expected) {
        var field = new KintoneFieldSchema { FieldType = type, Required = true };
        var result = _mapper.MapType(field, useKintoneNetLibrary: true);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// MapType メソッドが、Number フィールドに対して、DecimalPlaces の値に基づいて "decimal?" または "int?" を返すことを検証するテスト。
    /// </summary>
    [Fact]
    public void MapType_LibraryMode_Number_UsesDecimalOrInt() {
        var decimalField = new KintoneFieldSchema { FieldType = KintoneFieldType.Number, DecimalPlaces = 2 };
        var intField = new KintoneFieldSchema { FieldType = KintoneFieldType.Number, DecimalPlaces = 0 };

        Assert.Equal("decimal?", _mapper.MapType(decimalField, true));
        Assert.Equal("int?", _mapper.MapType(intField, true));
    }

    // -----------------------------
    // 4. MapType (Pure mode)
    // -----------------------------
    /// <summary>
    /// MapType メソッドが、KintoneFieldSchema の FieldType に基づいて、KintoneNetLibrary を使用しない場合の期待される C# 型名を返すことを検証するテスト。
    /// </summary>
    /// <param name="type">Kintone フィールドの種類</param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData(KintoneFieldType.Date, "DateOnly?")]
    [InlineData(KintoneFieldType.DateTime, "DateTime?")]
    [InlineData(KintoneFieldType.Time, "TimeOnly?")]
    [InlineData(KintoneFieldType.File, "List<string>")]
    [InlineData(KintoneFieldType.UserSelect, "List<UserInfo>")]
    [InlineData(KintoneFieldType.GroupSelect, "List<GroupInfo>")]
    [InlineData(KintoneFieldType.OrganizationSelect, "List<OrganizationInfo>")]
    public void MapType_PureMode_ReturnsExpected(KintoneFieldType type, string expected) {
        var field = new KintoneFieldSchema { FieldType = type };
        var result = _mapper.MapType(field, useKintoneNetLibrary: false);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// MapType メソッドが、Number フィールドに対して、DecimalPlaces の値に基づいて "decimal?" または "int?" を返すことを検証するテスト。
    /// </summary>
    [Fact]
    public void MapType_PureMode_Number_UsesDecimalOrInt() {
        var decimalField = new KintoneFieldSchema { FieldType = KintoneFieldType.Number, DecimalPlaces = 3 };
        var intField = new KintoneFieldSchema { FieldType = KintoneFieldType.Number, DecimalPlaces = 0 };

        Assert.Equal("decimal?", _mapper.MapType(decimalField, false));
        Assert.Equal("int?", _mapper.MapType(intField, false));
    }
}
