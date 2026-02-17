namespace KintoneNetLibrary.Tests.Helpers;

/// <summary>
/// シーク不可のストリームをシミュレートします。
/// </summary>
public class NonSeekableStream : MemoryStream {
    public override bool CanSeek => false;
}

/// <summary>
/// 指定された長さを持つダミーストリームを提供します（実際のデータは保持しません）。
/// </summary>
public class FakeLargeStream : Stream {
    private long _position;
    private readonly long _length;

    public FakeLargeStream(long length) {
        this._length = length;
        this._position = 0;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;

    public override long Length => this._length;

    public override long Position {
        get => this._position;
        set => this._position = value;
    }

    public override void Flush() { }

    /// <summary>
    /// 指定されたバッファに、実際のデータは書き込まずに、常に0バイトを返す読み取り操作をシミュレートします。
    /// </summary>
    /// <param name="buffer">読み取り先のバッファ</param>
    /// <param name="offset">バッファ内の書き込み開始位置</param>
    /// <param name="count">読み取るバイト数</param>
    /// <returns>実際に読み取ったバイト数（常に0）</returns>
    public override int Read(byte[] buffer, int offset, int count) {
        var remaining = this._length - this._position;
        var toRead = Math.Min(remaining, count);
        Array.Clear(buffer, offset, (int)toRead);
        this._position += toRead;
        return (int)toRead;
    }

    /// <summary>
    /// シーク操作をシミュレートしますが、実際のデータは存在しないため、常に0バイトを返す読み取り操作を維持します。
    /// </summary>
    /// <param name="offset">シークするオフセット</param>
    /// <param name="origin">シークの基準位置</param>
    /// <returns>新しいストリーム位置</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public override long Seek(long offset, SeekOrigin origin) {
        return this._position = origin switch {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => this._position + offset,
            SeekOrigin.End => this._length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
    }

    /// <summary>
    /// ストリームの長さを変更することはサポートされていないため、常に例外をスローします。
    /// </summary>
    /// <param name="value">新しいストリームの長さ</param>
    /// <exception cref="NotSupportedException">ストリームの長さの変更はサポートされていません。</exception>
    public override void SetLength(long value) =>
        throw new NotSupportedException();

    /// <summary>
    /// 書き込み操作はサポートされていないため、常に例外をスローします。
    /// </summary>
    /// <param name="buffer">書き込み元のバッファ</param>
    /// <param name="offset">バッファ内の読み取り開始位置</param>
    /// <param name="count">書き込むバイト数</param>
    /// <exception cref="NotSupportedException">書き込み操作はサポートされていません。</exception>
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}

/// <summary>
/// Read操作を遅延させるストリーム。
/// 遅延時間はコンストラクタで指定可能。
/// </summary>
public class SlowStream : MemoryStream {
    private readonly int _delayMilliseconds;

    /// <summary>
    /// 指定されたバッファを使用してストリームを初期化し、Read操作に遅延を追加します。
    /// </summary>
    /// <param name="buffer">初期化に使用するバッファ</param>
    /// <param name="delayMilliseconds">Read操作に追加する遅延時間（ミリ秒）</param>
    public SlowStream(byte[] buffer, int delayMilliseconds = 100) : base(buffer) {
        this._delayMilliseconds = delayMilliseconds;
    }

    /// <summary>
    /// ReadAsyncをオーバーライドして、指定された遅延時間を待機してから実際の読み取りを行います。
    /// </summary>
    /// <param name="buffer">読み取り先のバッファ</param>
    /// <param name="offset">バッファ内の書き込み開始位置</param>
    /// <param name="count">読み取るバイト数</param>
    /// <param name="cancellationToken">操作のキャンセルを通知するトークン</param>
    /// <returns>実際に読み取ったバイト数</returns>
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
        await Task.Delay(this._delayMilliseconds, cancellationToken);
        return await base.ReadAsync(buffer, offset, count, cancellationToken);
    }
}

/// <summary>
/// 一定バイト読み込み後に例外をスローするストリーム。
/// </summary>
public class FaultyStream : MemoryStream {
    private readonly long _failAfterBytes;
    private long _totalRead;

    /// <summary>
    /// 指定されたバッファを使用してストリームを初期化し、指定されたバイト数を読み込んだ後に例外をスローするように設定します。
    /// </summary>
    /// <param name="buffer">初期化に使用するバッファ</param>
    /// <param name="failAfterBytes">指定されたバイト数を読み込んだ後に例外をスローするバイト数</param>
    public FaultyStream(byte[] buffer, long failAfterBytes) : base(buffer) {
        this._failAfterBytes = failAfterBytes;
        this._totalRead = 0;
    }

