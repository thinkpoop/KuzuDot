using System;
using System.Runtime.InteropServices;

namespace KuzuDot.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct KuzuSystemConfig
    {
        public ulong BufferPoolSize;
        public ulong MaxNumThreads;
        [MarshalAs(UnmanagedType.U1)]
        public bool EnableCompression;
        [MarshalAs(UnmanagedType.U1)]
        public bool ReadOnly;
        public ulong MaxDbSize;
        [MarshalAs(UnmanagedType.U1)]
        public bool AutoCheckpoint;
        public ulong CheckpointThreshold;
    }

    [StructLayout(LayoutKind.Sequential)] internal struct KuzuDatabase { public IntPtr Database; }
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuConnection { public IntPtr Connection; }
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuPreparedStatement { public IntPtr PreparedStatement; public IntPtr BoundValues; }
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuQueryResult { public IntPtr QueryResult; [MarshalAs(UnmanagedType.U1)] public bool IsOwnedByCpp; }
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuFlatTuple { public IntPtr FlatTuple; [MarshalAs(UnmanagedType.U1)] public bool IsOwnedByCpp; }
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuValue { public IntPtr Value; [MarshalAs(UnmanagedType.U1)] public bool IsOwnedByCpp; }

    // Now internal native logical type (wrapped by public DataType)
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuLogicalTypeNative { public IntPtr DataType; }

    // Native internal ID struct is internal; wrapped by public KuzuInternalId in managed layer
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuInternalIdNative { public ulong TableId; public ulong Offset; }

    [StructLayout(LayoutKind.Sequential)] internal struct KuzuDate { public int Days; }
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuTimestamp { public long Value; }
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuTimestampNs { public long Value; }
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuTimestampMs { public long Value; }
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuTimestampSec { public long Value; }
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuTimestampTz { public long Value; }
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuInterval { public int Months; public int Days; public long Micros; }
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuInt128 { public ulong Low; public long High; }
    [StructLayout(LayoutKind.Sequential)] internal struct KuzuQuerySummary { public IntPtr QuerySummary; }

    // Portable subset of C's struct tm (platforms commonly expose at least these 9 int fields).
    [StructLayout(LayoutKind.Sequential)]
    internal struct KuzuTm
    {
        public int tm_sec;   // Seconds [0,60]
        public int tm_min;   // Minutes [0,59]
        public int tm_hour;  // Hour [0,23]
        public int tm_mday;  // Day of month [1,31]
        public int tm_mon;   // Month of year [0,11]
        public int tm_year;  // Years since 1900
        public int tm_wday;  // Day of week [0,6] (Sunday = 0)
        public int tm_yday;  // Day of year [0,365]
        public int tm_isdst; // Daylight Savings flag
    }
}