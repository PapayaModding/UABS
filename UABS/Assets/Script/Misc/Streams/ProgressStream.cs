using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UABS.Assets.Script.Misc.Streams
{
    public class ProgressStream : MemoryStream
    {
        private readonly ProgressStreamCallback _callback;

        public ProgressStream(ProgressStreamCallback callback)
        {
            _callback = callback;
        }

        public override int Read(Span<byte> destination)
        {
            int result = base.Read(destination);
            _callback.ReadByte(result);
            return result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = base.Read(buffer, offset, count);
            _callback.ReadByte(result);
            return result;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            var resultTask = base.ReadAsync(destination, cancellationToken);
            // Log result asynchronously
            resultTask.AsTask().ContinueWith(t =>
            {
                _callback.ReadByte(t.Result);
            }, TaskScheduler.FromCurrentSynchronizationContext());
            return resultTask;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var resultTask = base.ReadAsync(buffer, offset, count, cancellationToken);
            // Log result asynchronously
            resultTask.ContinueWith(t =>
            {
                _callback.ReadByte(t.Result);
            }, TaskScheduler.FromCurrentSynchronizationContext());
            return resultTask;
        }

        public override int ReadByte()
        {
            int result = base.ReadByte();
            _callback.ReadByte(result);
            return result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            base.Write(buffer, offset, count);
            _callback.WriteByte(count);
        }

        public override void Write(ReadOnlySpan<byte> source)
        {
            base.Write(source);
            _callback.WriteByte(source.Length);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var task = base.WriteAsync(buffer, offset, count, cancellationToken);
            task.ContinueWith(_ =>
            {
                _callback.WriteByte(count);
            }, TaskScheduler.FromCurrentSynchronizationContext());
            return task;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            var task = base.WriteAsync(source, cancellationToken);
            task.AsTask().ContinueWith(_ =>
            {
                _callback.WriteByte(source.Length);
            }, TaskScheduler.FromCurrentSynchronizationContext());
            return task;
        }

        public override void WriteByte(byte value)
        {
            base.WriteByte(value);
            _callback.WriteByte(1);
        }

        public override void WriteTo(Stream stream)
        {
            base.WriteTo(stream);
            long written = Length;
            _callback.WriteByte(written);
        }
    }
}