using System.Reflection;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Extensions;

public static class KintoneModelFileExtensions {
    public static async Task<IEnumerable<FileInfo>> DownloadFilesAsync<T>(this T model, IKintoneModelFileService<T> fileService, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false) where T : KintoneModelBase<T>, new() {
        var type = typeof(T);
        var props = type.GetProperties();

        // 1. 属性ベースで探索
        var attributeFiles = props
            .Where(p => p.GetCustomAttributes(typeof(KintoneItemAttribute), true)
                .OfType<KintoneItemAttribute>()
                .Any(attr => attr.FieldType == KintoneFieldType.File))
            .SelectMany(p => ExtractFiles(p, model));

        // 2. 属性がない場合は型ベースで探索
        var fallbackFiles = props
            .Where(p => typeof(KintoneFile).IsAssignableFrom(p.PropertyType) ||
                        typeof(IEnumerable<KintoneFile>).IsAssignableFrom(p.PropertyType))
            .Where(p => !p.IsDefined(typeof(KintoneItemAttribute), true))
            .SelectMany(p => ExtractFiles(p, model));

        var kintoneFiles = attributeFiles.Concat(fallbackFiles).Where(f => !string.IsNullOrEmpty(f.FileKey));
        return await fileService.DownloadFilesAsync(model, kintoneFiles, targetDirectory, overwrite, throwIfExists);
    }
    public static async Task<IEnumerable<KintoneFile>> UploadFilesAsync<T>(this T model, IKintoneModelFileService<T> fileService, IEnumerable<FileInfo> files) where T : KintoneModelBase<T>, new() {
        var uploaded = await fileService.UploadFilesAsync(model, files);
        await fileService.MapUploadedFilesToModelAsync(model, files);
        return uploaded;
    }
    private static IEnumerable<KintoneFile> ExtractFiles(PropertyInfo prop, object model) {
        var value = prop.GetValue(model);
        return value switch {
            KintoneFile file => [file],
            IEnumerable<KintoneFile> files => files,
            _ => []
        };
    }
}

