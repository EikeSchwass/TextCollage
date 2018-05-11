using System;

namespace TextCollage
{
    internal static class ThreadStaticRandom
    {
        #region Fields and Constants

        private static readonly Random global = new Random();
        [ThreadStatic] private static Random local;

        #endregion

        #region  Methods

        public static double NextDouble()
        {
            Random inst = local;
            if (inst == null)
            {
                int seed;
                lock (global)
                    seed = global.Next();
                local = inst = new Random(seed);
            }
            return inst.NextDouble();
        }

        public static int Next(int maxValue)
        {
            Random inst = local;
            if (inst == null)
            {
                int seed;
                lock (global)
                    seed = global.Next();
                local = inst = new Random(seed);
            }
            return inst.Next(maxValue);
        }

        public static int Next(int minValue, int maxValue)
        {
            Random inst = local;
            if (inst == null)
            {
                int seed;
                lock (global)
                    seed = global.Next();
                local = inst = new Random(seed);
            }
            return inst.Next(minValue, maxValue);
        }

        #endregion
    }
}