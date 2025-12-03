using System;
using System.Collections.Generic;
using System.Text;

namespace TesteLLMs.Models
{
    public class BenchmarkMetrics
    {
        public string Operation { get; set; } = string.Empty;
        public long ElapsedMilliseconds { get; set; }
        public long ElapsedTicks { get; set; }
        public int ResultCount { get; set; }
        public string? Tipo { get; set; }

        public long MemoryBeforeBytes { get; set; }
        public long MemoryAfterBytes { get; set; }
        public long MemoryUsedBytes => MemoryAfterBytes - MemoryBeforeBytes;
    }

}
