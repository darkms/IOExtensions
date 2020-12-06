using FluentAssertions;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace IOExtensions.Tests
{
    public class FixedLengthBufferingStreamTests
    {
        [Fact]
        public async Task ShouldReadBytesFromSource()
        {
            // arrange
            var source = Enumerable.Range(0, 255)
                .Select(i => (byte)i)
                .ToArray();
            var stream = new FixedLengthBufferingStream(
                new ForwardOnlyStream(new MemoryStream(source)),
                source.Length);

            // act
            var result = new byte[source.Length];
            await stream.ReadAsync(result, 0, result.Length, default);

            // assert
            result.Should().BeEquivalentTo(source);
        }

        [Fact]
        public async Task ShouldAllowContentToBeReadMultipleTimes()
        {
            // arrange
            var source = Enumerable.Range(0, 255)
                .Select(i => (byte)i)
                .ToArray();
            var stream = new FixedLengthBufferingStream(
                new ForwardOnlyStream(new MemoryStream(source)),
                source.Length);
            await stream.ReadAsync(new byte[source.Length], 0, source.Length, default);

            // act
            stream.Position = 0;
            var result = new byte[source.Length];
            await stream.ReadAsync(result, 0, result.Length, default);

            // assert
            result.Should().BeEquivalentTo(source);
        }

        [Fact]
        public async Task ShouldAllowSeekingToOffsetIfPositionIsAlreadyReadFromSource()
        {
            // arrange
            var source = Enumerable.Range(0, 255)
                .Select(i => (byte)i)
                .ToArray();
            var stream = new FixedLengthBufferingStream(
                new ForwardOnlyStream(new MemoryStream(source)),
                source.Length);
            await stream.ReadAsync(new byte[source.Length], 0, source.Length, default);

            // act
            stream.Position = 200;
            var result = new byte[source.Length - 200];
            await stream.ReadAsync(result, 0, result.Length, default);

            // assert
            result.Should().BeEquivalentTo(source.Skip(200));
        }

        private class ForwardOnlyStream : Stream
        {
            private readonly Stream _underlyingStream;

            public ForwardOnlyStream(Stream underlyingStream)
            {
                _underlyingStream = underlyingStream;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _underlyingStream.Length;
            public override long Position
            {
                get => throw new System.NotImplementedException();
                set => throw new System.NotImplementedException();
            }
            public override void Flush() => throw new System.NotImplementedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new System.NotImplementedException();
            public override void SetLength(long value) => throw new System.NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new System.NotImplementedException();

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _underlyingStream.Read(buffer, offset, count);
            }
        }
    }
}