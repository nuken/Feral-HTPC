using System;
using System.IO;
using System.Threading;

namespace FeralCode
{
    public class LiveTailStream : Stream
    {
        private readonly FileStream _fileStream;
        private readonly Func<bool> _isDownloadActive;
        private readonly DateTime _startTime;

        public LiveTailStream(string filePath, Func<bool> isDownloadActive)
        {
            _isDownloadActive = isDownloadActive;
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _startTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Bypasses VLC's cached seeker by physically moving the read pointer.
        /// </summary>
        public void ForceSeek(double secondsToJump)
        {
            // Calculate the average bytes per second to map time to a byte offset
            double elapsedSeconds = (DateTime.UtcNow - _startTime).TotalSeconds;
            if (elapsedSeconds < 1) elapsedSeconds = 1; // Prevent divide by zero
            
            long averageBytesPerSec = (long)(_fileStream.Length / elapsedSeconds);
            long bytesToJump = (long)(averageBytesPerSec * secondsToJump);
            
            long newPos = _fileStream.Position + bytesToJump;
            
            // Prevent seeking past the safe margin of the downloading file
            long safeMargin = _isDownloadActive() ? 188000 : 0; 
            long maxSafePos = _fileStream.Length - safeMargin;
            
            if (newPos > maxSafePos) newPos = maxSafePos;
            if (newPos < 0) newPos = 0;
            
            // CRITICAL MAGIC: Align to a 188-byte MPEG-TS packet boundary.
            // This guarantees VLC lands on a clean packet header and maintains A/V sync!
            newPos -= (newPos % 188);

            _fileStream.Position = newPos;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;
            int retries = 0;
            int maxRetries = 100; 

            while (totalBytesRead == 0 && retries < maxRetries)
            {
                long availableBytes = _fileStream.Length - _fileStream.Position;
                long safeMargin = _isDownloadActive() ? 188000 : 0; 

                if (availableBytes > safeMargin)
                {
                    int bytesToRead = (int)Math.Min(count, availableBytes - safeMargin);
                    if (bytesToRead > 0)
                    {
                        totalBytesRead = _fileStream.Read(buffer, offset, bytesToRead);
                    }
                }

                if (totalBytesRead == 0)
                {
                    if (!_isDownloadActive() && availableBytes == 0)
                    {
                        break; 
                    }

                    Thread.Sleep(50);
                    retries++;
                }
            }

            return totalBytesRead;
        }

        // --- FIX: Tell VLC this is a live pipe, not a static file ---
        public override bool CanRead => true;
        public override bool CanSeek => false; // <-- CRITICAL FIX
        public override bool CanWrite => false;
        public override long Length => _fileStream.Length;
        
        public override long Position
        {
            get => _fileStream.Position;
            set => _fileStream.Position = value;
        }

        // VLC will no longer call this natively, preventing it from interfering with ForceSeek
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fileStream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}