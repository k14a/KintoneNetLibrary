namespace KintoneNetLibrary.Tests.Helpers;

/// <summary>
/// HTTPリクエストをスタブ化するための HttpMessageHandler の実装。
/// テストで HttpClient を使用する際に、実際のHTTPリクエストを送信せずに、指定されたロジックでレスポンスを生成するために使用されます。
/// </summary>
public class StubHttpMessageHandler : HttpMessageHandler {
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    /// <summary>
    /// 指定されたロジックでレスポンスを生成するハンドラー関数を受け取るコンストラクタ。
    /// </summary>
    /// <param name="handler">HTTPリクエストを受け取り、HTTPレスポンスを返す非同期関数</param>
    public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) {
        this._handler = handler;
    }

    /// <summary>
    /// HTTPリクエストを処理し、指定されたロジックでHTTPレスポンスを生成します。
    /// </summary>
    /// <param name="request">HTTPリクエストメッセージ</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>生成されたHTTPレスポンスメッセージ</returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        return this._handler(request);
    }
}
