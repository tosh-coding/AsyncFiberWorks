using System;
using System.Collections.Generic;

namespace Retlang.Core
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