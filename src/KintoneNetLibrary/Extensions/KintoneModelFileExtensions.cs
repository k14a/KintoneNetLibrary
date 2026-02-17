using System.Reflection;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Extensions;

/// <summary>
/// KintoneModelBaseのファイル操作に関する拡張メソッドを提供します。
/// </summary>
public static class KintoneModelFileExtensions {
    /// <summary>
    /// モデルに関連付けられたファイルをダウンロードします。
    /// </summary>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルの型</typeparam>
    /// <param name="model">ダウンロード対象のモデル</param>
    /// <param name="fileService">ファイル操作サービス</param>
    /// <param name="targetDirectory">ダウンロード先のディレクトリ</param>
    /// <param name="overwrite">既存ファイルを上書きするかどうか</param>
    /// <param name="throwIfExists">既存ファイルが存在する場合に例外をスローするかどうか</param>
    /// <returns>ダウンロードされたファイルの情報</returns>
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
    /// <summary>
    /// モデルに関連付けられたファイルをアップロードします。
    /// </summary>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルの型</typeparam>
    /// <param name="model">アップロード対象のモデル</param>
    /// <param name="fileService">ファイル操作サービス</param>
    /// <param name="files">アップロードするファイルのコレクション</param>
    /// <returns>アップロードされたKintoneFileオブジェクトのコレクション</returns>
    public static async Task<IEnumerable<KintoneFile>> UploadFilesAsync<T>(this T model, IKintoneModelFileService<T> fileService, IEnumerable<FileInfo> files) where T : KintoneModelBase<T>, new() {
        var uploaded = await fileService.UploadFilesAsync(model, files);
        await fileService.MapUploadedFilesToModelAsync(model, files);
        return uploaded;
    }
    /// <summary>
    /// プロパティからKintoneFileオブジェクトを抽出します。
    /// </summary>
    /// <param name="prop">抽出対象のプロパティ情報</param>
    /// <param name="model">プロパティを持つモデルオブジェクト</param>
    /// <returns>抽出されたKintoneFileオブジェクトのコレクション</returns>
    private static IEnumerable<KintoneFile> ExtractFiles(PropertyInfo prop, object model) {
        var value = prop.GetValue(model);
        return value switch {
            KintoneFile file => [file],
            IEnumerable<KintoneFile> files => files,
            _ => []
        };
    }
}

