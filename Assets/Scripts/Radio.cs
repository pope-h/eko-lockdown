using System.Collections.Generic;
using UnityEngine;

// Eko Lockdown: the radio. Real recorded music, one folder of tracks per station under
// Resources/Music, played back to back like a station would. The news station is a quiet
// synthesised bed under the bulletins. R / T change station. Louder in a vehicle, ducked
// near the street party.
namespace Eko
{
    public class Radio : MonoBehaviour
    {
        public class Station
        {
            public string Freq, Name, Genre, Folder; public float Bpm; public string[] Talk;
            public AudioClip[] Tracks; public int Track;
        }
        public static readonly List<Station> Stations = new List<Station>();
        public static int Current = 0;
        public static string Caption = ""; public static float CaptionUntil;
        AudioSource music, hiss; float hissT, nextTalk = 10f, tunedAt; bool started;
        static AudioClip newsBed;
        const int Rate = 22050;

        // Shown on the title and end screens. The licence asks for it.
        public const string Credits = "Music: Alex Morgan, via Free Music Archive (CC BY 4.0)";

        void Awake()
        {
            music = gameObject.AddComponent<AudioSource>(); music.loop = false; music.spatialBlend = 0f; music.volume = 0f;
            hiss = gameObject.AddComponent<AudioSource>(); hiss.loop = true; hiss.clip = Static(); hiss.volume = 0f; hiss.Play();
            Stations.Clear();
            Stations.Add(new Station
            {
                Freq = "99.1", Name = "EKO FM", Genre = "Afrobeats, all night", Folder = "eko", Bpm = 104f,
                Talk = new[] { "Curfew is eight o'clock, but the music never sleeps. Lock your door and turn us up.", "If you can hear this, you are not alone. Eko FM is still on air.", "This one goes out to the danfo drivers still running the mainland. God keep you.", "They say the sick ones follow noise. So do we. Stay inside." },
            });
            Stations.Add(new Station
            {
                Freq = "102.3", Name = "ISLAND FM", Genre = "Amapiano behind the walls", Folder = "island", Bpm = 112f,
                Talk = new[] { "Island FM, broadcasting from behind the Lekki walls. Generators on, gates shut.", "Lekki, the checkpoint is closing early tonight. Nobody in, nobody out.", "Log drum therapy for a city that cannot sleep." },
            });
            Stations.Add(new Station
            {
                Freq = "95.1", Name = "STREET HEAT", Genre = "Afro house from a rooftop in Yaba", Folder = "street", Bpm = 122f,
                Talk = new[] { "Street Heat, pirate radio, no licence, no fear.", "If we go, we go dancing. Yaba, the owambe is still on.", "Ajegunle, Mushin, Apapa. Shout if you are still breathing." },
            });
            Stations.Add(new Station { Freq = "90.5", Name = "LOCKDOWN NEWS", Genre = "Emergency bulletins", Bpm = 60f, Talk = new string[0] });

            foreach (var s in Stations)
            {
                if (s.Folder == null) continue;
                var clips = Resources.LoadAll<AudioClip>("Music/" + s.Folder);
                System.Array.Sort(clips, (a, b) => string.CompareOrdinal(a.name, b.name));
                s.Tracks = clips; s.Track = Random.Range(0, Mathf.Max(1, clips.Length));
            }
        }

        static bool HasMusic(Station s) => s.Tracks != null && s.Tracks.Length > 0;

        /// A clip for the street party: the first Afrobeats track, or the news bed if none shipped.
        public static AudioClip ClipFor(int i, out float bpm)
        {
            var s = Stations[i]; bpm = s.Bpm;
            return HasMusic(s) ? s.Tracks[0] : NewsBed();
        }

        static string Title(AudioClip c) => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(c.name.Replace('_', ' '));

        void TuneTo(int i)
        {
            Current = i; hissT = 0.3f; tunedAt = Time.unscaledTime; CaptionUntil = 0f;
            if (i < 0) { music.Stop(); GameUI.RadioCard("RADIO OFF", "R to turn it back on"); return; }
            var s = Stations[i];
            if (HasMusic(s))
            {
                music.clip = s.Tracks[s.Track]; music.loop = false;
                // Tune in part-way through, the way a real station is already playing.
                music.time = Random.Range(0.05f, 0.5f) * music.clip.length;
                music.Play();
                GameUI.RadioCard(s.Freq + "  " + s.Name, s.Genre + "   ·   now playing: " + Title(music.clip));
            }
            else
            {
                music.clip = NewsBed(); music.loop = true; music.time = 0f; music.Play();
                GameUI.RadioCard(s.Freq + "  " + s.Name, s.Genre);
            }
            nextTalk = s.Folder == null ? 2f : Random.Range(8f, 16f);
        }

        void NextTrack()
        {
            var s = Stations[Current];
            s.Track = (s.Track + 1) % s.Tracks.Length;
            music.clip = s.Tracks[s.Track]; music.time = 0f; music.Play(); tunedAt = Time.unscaledTime;
            Caption = s.Freq + " " + s.Name + ":  Up next, " + Title(music.clip) + ".";
            CaptionUntil = Time.unscaledTime + 6f;
        }

