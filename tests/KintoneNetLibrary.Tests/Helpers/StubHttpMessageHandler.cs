namespace KintoneNetLibrary.Tests.Helpers;

public class StubHttpMessageHandler : HttpMessageHandler {
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        return _handler(request);
    }
}