    /// <summary>
    /// 指定されたバイト数を読み込んだ後にIOExceptionをスローするようにオーバーライドされたReadメソッド。
    /// </summary>
    /// <param name="buffer">読み取り先のバッファ</param>
    /// <param name="offset">バッファ内の書き込み開始位置</param>
    /// <param name="count">読み取るバイト数</param>
    /// <returns>実際に読み取ったバイト数</returns>
    /// <exception cref="IOException">指定されたバイト数を読み込んだ後にスローされる例外</exception>
    public override int Read(byte[] buffer, int offset, int count) {
        if (this._totalRead >= this._failAfterBytes) {
            throw new IOException("FaultyStream: 読み込み中に意図的な例外をスローしました。");
        }

        int toRead = count;

        if (this._totalRead + toRead > this._failAfterBytes) {
            toRead = (int)(this._failAfterBytes - this._totalRead);
        }

        int bytesRead = base.Read(buffer, offset, toRead);
        this._totalRead += bytesRead;

        return bytesRead;
    }
}

/// <summary>
/// 常に0バイトを返す空ストリーム。
/// </summary>
public class EmptyStream : Stream {
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;

    public override long Length => 0;
    public override long Position { get => 0; set => throw new NotSupportedException(); }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) => 0;

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

/// <summary>
/// 指定されたバイト数を読み込んだ後にストリームが切断されたかのようにIOExceptionをスローするストリーム。
/// </summary>
public class CutoffStream : Stream {
    private readonly MemoryStream _baseStream;
    private readonly long _cutoff;
    private long _readBytes;

    /// <summary>
    /// 指定された内容を持つストリームを初期化し、指定されたバイト数を読み込んだ後にIOExceptionをスローするように設定します。
    /// </summary>
    /// <param name="content">ストリームの初期内容</param>
    /// <param name="cutoffAfterBytes">指定されたバイト数を読み込んだ後にIOExceptionをスローするバイト数</param>
    public CutoffStream(byte[] content, long cutoffAfterBytes) {
        this._baseStream = new MemoryStream(content);
        this._cutoff = cutoffAfterBytes;
        this._readBytes = 0;
    }

    /// <summary>
    /// 指定されたバイト数を読み込んだ後にIOExceptionをスローするようにオーバーライドされたReadメソッド。
    /// </summary>
    /// <param name="buffer">読み取り先のバッファ</param>
    /// <param name="offset">バッファ内の書き込み開始位置</param>
    /// <param name="count">読み取るバイト数</param>
    /// <returns>実際に読み取ったバイト数</returns>
    /// <exception cref="IOException">指定されたバイト数を読み込んだ後にスローされる例外</exception>
    public override int Read(byte[] buffer, int offset, int count) {
        if (this._readBytes >= this._cutoff) {
            throw new IOException("Simulated stream cutoff.");
        }

        var readCount = this._baseStream.Read(buffer, offset, count);
        this._readBytes += readCount;
        return readCount;
    }

    // 必須のオーバーライド（詳細は省略可）
    public override bool CanRead => this._baseStream.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => this._baseStream.Length;
    public override long Position { get => this._baseStream.Position; set => this._baseStream.Position = value; }
    public override void Flush() => this._baseStream.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

/// <summary>
/// 指定されたバイト数を読み込んだ後にIOExceptionをスローするストリーム。
/// </summary>
public class ThrowingStream : MemoryStream {
    private readonly long _throwAfterBytes;
    private long _totalBytesRead = 0;

    /// <summary>
    /// 指定されたバッファを使用してストリームを初期化し、指定されたバイト数を読み込んだ後に例外をスローするように設定します。
    /// </summary>
    /// <param name="buffer">ストリームの初期内容</param>
    /// <param name="throwAfterBytes">指定されたバイト数を読み込んだ後にIOExceptionをスローするバイト数</param>
    public ThrowingStream(byte[] buffer, long throwAfterBytes) : base(buffer) {
        this._throwAfterBytes = throwAfterBytes;
    }

    /// <summary>
    /// 指定されたバイト数を読み込んだ後にIOExceptionをスローするようにオーバーライドされたReadメソッド。
    /// </summary>
    /// <param name="buffer">読み取り先のバッファ</param>
    /// <param name="offset">バッファ内の書き込み開始位置</param>
    /// <param name="count">読み取るバイト数</param>
    /// <returns>実際に読み取ったバイト数</returns>
    /// <exception cref="IOException"></exception>
    public override int Read(byte[] buffer, int offset, int count) {
        if (this._totalBytesRead >= this._throwAfterBytes) {
            throw new IOException("読み込み中に例外が発生しました（テスト用）。");
        }

        int bytesRead = base.Read(buffer, offset, count);
        this._totalBytesRead += bytesRead;
        return bytesRead;
    }

    /// <summary>
    /// 指定されたバイト数を読み込んだ後にIOExceptionをスローするようにオーバーライドされたReadAsyncメソッド。
    /// </summary>
    /// <param name="buffer">読み取り先のバッファ</param>
    /// <param name="offset">バッファ内の書き込み開始位置</param>
    /// <param name="count">読み取るバイト数</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>実際に読み取ったバイト数</returns>
    /// <exception cref="IOException">指定されたバイト数を読み込んだ後にスローされる例外</exception>
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
        if (this._totalBytesRead >= this._throwAfterBytes) {
            throw new IOException("読み込み中に例外が発生しました（テスト用）。");
        }

        int bytesRead = await base.ReadAsync(buffer, offset, count, cancellationToken);
        this._totalBytesRead += bytesRead;
        return bytesRead;
    }
}
