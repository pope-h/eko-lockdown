using System.Collections.Generic;
using UnityEngine;
using T = Eko.Controls.Touch;

// Eko Lockdown: on-screen controls for phones. Left thumb: a floating joystick (move, or throttle
// and steering in a vehicle). Right thumb: drag to look, pinch to zoom. Buttons on the right change
// with what you are doing: on foot, in a vehicle, or standing next to someone. Tap the minimap
// for the full map. Everything here only writes into Controls.Touch.
namespace Eko
{
    public class TouchControls : MonoBehaviour
    {
        /// Show the touch HUD: on phones and tablets, after any touch, or with -ekotouch for testing.
        public static bool On;

        class Btn { public string Id, Label; public Vector2 C; public float R; public bool Hold, Down; }
        readonly List<Btn> btns = new List<Btn>();
        readonly Dictionary<int, Btn> held = new Dictionary<int, Btn>();
        int stickId = -1, lookId = -1; Vector2 stickBase, stickNow, lookLast;
        float pinchLast; bool sneakToggle;
        const float StickR = 70f;

        float S => Screen.height / 720f;
        float W => Screen.width / S;
        const float H = 720f;
        Vector2 Gui(Vector2 screen) => new Vector2(screen.x / S, (Screen.height - screen.y) / S);

        void Awake()
        {
            On = Application.isMobilePlatform || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ekotouch") >= 0;
            Input.multiTouchEnabled = true;
        }

        bool Playing => GameUI.Mode == Mode.None;

        /// The buttons that make sense right now, laid out in an arc for the right thumb.
        void Layout()
        {
            btns.Clear();
            var p = Player.I; if (p == null) return;
            if (GameUI.Mode == Mode.Map) { btns.Add(new Btn { Id = "map", Label = "CLOSE", C = new Vector2(W - 70f, 60f), R = 40f }); return; }
            if (!Playing) return;
            bool driving = p.Driving != null;
            float x = W - 100f, y = H - 110f;
            if (driving)
            {
                btns.Add(new Btn { Id = "brake", Label = "BRAKE", C = new Vector2(x, y), R = 58f, Hold = true });
                btns.Add(new Btn { Id = "car", Label = "GET OUT", C = new Vector2(x - 135f, y + 30f), R = 42f });
                btns.Add(new Btn { Id = "horn", Label = "HORN", C = new Vector2(x - 25f, y - 135f), R = 40f });
            }
            else
            {
                if (p.Near != null) btns.Add(new Btn { Id = "act", Label = Short(p.Near), C = new Vector2(x, y), R = 58f });
                else btns.Add(new Btn { Id = "run", Label = "RUN", C = new Vector2(x, y), R = 58f, Hold = true });
                btns.Add(new Btn { Id = "jump", Label = "JUMP", C = new Vector2(x - 135f, y + 30f), R = 40f });
                btns.Add(new Btn { Id = "sneak", Label = sneakToggle ? "STAND" : "SNEAK", C = new Vector2(x - 25f, y - 135f), R = 40f });
                if (p.NearCar != null) btns.Add(new Btn { Id = "car", Label = p.NearCar.State == Car.Mode.Traffic ? "TAKE CAR" : "DRIVE", C = new Vector2(x - 150f, y - 100f), R = 44f });
            }
            if (Game.Bitten) btns.Add(new Btn { Id = "dose", Label = "DOSE " + Game.Doses, C = new Vector2(W - 60f, y - 230f), R = 36f });
            btns.Add(new Btn { Id = "radio", Label = "RADIO", C = new Vector2(W - 60f, 275f), R = 30f });
        }

        static string Short(Interactable it) => it.Kind == Kind.Door ? "ENTER" : it.Kind == Kind.Gate ? "TALK" : it.Prompt.StartsWith("Talk") ? "TALK" : "USE";

