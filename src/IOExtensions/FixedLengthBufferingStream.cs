using Microsoft.IO;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IOExtensions
{
    /// <summary>
    /// This stream provides transparent in-memory buffering when reading forward-only streams,
    /// allowing you to read the data or particular segments multiple times.
    /// </summary>
    public class FixedLengthBufferingStream : Stream
    {
        private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();

        private Stream _sourceStream;
        private long _sourceStreamPosition;

        private Stream _bufferStream;
        private long _fixedLength;

        public FixedLengthBufferingStream(Stream sourceStream, int fixedLength)
        {
            _sourceStream = sourceStream;
            _bufferStream = MemoryStreamManager.GetStream(
                typeof(FixedLengthBufferingStream).FullName,
                fixedLength);
            _fixedLength = fixedLength;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _fixedLength;
        public override long Position
        {
            get => _bufferStream.Position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush() => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, default).GetAwaiter().GetResult();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin != SeekOrigin.Begin)
            {
                throw new NotImplementedException("Only SeekOrigin.Begin is supported");
            }

            if (offset > _sourceStreamPosition)
            {
                throw new NotImplementedException("Can only seek to positions that were already read");
            }

            _bufferStream.Position = offset;
            return offset;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (count == 0)
            {
                return 0;
            }

            var maxBytesToReadFromBuffer = _sourceStreamPosition - _bufferStream.Position;
            if (maxBytesToReadFromBuffer > 0)
            {
                var bytesToReadFromBuffer = (int)Math.Min(count, maxBytesToReadFromBuffer);
                return await _bufferStream.ReadAsync(buffer, offset, bytesToReadFromBuffer, cancellationToken);
            }

            var maxBytesToReadFromSource = _fixedLength - _sourceStreamPosition;
            if (maxBytesToReadFromSource > 0)
            {
                var bytesToReadFromSource = (int)Math.Min(count, maxBytesToReadFromSource);
                var bytesRead = await _sourceStream.ReadAsync(buffer, offset, bytesToReadFromSource, cancellationToken);
                _sourceStreamPosition += bytesRead;

                if (bytesRead > 0)
                {
                    await _bufferStream.WriteAsync(buffer, offset, bytesRead, cancellationToken);
                }

                return bytesRead;
            }

            return 0;
        }
    }
}
