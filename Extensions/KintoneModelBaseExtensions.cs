using KintoneNetLibrary.Domain.Entities;

public static class KintoneModelFileExtensions
{
    public static async Task<IEnumerable<FileInfo>> DownloadFilesAsync<T>(this T model, IKintoneModelFileService<T> fileService, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false) where T : KintoneModelBase<T>, new()
    {
        // モデル内の KintoneFile[] を探索
        var files = typeof(T).GetProperties()
            .Where(p => typeof(IEnumerable<KintoneFile>).IsAssignableFrom(p.PropertyType))
            .SelectMany(p => (IEnumerable<KintoneFile>?)p.GetValue(model) ?? Enumerable.Empty<KintoneFile>())
            .Where(f => !string.IsNullOrEmpty(f.FileKey))
            .ToList();

        return await fileService.DownloadFilesAsync(model, files, targetDirectory, overwrite, throwIfExists);
    }
    public static async Task<IEnumerable<KintoneFile>> UploadFilesAsync<T>( this T model, IKintoneModelFileService<T> fileService, IEnumerable<FileInfo> files) where T : KintoneModelBase<T>, new() {
        var uploaded = await fileService.UploadFilesAsync(model, files);
        await fileService.MapUploadedFilesToModelAsync(model, files);
        return uploaded;
    }

}
