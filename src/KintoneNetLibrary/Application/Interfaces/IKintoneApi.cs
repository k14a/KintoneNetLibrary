namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// Kintone API インターフェイス（全操作を統合した umbrella インターフェイス）
/// </summary>
public interface IKintoneApi : IKintoneRecordReadApi, IKintoneRecordWriteApi, IKintoneFileApi, IKintoneCursorApi {
}