        void Update()
        {
            bool quiet = GameUI.Mode == Mode.Intro || GameUI.Mode == Mode.End;
            if (!quiet && !started) { started = true; TuneTo(0); }
            if (!quiet && GameUI.Mode != Mode.Dialogue)
            {
                if (Controls.RadioNext) TuneTo(Current + 1 >= Stations.Count ? -1 : Current + 1);
                else if (Controls.RadioPrev) TuneTo(Current - 1 < -1 ? Stations.Count - 1 : Current - 1);
            }
            // A finished song rolls into the next one on the same station.
            if (started && Current >= 0 && HasMusic(Stations[Current]) && !music.isPlaying && Time.unscaledTime - tunedAt > 2f) NextTrack();

            float vol = Player.I != null && Player.I.Driving != null ? 0.85f : 0.45f;
            if (Player.I != null) vol *= 0.15f + 0.85f * Mathf.Clamp01((Player.Flat(Player.I.Pos - new Vector3(262f, 0f, 225f)) - 20f) / 50f);
            if (quiet || Current < 0) vol = 0f;
            music.volume = Mathf.MoveTowards(music.volume, vol, Time.unscaledDeltaTime * 1.5f);
            hissT -= Time.unscaledDeltaTime; hiss.volume = hissT > 0 ? 0.12f * hissT / 0.3f : 0f;

            // The DJs and the news readers, as captions.
            if (Current >= 0 && !quiet)
            {
                nextTalk -= Time.unscaledDeltaTime;
                if (nextTalk <= 0)
                {
                    var s = Stations[Current];
                    Caption = s.Freq + " " + s.Name + ":  " + (s.Folder == null ? Bulletin() : s.Talk[Random.Range(0, s.Talk.Length)]);
                    CaptionUntil = Time.unscaledTime + 8f; nextTalk = s.Folder == null ? Random.Range(14f, 20f) : Random.Range(28f, 45f);
                }
            }
        }

        static readonly string[][] Bulletins =
        {
            new[] { "The government insists Eko Fever is under control. The Taskforce reports three hundred arrests in Mushin this week.", "Anyone showing dark veins must report to the quarantine camp in Ikorodu. Street Suppressant prices have doubled since Monday." },
            new[] { "Yaba residents say the parties have not stopped, Taskforce or no Taskforce. Police report diesel thefts from depots in Apapa.", "Families are gathering at the Ikorodu camp gates. Visitors are not permitted. Nobody has been released." },
            new[] { "Security at Apapa port has been doubled after reports of forged work permits. The Lekki Link checkpoint now checks every hand.", "Commissioner for Health Chief Adeyemi says there is no cure, and asks Lagosians to trust the Taskforce." },
            new[] { "The army has sealed Third Mainland Bridge. The Lekki Link remains open to permit holders only." },
            new[] { "Unconfirmed reports of a private supply of a drug called ZX on the Island. The Commissioner's office calls them dangerous lies.", "This station will keep broadcasting for as long as we can. If you are listening from the camp, we have not forgotten you." },
        };

        static string Bulletin()
        {
            var b = Bulletins[Mathf.Clamp(Story.Chapter - 1, 0, Bulletins.Length - 1)];
            return b[Random.Range(0, b.Length)];
        }

        // ── The news bed: a soft synthesised pad with a three-note chime ─────
        static float Mid(int m) => 440f * Mathf.Pow(2f, (m - 69) / 12f);

        static void Add(float[] d, int at, int len, System.Func<float, float> f)
        {
            for (int i = 0; i < len; i++) { int k = at + i; if (k < 0) continue; if (k >= d.Length) break; d[k] += f(i / (float)Rate); }
        }

        static AudioClip NewsBed()
        {
            if (newsBed != null) return newsBed;
            float len = 16f; var d = new float[(int)(len * Rate)];
            int[][] chords = { new[] { 45, 52, 57, 60 }, new[] { 41, 48, 53, 57 } };
            for (int c = 0; c < 2; c++)
                foreach (var m in chords[c])
                {
                    float f = Mid(m), dur = len / 2f;
                    Add(d, (int)(c * dur * Rate), (int)(dur * Rate), t => (Mathf.Sin(2 * Mathf.PI * f * t) + 0.5f * Mathf.Sin(2 * Mathf.PI * f * 1.003f * t)) * Mathf.Min(1f, t / 1.5f) * Mathf.Min(1f, (dur - t) / 1.5f) * 0.05f);
                }
            int[] chime = { 72, 76, 79 };
            for (int i = 0; i < 3; i++)
            {
                float f = Mid(chime[i]);
                Add(d, (int)(i * 0.35f * Rate), Rate, t => (Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * 6f) + 0.4f * Mathf.Sin(2 * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t * 14f)) * 0.22f);
            }
            float peak = 0.001f; foreach (var v in d) peak = Mathf.Max(peak, Mathf.Abs(v));
            for (int i = 0; i < d.Length; i++) d[i] = d[i] / peak * 0.6f;
            newsBed = AudioClip.Create("news", d.Length, 1, Rate, false); newsBed.SetData(d, 0);
            return newsBed;
        }

        static AudioClip Static()
        {
            var r = new System.Random(5); var d = new float[Rate / 2];
            for (int i = 0; i < d.Length; i++) d[i] = ((float)r.NextDouble() * 2f - 1f) * 0.5f;
            var c = AudioClip.Create("static", d.Length, 1, Rate, false); c.SetData(d, 0); return c;
        }
    }
}
