﻿using System;
using System.Collections.Generic;

namespace AsyncFiberWorks.Core
{
    internal static class ListUtil
    {
        public static void Swap(ref List<Action> a, ref List<Action> b)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }
    }
}