using System;
using System.Collections.Generic;

namespace AsyncFiberWorks.Core
{
    internal static class Queues
    {
        public static void Swap(ref Queue<Action> a, ref Queue<Action> b)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }
    }
}