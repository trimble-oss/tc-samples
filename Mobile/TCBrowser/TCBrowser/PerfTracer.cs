namespace Examples.Mobile
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The performance sample.
    /// </summary>
    public class PerfTracer : IDisposable
    {
        private static ConcurrentBag<PerfSample> samples = new ConcurrentBag<PerfSample>();

        private Stopwatch watch = Stopwatch.StartNew();
        private readonly string name;
        private readonly DateTimeOffset time;

        public PerfTracer(string name)
        {
            this.name = name;
            this.time = DateTimeOffset.UtcNow;
        }

        public static IEnumerable<PerfSample> Samples { get { return samples; } }

        public static void Clear()
        {
            Interlocked.Exchange(ref samples, new ConcurrentBag<PerfSample>());
        }

        public static void Record<T>(string name, Action func)
        {
            using (new PerfTracer(name))
            {
                func();
            }
        }

        public static T Record<T>(string name, Func<T> func)
        {
            using (new PerfTracer(name))
            {
                return func();
            } 
        }

        public static async Task<T> Record<T>(string name, Func<Task<T>> func)
        {
            using (new PerfTracer(name))
            {
                return await func().ConfigureAwait(false);
            }
        }

        public static async Task Record(string name, Func<Task> func)
        {
            using (new PerfTracer(name))
            {
                await func().ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            var sample = this.watch.ElapsedMilliseconds;
            samples.Add(
                new PerfSample
                {
                    Time = this.time,
                    Name = this.name,
                    ElapsedMilliseconds = sample,
                });
        }

        public class PerfSample
        {
            public DateTimeOffset Time { get; set; }

            public string Name { get; set; }

            public long ElapsedMilliseconds { get; set; }
        }
    }
}
