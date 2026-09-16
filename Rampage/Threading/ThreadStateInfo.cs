using System;

namespace Rampage.Threading
{

    public class ThreadStateInfo
    {

        public ManualResetEvent? ResetEvent
        {
            get; set;
        }

        public object? Data
        {
            get; set;
        }

        public int ThreadIndex
        {
            get; set;
        }

        public string? BatchName
        {
            get; set;
        }

    }
}
