using System;
using System.Collections.Generic;

namespace UABS.Assets.Script.Misc.Streams
{
    public class ProgressStreamCallback
    {
        private long _totalBytesRead = 0;
        private long _totalBytesWritten = 0;

        public long TotalBytesRead => _totalBytesRead;
        public long TotalBytesWritten => _totalBytesWritten;

        public List<Action<long>> OnBytesRead { get; } = new();
        public List<Action<long>> OnBytesWritten { get; } = new();

        public void ReadByte(long bytes)
        {
            _totalBytesRead += bytes;
            foreach (Action<long> action in OnBytesRead)
            {
                action.Invoke(_totalBytesRead);
            }
        }

        public void WriteByte(long bytes)
        {
            _totalBytesWritten += bytes;
            foreach (Action<long> action in OnBytesWritten)
            {
                action.Invoke(_totalBytesWritten);
            }
        }
    }
}