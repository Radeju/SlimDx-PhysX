using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PhysX
{

    public static class QueryPerformance
    {
        [DllImport("KERNEL32")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        public static long Counter()
        {
            long p;
            QueryPerformanceCounter(out p);
            return p;
        }

        public static long Frequency()
        {
            long f;
            QueryPerformanceFrequency(out f);
            return f;
        }
    }
}