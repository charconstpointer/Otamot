using System;

namespace Otamot.Agent
{
    public class RestartPolicy : IPolicy
    {
        public int MaxRestarts { get; set; }
        public int RestartsWithoutSuccess { get; set; }
        public RetryStrategy Strategy { get; set; }
        public TimeSpan WaitTime { get; set; } = TimeSpan.FromSeconds(1);
    }

    public enum RetryStrategy
    {
        Constant,
        Incremental
    }
}