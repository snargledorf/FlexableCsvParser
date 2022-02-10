using System;
using System.Diagnostics;
using System.IO;

namespace TestHarness
{
    internal class DataRateStream : Stream
    {
        private long bytesRead = 0;
        private Stopwatch stopwatch = new Stopwatch();
        private TimeSpan updateFrequency = TimeSpan.FromSeconds(1);

        private Stream stream;
        private TimeSpan lastUpdate;

        public DataRateStream(Stream stream)
        {
            this.stream = stream;
        }

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

            if (stopwatch.Elapsed - lastUpdate >= updateFrequency)
            {
                double bytesPerSecond = bytesRead / stopwatch.Elapsed.TotalSeconds;
                double kilobytesPerSecond = bytesPerSecond / 1000;
                double megabytesPerSecond = kilobytesPerSecond / 1000;

                Console.Title =$"{megabytesPerSecond} Mb/s";
                lastUpdate = stopwatch.Elapsed;
            }

            int read = stream.Read(buffer, offset, count);
            bytesRead += read;
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
    }
}