        void Update()
        {
            if (Input.touchCount > 0) On = true;
            T.Move = Vector2.zero; T.Run = false; T.Brake = false;
            if (!On) return;
            Layout();
            if (!Playing && GameUI.Mode != Mode.Map) { stickId = lookId = -1; held.Clear(); sneakToggle = false; T.Sneak = false; return; }

            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i); var g = Gui(t.position);
                if (t.phase == TouchPhase.Began)
                {
                    var b = Hit(g);
                    if (b != null) { held[t.fingerId] = b; Press(b); continue; }
                    if (Playing && MinimapRect.Contains(g)) { T.MapBtn = true; continue; }
                    if (Playing && g.x < W * 0.42f && g.y > H * 0.3f && stickId < 0) { stickId = t.fingerId; stickBase = stickNow = g; continue; }
                    if (lookId < 0) { lookId = t.fingerId; lookLast = g; }
                    else pinchLast = -1f;
                }
                else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                {
                    if (t.fingerId == stickId) stickNow = g;
                    else if (t.fingerId == lookId && Input.touchCount < 3)
                    {
                        Vector2 d = g - lookLast; lookLast = g;
                        if (!Pinching()) { T.LookX += d.x * 0.35f; T.LookY += d.y * 0.22f; }
                    }
                }
                else
                {
                    if (t.fingerId == stickId) stickId = -1;
                    if (t.fingerId == lookId) lookId = -1;
                    held.Remove(t.fingerId);
                }
            }
            Pinch();

