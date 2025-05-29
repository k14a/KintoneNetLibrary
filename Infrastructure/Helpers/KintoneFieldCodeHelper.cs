using System.Reflection;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Helpers;

public static class KintoneFieldCodeHelper {
    public static string[] GetKintoneFieldCodes(this Type type) {
        return type.GetProperties()
            .Select(prop => prop.GetCustomAttribute<KintoneItemAttribute>())
            .Where(attr => attr != null && !string.IsNullOrEmpty(attr.FieldCode) && attr.FieldCode.Length <= KintoneConstants.KintoneFieldCodeMaxLength)
            .Select(attr => attr.FieldCode)
            .ToArray();
    }
}
