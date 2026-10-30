using UnityEngine;

namespace NibBall
{
    /// <summary>Four bits, left to right 8 4 2 1, the way the bins stand. Pure state: no scene, no physics.</summary>
    public sealed class Nibble
    {
        public static readonly int[] Weights = { 8, 4, 2, 1 };
        readonly bool[] on = new bool[4];

        public bool IsOn(int bin) => on[bin];

        public int Value
        {
            get { int v = 0; for (int i = 0; i < 4; i++) if (on[i]) v += Weights[i]; return v; }
        }

        public string Binary
        {
            get { var c = new char[4]; for (int i = 0; i < 4; i++) c[i] = on[i] ? '1' : '0'; return new string(c); }
        }

        /// <summary>Flips one bin's bit and says whether it is now on.</summary>
        public bool Toggle(int bin) { on[bin] = !on[bin]; return on[bin]; }

        public void Clear() { for (int i = 0; i < 4; i++) on[i] = false; }

        public void Set(int value) { for (int i = 0; i < 4; i++) on[i] = (value & Weights[i]) != 0; }

        /// <summary>A target from 1 to 15 that is not what is already showing.</summary>
        public int NextTarget(System.Random rng)
        {
            int t;
            do t = rng.Next(1, 16); while (t == Value);
            return t;
        }
    }
}
