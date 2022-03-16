using System;
using System.ComponentModel;



namespace CopyCore
{
    public interface ICopierProgress
    {
        int Progress { get; }
        long Speed { get; }

        TimeSpan RemainingTime { get; }
        long TotalLength { get; }

        int FilesCount { get; }

        long BytesCopied { get; }

        int CopiedFiles { get; }
    }
}
