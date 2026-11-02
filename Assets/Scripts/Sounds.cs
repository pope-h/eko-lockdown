using UnityEngine;

// Eko Lockdown: the sound of the streets, synthesised in code. Engine loops for cars, buses and
// bikes, danfo horns, and a city rumble under everything. Also keeps the game silent whenever
// its page or window is not in front of the player.
namespace Eko
{
    public static class Sounds
    {
        const int Rate = 22050;
        static AudioClip car, big, bike, horn, hornDouble, city;

        public static AudioClip Engine(VType t)
        {
            switch (t)
            {
                case VType.Okada: case VType.Keke: return bike ??= EngineLoop("bike", 46, 0.55f, true, 9);
                case VType.Danfo: case VType.Truck: case VType.Jeep: return big ??= EngineLoop("big", 28, 0.45f, false, 7);
                default: return car ??= EngineLoop("car", 36, 0.3f, false, 5);
            }
        }

        /// One second that loops cleanly: whole cycles of the firing note, harmonics, and a little grit.
        static AudioClip EngineLoop(string name, int f0, float grit, bool twoStroke, int seed)
        {
            int n = Rate; var d = new float[n]; var r = new System.Random(seed);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, ph = 2f * Mathf.PI * f0 * t, v = 0f;
                if (twoStroke) { for (int k = 1; k <= 9; k++) v += Mathf.Sin(k * ph + k * 0.7f) / k; v *= 0.8f; }
                else { v = Mathf.Sin(ph) + 0.6f * Mathf.Sin(2 * ph + 0.3f) + 0.35f * Mathf.Sin(3 * ph + 1.1f) + 0.2f * Mathf.Sin(4 * ph + 0.5f); v *= 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(ph)); }
                lp += 0.15f * (((float)r.NextDouble() * 2f - 1f) - lp);
                d[i] = (v * 0.5f + lp * grit) * 0.6f;
            }
            Seam(d, Rate / 40);
            var c = AudioClip.Create(name, n, 1, Rate, false); c.SetData(d, 0); return c;
        }

        /// The classic danfo "pim", or "pim-pim".
        public static AudioClip Horn(bool twice) => twice ? (hornDouble ??= HornClip("horn2", 2)) : (horn ??= HornClip("horn", 1));

        static AudioClip HornClip(string name, int beeps)
        {
            float beep = 0.22f, gap = 0.09f; int n = (int)((beep + gap) * beeps * Rate); var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, local = t % (beep + gap);
                if (local > beep) continue;
                float env = Mathf.Min(1f, local * 80f) * Mathf.Min(1f, (beep - local) * 40f);
                float a = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 415f * t)), b = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 520f * t));
                d[i] = (a + b) * 0.18f * env;
            }
            var c = AudioClip.Create(name, n, 1, Rate, false); c.SetData(d, 0); return c;
        }

        /// Eight seconds of city: a low traffic rumble, a generator hum, passing cars and distant horns.
        public static AudioClip City
        {
            get
            {
                if (city != null) return city;
                int n = Rate * 8; var d = new float[n]; var r = new System.Random(12);
                float b = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate;
                    b = Mathf.Clamp(b + ((float)r.NextDouble() * 2f - 1f) * 0.02f, -1f, 1f) * 0.998f; // brown noise
                    d[i] = b * 0.6f + Mathf.Sin(2 * Mathf.PI * 100f * t) * 0.03f;
                }
                // Cars passing by: noise that swells and fades.
                for (int k = 0; k < 5; k++)
                {
                    int at = r.Next(n), len = Rate * 2; float lp = 0f;
                    for (int i = 0; i < len; i++) { lp += 0.06f * (((float)r.NextDouble() * 2f - 1f) - lp); d[(at + i) % n] += lp * Mathf.Sin(Mathf.PI * i / len) * 0.9f; }
                }
                // Distant horns.
                var hn = Horn(true); var hd = new float[hn.samples]; hn.GetData(hd, 0);
                for (int k = 0; k < 3; k++) { int at = r.Next(n); float pitch = 0.8f + (float)r.NextDouble() * 0.4f; for (int i = 0; i < hd.Length / pitch; i++) d[(at + i) % n] += hd[(int)(i * pitch)] * 0.18f; }
                Seam(d, Rate / 4);
                city = AudioClip.Create("city", n, 1, Rate, false); city.SetData(d, 0); return city;
            }
        }

        /// Blend the end of a loop into its start so it repeats without a click.
        static void Seam(float[] d, int fade)
        {
            int n = d.Length;
            for (int i = 0; i < fade; i++) { float w = i / (float)fade; d[i] = d[i] * w + d[n - fade + i] * (1f - w); }
            for (int i = n - fade; i < n; i++) d[i] = d[i] * 0.999f;
        }
    }

    /// The city around you, quieter indoors and on the Island. Also mutes everything when the
    /// game is not in front of the player.
    public class Ambience : MonoBehaviour
    {
        AudioSource src;

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void EkoWatchVisibility();
#endif

        void Start()
        {
            src = gameObject.AddComponent<AudioSource>(); src.clip = Sounds.City; src.loop = true; src.spatialBlend = 0f; src.volume = 0f; src.Play();
#if UNITY_WEBGL && !UNITY_EDITOR
            EkoWatchVisibility();
#endif
        }

        void OnApplicationFocus(bool focus) { AudioListener.pause = !focus; }
        void OnApplicationPause(bool paused) { AudioListener.pause = paused; }

        void Update()
        {
            float want = 0f;
            if (GameUI.Mode != Mode.Intro && GameUI.Mode != Mode.End && Player.I != null)
            {
                var p = Player.I.Pos;
                want = Interior.Inside(p) ? 0.05f : p.x > 590f ? 0.12f : 0.28f;
            }
            src.volume = Mathf.MoveTowards(src.volume, want, Time.unscaledDeltaTime * 0.5f);
        }
    }
}
