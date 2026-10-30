using UnityEngine;

namespace NibBall
{
    /// <summary>Every sound is synthesised at first use, so the app needs no audio files.</summary>
    public static class Sfx
    {
        const int Rate = 24000;
        static AudioClip thud, ding, dong, shatter, crash, cheer, pop;

        static AudioClip Make(string name, float seconds, System.Func<float, float, float> f)
        {
            int n = (int)(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate, seconds), -1f, 1f);
            var c = AudioClip.Create(name, n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        static float Tone(float t, float hz) => Mathf.Sin(2f * Mathf.PI * hz * t);

        static void Build()
        {
            if (thud != null) return;
            var rng = new System.Random(7);
            float Noise() => (float)(rng.NextDouble() * 2 - 1);

            thud = Make("thud", 0.18f, (t, d) => Tone(t, 90f - 150f * t) * Mathf.Exp(-t * 22f) * 0.9f);
            pop = Make("pop", 0.12f, (t, d) => Tone(t, 400f + 800f * t) * Mathf.Exp(-t * 35f) * 0.6f);
            ding = Make("ding", 0.6f, (t, d) => (Tone(t, 880f) + 0.5f * Tone(t, 1320f)) * Mathf.Exp(-t * 7f) * 0.45f);
            dong = Make("dong", 0.6f, (t, d) => (Tone(t, 330f) + 0.4f * Tone(t, 247f)) * Mathf.Exp(-t * 6f) * 0.5f);

            // glass: a hiss of bright noise with a scatter of high tinkles
            var tinkles = new float[8]; var tinkleAt = new float[8];
            for (int i = 0; i < 8; i++) { tinkles[i] = 2500f + 3500f * (float)rng.NextDouble(); tinkleAt[i] = 0.05f + 0.5f * (float)rng.NextDouble(); }
            float prev = 0f;
            shatter = Make("shatter", 0.9f, (t, d) =>
            {
                float n = Noise(); float hp = n - prev; prev = n;
                float v = hp * Mathf.Exp(-t * 7f) * 0.7f;
                for (int i = 0; i < 8; i++) if (t > tinkleAt[i]) v += Tone(t, tinkles[i]) * Mathf.Exp(-(t - tinkleAt[i]) * 30f) * 0.25f;
                return v;
            });

            // wall: a low boom under a lowpassed crunch
            float lp = 0f;
            crash = Make("crash", 0.8f, (t, d) =>
            {
                lp += (Noise() - lp) * 0.25f;
                return (lp * Mathf.Exp(-t * 5f) * 1.4f) + Tone(t, 55f) * Mathf.Exp(-t * 7f) * 0.7f;
            });

            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            cheer = Make("cheer", 0.9f, (t, d) =>
            {
                int k = Mathf.Min(3, (int)(t / 0.14f));
                float tn = t - k * 0.14f;
                return Tone(t, notes[k]) * Mathf.Exp(-tn * 6f) * 0.5f + (k == 3 ? Tone(t, notes[3] * 1.5f) * 0.2f * Mathf.Exp(-tn * 4f) : 0f);
            });
        }

        static void Play(AudioClip c, Vector3 at, float volume = 1f)
        {
            if (c == null) return;
            AudioSource.PlayClipAtPoint(c, at, volume);
        }

        public static void Thud(Vector3 at, float strength) { Build(); Play(thud, at, Mathf.Clamp01(strength / 5f)); }
        public static void Pop(Vector3 at) { Build(); Play(pop, at, 0.8f); }
        public static void BitOn(Vector3 at) { Build(); Play(ding, at); }
        public static void BitOff(Vector3 at) { Build(); Play(dong, at); }
        public static void Shatter(Vector3 at) { Build(); Play(shatter, at); }
        public static void Crash(Vector3 at) { Build(); Play(crash, at); }
        public static void Cheer(Vector3 at) { Build(); Play(cheer, at); }
    }
}
