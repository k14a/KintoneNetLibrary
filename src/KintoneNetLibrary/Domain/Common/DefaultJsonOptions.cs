using System.Text.Json;
using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Common;

/// <summary>
/// DefaultJsonOptionsクラスは、KintoneNetLibraryで使用されるデフォルトのJSONシリアライゼーションオプションを提供します。
/// このクラスは、JSONのプロパティ名をキャメルケースに変換し、null値を無視する設定を含んでいます。
/// </summary>
/// <remarks>
/// このクラスは、KintoneNetLibraryの他の部分で使用されるJSONシリアライゼーションの設定を一元管理するために使用されます。
/// これにより、コードの一貫性が保たれ、JSONのシリアライゼーションとデシリアライゼーションの動作が統一されます。
/// </remarks>
public static class DefaultJsonOptions {
    /// <summary>
    /// デフォルトのJSONシリアライゼーションオプションを取得します。
    /// このオプションは、プロパティ名をキャメルケースに変換し、null値を無視する設定が含まれています。
    /// </summary>
    /// <remarks>
    /// このオプションは、KintoneNetLibraryの他の部分で使用されるJSONシリアライゼーションの設定を提供します。
    /// これにより、JSONのプロパティ名がキャメルケースに変換され、null値がシリアライズされないようになります。
    /// </remarks>
    public static readonly JsonSerializerOptions Default = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
