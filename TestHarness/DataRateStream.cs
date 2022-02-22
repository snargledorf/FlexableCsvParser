using System;
using System.Diagnostics;
using System.IO;

namespace TestHarness
{
    internal class DataRateStream : Stream
    {
        private long currentSampleBytesRead = 0;

        private readonly Stopwatch stopwatch = new();

        private readonly Stream stream;
        private readonly TimeSpan sampleTimeFrame;

        public DataRateStream(Stream stream, TimeSpan sampleTimeFrame)
        {
            this.stream = stream;
            this.sampleTimeFrame = sampleTimeFrame;
        }

        public event EventHandler<double> DataRateUpdate;

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => stream.CanWrite;

        public override long Length => stream.Length;

        public override long Position { get => stream.Position; set => stream.Position = value; }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!stopwatch.IsRunning)
                stopwatch.Start();

            int read = stream.Read(buffer, offset, count);

            currentSampleBytesRead += read;

            if (stopwatch.Elapsed >= sampleTimeFrame)
            {
                stopwatch.Stop();

                double bytesPerSecond = currentSampleBytesRead / stopwatch.Elapsed.TotalSeconds;

                OnDataRateUpdated(bytesPerSecond);

                currentSampleBytesRead = 0;
                stopwatch.Restart();
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        protected virtual void OnDataRateUpdated(double e)
        {
            DataRateUpdate?.Invoke(this, e);
        }
    }
}