            if (stickId >= 0)
            {
                Vector2 off = Vector2.ClampMagnitude(stickNow - stickBase, StickR);
                T.Move = new Vector2(off.x, -off.y) / StickR;
                // Pushing the stick right to the edge breaks into a run.
                if (off.magnitude > StickR * 0.95f && Player.I != null && Player.I.Driving == null && !sneakToggle) T.Run = true;
            }
            foreach (var b in held.Values) { if (b.Id == "run") T.Run = true; if (b.Id == "brake") T.Brake = true; }
            T.Sneak = sneakToggle;
        }

        /// Two free fingers on the right: pinch to zoom the camera (or the map).
        bool Pinching()
        {
            int free = 0;
            for (int i = 0; i < Input.touchCount; i++) { var id = Input.GetTouch(i).fingerId; if (id != stickId && !held.ContainsKey(id)) free++; }
            return free >= 2;
        }

        void Pinch()
        {
            if (!Pinching()) { pinchLast = -1f; return; }
            var pts = new List<Vector2>();
            for (int i = 0; i < Input.touchCount && pts.Count < 2; i++) { var t = Input.GetTouch(i); if (t.fingerId != stickId && !held.ContainsKey(t.fingerId)) pts.Add(Gui(t.position)); }
            float d = Vector2.Distance(pts[0], pts[1]);
            if (pinchLast > 0f) T.Zoom += (d - pinchLast) * 0.012f;
            pinchLast = d;
        }

        Btn Hit(Vector2 g) { foreach (var b in btns) if ((g - b.C).sqrMagnitude < b.R * b.R * 1.2f) return b; return null; }

        void Press(Btn b)
        {
            switch (b.Id)
            {
                case "act": T.Act = true; break;
                case "car": T.CarBtn = true; break;
                case "dose": T.DoseBtn = true; break;
                case "jump": T.JumpBtn = true; break;
                case "horn": T.HornBtn = true; break;
                case "radio": T.RadioBtn = true; break;
                case "map": T.MapBtn = true; break;
                case "sneak": sneakToggle = !sneakToggle; break;
            }
        }

        /// Where GameUI draws the minimap on touch screens (tap it for the full map).
        public static Rect MinimapRect => new Rect(Screen.width / (Screen.height / 720f) - 174f, 84f, 160f, 160f);

        // ── Drawing ──────────────────────────────────────────────────────────
        static GUIStyle lbl;
        static Texture2D disc;

        /// A filled circle with a bright rim, smooth at the edge.
        static Texture2D Disc()
        {
            if (disc != null) return disc;
            const int s = 128; disc = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = new Vector2(x - s / 2f + 0.5f, y - s / 2f + 0.5f).magnitude / (s / 2f);
                float edge = Mathf.Clamp01((1f - d) * s / 3f);
                float rim = d > 0.86f ? 1f : 0.42f;
                px[y * s + x] = new Color(1, 1, 1, rim * edge);
            }
            disc.SetPixels(px); disc.Apply(); return disc;
        }

        void OnGUI()
        {
            if (!On) return;
            GUI.depth = -10;
            GUI.matrix = Matrix4x4.Scale(new Vector3(S, S, 1));
            if (lbl == null) lbl = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 14, wordWrap = true, normal = { textColor = Color.white } };

            if (Screen.height > Screen.width)
            {
                GUI.color = new Color(0, 0, 0, 0.92f); GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture); GUI.color = Color.white;
                GUI.Label(new Rect(20, H / 2 - 60, W - 40, 120), "Turn your phone sideways to play", new GUIStyle(lbl) { fontSize = 34 });
                return;
            }
            if (!Playing && GameUI.Mode != Mode.Map) return;

            var ring = Disc();
            if (Playing)
            {
                // The stick: a faint resting hint, or the base and knob under your thumb.
                Vector2 b = stickId >= 0 ? stickBase : new Vector2(150f, H - 150f);
                GUI.color = new Color(1, 1, 1, stickId >= 0 ? 0.35f : 0.15f);
                GUI.DrawTexture(new Rect(b.x - StickR - 20, b.y - StickR - 20, (StickR + 20) * 2, (StickR + 20) * 2), ring);
                Vector2 k = stickId >= 0 ? stickBase + Vector2.ClampMagnitude(stickNow - stickBase, StickR) : b;
                GUI.color = new Color(1, 1, 1, stickId >= 0 ? 0.8f : 0.3f);
                GUI.DrawTexture(new Rect(k.x - 34, k.y - 34, 68, 68), ring);
                GUI.color = Color.white;
                if (stickId < 0) GUI.Label(new Rect(b.x - 80, b.y + StickR + 14, 160, 24), "MOVE", new GUIStyle(lbl) { normal = { textColor = new Color(1, 1, 1, 0.5f) } });
            }
            foreach (var bt in btns)
            {
                bool down = false; foreach (var h in held.Values) if (h.Id == bt.Id) down = true;
                bool lit = (bt.Id == "sneak" && sneakToggle) || down;
                Color c = bt.Id == "act" ? new Color(1f, 0.82f, 0.2f) : bt.Id == "dose" ? new Color(0.4f, 1f, 0.4f) : bt.Id == "brake" ? new Color(1f, 0.4f, 0.35f) : Color.white;
                GUI.color = new Color(0, 0, 0, 0.55f); GUI.DrawTexture(new Rect(bt.C.x - bt.R, bt.C.y - bt.R, bt.R * 2, bt.R * 2), ring);
                GUI.color = new Color(c.r, c.g, c.b, lit ? 0.95f : 0.6f);
                GUI.DrawTexture(new Rect(bt.C.x - bt.R, bt.C.y - bt.R, bt.R * 2, bt.R * 2), ring);
                GUI.color = Color.white;
                var ls = new GUIStyle(lbl) { fontSize = bt.R > 50 ? 17 : 13, normal = { textColor = new Color(0, 0, 0, 0.7f) } };
                GUI.Label(new Rect(bt.C.x - bt.R + 1, bt.C.y - 15, bt.R * 2, 32), bt.Label, ls);
                ls.normal.textColor = Color.white;
                GUI.Label(new Rect(bt.C.x - bt.R, bt.C.y - 16, bt.R * 2, 32), bt.Label, ls);
            }
        }
    }
}
