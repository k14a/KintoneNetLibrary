using System;
using System.IO;

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
        _length = length;
        _position = 0;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position {
        get => _position;
        set => _position = value;
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) {
        var remaining = _length - _position;
        var toRead = Math.Min(remaining, count);
        Array.Clear(buffer, offset, (int)toRead);
        _position += toRead;
        return (int)toRead;
    }

    public override long Seek(long offset, SeekOrigin origin) {
        return _position = origin switch {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
    }

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
/// <summary>
/// Read操作を遅延させるストリーム。
/// 遅延時間はコンストラクタで指定可能。
/// </summary>
public class SlowStream : MemoryStream {
    private readonly int _delayMilliseconds;

    public SlowStream(byte[] buffer, int delayMilliseconds = 100) : base(buffer) {
        _delayMilliseconds = delayMilliseconds;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
        await Task.Delay(_delayMilliseconds, cancellationToken);
        return await base.ReadAsync(buffer, offset, count, cancellationToken);
    }
}

/// <summary>
/// 一定バイト読み込み後に例外をスローするストリーム。
/// </summary>
public class FaultyStream : MemoryStream {
    private readonly long _failAfterBytes;
    private long _totalRead;

    public FaultyStream(byte[] buffer, long failAfterBytes) : base(buffer) {
        _failAfterBytes = failAfterBytes;
        _totalRead = 0;
    }

    public override int Read(byte[] buffer, int offset, int count) {
        if (_totalRead >= _failAfterBytes) {
            throw new IOException("FaultyStream: 読み込み中に意図的な例外をスローしました。");
        }

        int toRead = count;

        if (_totalRead + toRead > _failAfterBytes) {
            toRead = (int)(_failAfterBytes - _totalRead);
        }

        int bytesRead = base.Read(buffer, offset, toRead);
        _totalRead += bytesRead;

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
