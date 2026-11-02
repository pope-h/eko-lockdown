using System.Collections.Generic;
using UnityEngine;

// Eko Lockdown: HUD, dialogue, menus and maps, drawn with IMGUI so the project needs no UI package.
namespace Eko
{
    public enum Mode { Intro, None, Dialogue, Choice, Map, End, Arrested }

    public class GameUI : MonoBehaviour
    {
        public static Mode Mode = Mode.Intro;
        static string toast = ""; static float toastUntil; static bool toastBad;
        static string card = ""; static float cardUntil;
        static string region = "", regionSub = ""; static float regionUntil; static string lastRegion = "";
        static string radioTitle = "", radioSub = ""; static float radioUntil;
        static Color flashCol; static float flashUntil;
        static string[] lines; static int li; static float shown; static System.Action onLinesDone;
        static string[] choiceOpts; static System.Action<int> onChoice;
        float regionTick;

        GUIStyle label, big, button, tag, tagBig, small, huge, regionStyle;
        Texture2D bar;
        float W, H, S;
        Vector2 GuiOffset;

        public static void ResetUI() { Mode = Mode.Intro; toast = ""; card = ""; lines = null; lastRegion = ""; }
        public static void Toast(string m, bool bad = false) { toast = m; toastUntil = Time.unscaledTime + 4.5f; toastBad = bad; }
        public static void Card(string t) { card = t; cardUntil = Time.unscaledTime + 3.2f; }
        public static void Flash(Color c) { flashCol = c; flashUntil = Time.unscaledTime + 0.7f; }
        public static void RadioCard(string t, string sub) { radioTitle = t; radioSub = sub; radioUntil = Time.unscaledTime + 3.5f; }
        public static void PlayLines(string[] l, System.Action done) { lines = l; li = 0; shown = 0; onLinesDone = done; Mode = Mode.Dialogue; }
        public static void OpenChoice(string[] opts, System.Action<int> cb) { choiceOpts = opts; onChoice = cb; Mode = Mode.Choice; }
        public static void EndGame() { Mode = Mode.End; }
        /// Finishes the current dialogue immediately. Used by the headless story test and the screenshot tool.
        public static void SkipDialogue() { if (lines == null) return; var cb = onLinesDone; lines = null; Mode = Mode.None; cb?.Invoke(); }

        public static void Arrest() { Mode = Mode.Arrested; }

        public static void Open(Interactable it)
        {
            if (it.Kind == Kind.Door) { it.GetComponent<Door>().Use(); return; }
            if (!Story.Interact(it.Id)) Toast(Story.Bark(it.Id));
        }

        void Update()
        {
            if (Mode == Mode.None && Controls.MapKey) Mode = Mode.Map;
            else if (Mode == Mode.Map && (Controls.MapKey || Input.GetKeyDown(KeyCode.Escape))) Mode = Mode.None;
            if (Mode == Mode.None && Controls.Dose)
            {
                if (Game.TakeDose()) Toast("You take a dose. The veins fade."); else Toast("No doses. Look for the green glow on the streets, or buy one from Mama Put or the Plug.", true);
            }
            if (Mode == Mode.Dialogue && lines != null)
            {
                shown += Time.unscaledDeltaTime * 55f;
                string full = lines[li].Substring(lines[li].IndexOf('|') + 1);
                bool adv = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
                if (adv)
                {
                    if (shown < full.Length) shown = full.Length;
                    else if (++li >= lines.Length) { var cb = onLinesDone; lines = null; Mode = Mode.None; cb?.Invoke(); }
                    else shown = 0;
                }
            }
            regionTick -= Time.unscaledDeltaTime;
            if (regionTick <= 0 && Player.I != null && Mode != Mode.Intro)
            {
                regionTick = 0.4f;
                var p = new Vector2(Player.I.Pos.x, Player.I.Pos.z);
                var r = City.RegionAt(p);
                string name = r != null ? r.Name : p.x > 450f && p.x < 600f ? "LAGOS LAGOON" : "";
                if (name != "" && name != lastRegion)
                {
                    lastRegion = name; region = name; regionSub = r != null ? r.Sub : "Between the mainland and the island";
                    regionUntil = Time.unscaledTime + 4f;
                }
            }
        }

        void Styles()
        {
            if (label != null) return;
            bar = new Texture2D(1, 1); bar.SetPixel(0, 0, Color.white); bar.Apply();
            label = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true, normal = { textColor = new Color(0.9f, 0.88f, 0.8f) } };
            small = new GUIStyle(label) { fontSize = 14, normal = { textColor = new Color(0.75f, 0.82f, 0.72f) } };
            big = new GUIStyle(label) { fontSize = 30, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.85f, 0.12f, 0.1f) } };
            huge = new GUIStyle(big) { fontSize = 50, alignment = TextAnchor.MiddleCenter };
            regionStyle = new GUIStyle(big) { fontSize = 46, alignment = TextAnchor.LowerRight, normal = { textColor = new Color(1f, 0.95f, 0.85f) } };
            button = new GUIStyle(GUI.skin.button) { fontSize = 17, wordWrap = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 12, 8, 8) };
            tag = new GUIStyle(label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            tagBig = new GUIStyle(tag) { fontSize = 24, fontStyle = FontStyle.Bold };
        }

        GUIStyle Colored(GUIStyle b, Color c) { var s = new GUIStyle(b); s.normal.textColor = c; return s; }
        void Fill(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, bar); GUI.color = o; }
        void Center(string t, float y, GUIStyle st) { GUI.Label(new Rect(W / 2 - 480, y, 960, 60), t, new GUIStyle(st) { alignment = TextAnchor.UpperCenter }); }
        void Shadowed(Rect r, string t, GUIStyle st, Color c) { GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), t, Colored(st, new Color(0, 0, 0, 0.8f * c.a))); GUI.Label(r, t, Colored(st, c)); }

        /// World point to GUI coordinates. Returns false when behind the camera.
        bool ToGui(Vector3 w, out Vector2 g)
        {
            var cam = Camera.main; g = Vector2.zero; if (cam == null) return false;
            var sp = cam.WorldToScreenPoint(w);
            g = new Vector2(sp.x / S, (Screen.height - sp.y) / S);
            return sp.z > 0;
        }

        void OnGUI()
        {
            Styles();
            S = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(S, S, 1));
            W = Screen.width / S; H = 720f;

            if (Mode != Mode.Intro && Mode != Mode.End && Mode != Mode.Arrested)
            {
                float a = Game.Inf > Game.ShowingAt ? Mathf.Clamp01((Game.Inf - Game.ShowingAt) / 110f) : 0f;
                if (a > 0) Fill(new Rect(0, 0, W, H), new Color(0.05f, 0.25f, 0.02f, a));
                if (Mode != Mode.Map) { DrawTags(); if (Mode == Mode.None) DrawWaypoint(); }
                if (Mode != Mode.Dialogue && Mode != Mode.Map) DrawHud();
                if (Mode == Mode.None && Player.I != null) Prompts();
            }
            if (Time.unscaledTime < regionUntil && Mode != Mode.Intro)
            {
                float fade = Mathf.Clamp01((regionUntil - Time.unscaledTime) / 0.8f);
                Shadowed(new Rect(W - 760, H - 330, 740, 60), region, regionStyle, new Color(1f, 0.95f, 0.85f, fade));
                Shadowed(new Rect(W - 760, H - 272, 740, 28), regionSub, new GUIStyle(small) { fontSize = 17, alignment = TextAnchor.UpperRight }, new Color(0.95f, 0.9f, 0.8f, fade));
            }
            if (Time.unscaledTime < radioUntil && Mode != Mode.Intro)
            {
                Fill(new Rect(W / 2 - 200, 96, 400, 56), new Color(0, 0, 0, 0.6f));
                GUI.Label(new Rect(W / 2 - 200, 100, 400, 28), radioTitle, new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 20, normal = { textColor = new Color(1f, 0.85f, 0.3f) } });
                GUI.Label(new Rect(W / 2 - 200, 124, 400, 24), radioSub, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
            }
            if (Time.unscaledTime < toastUntil) Center(toast, H - 205, toastBad ? Colored(label, new Color(1f, 0.5f, 0.45f)) : Colored(label, new Color(0.75f, 1f, 0.6f)));
            if (Time.unscaledTime < cardUntil) { Fill(new Rect(0, H / 2 - 70, W, 120), new Color(0, 0, 0, 0.7f)); GUI.Label(new Rect(0, H / 2 - 60, W, 100), card, huge); }
            if (Time.unscaledTime < flashUntil) Fill(new Rect(0, 0, W, H), new Color(flashCol.r, flashCol.g, flashCol.b, 0.45f * (flashUntil - Time.unscaledTime) / 0.7f));

            switch (Mode)
            {
                case Mode.Intro: Intro(); break;
                case Mode.Dialogue: Dialogue(); break;
                case Mode.Choice: ChoiceMenu(); break;
                case Mode.Map: BigMap(); break;
                case Mode.End: End(); break;
                case Mode.Arrested: ArrestedScreen(); break;
            }
        }

        void Prompts()
        {
            var p = Player.I; string t = null;
            if (p.Driving != null) t = "[F]  Get out";
            else if (p.Near != null) t = "[E]  " + p.Near.Prompt;
            else if (p.NearCar != null) t = "[F]  " + (p.NearCar.State == Car.Mode.Traffic ? "Take the " : "Drive the ") + p.NearCar.Label.ToLower();
            if (t != null) { Fill(new Rect(W / 2 - 230, H - 168, 460, 34), new Color(0, 0, 0, 0.55f)); GUI.Label(new Rect(W / 2 - 230, H - 166, 460, 30), t, new GUIStyle(label) { alignment = TextAnchor.MiddleCenter }); }
        }

        void DrawTags()
        {
            if (Player.I == null) return;
            foreach (var t in Tag.All)
            {
                if (t == null || !t.isActiveAndEnabled) continue;
                float d = Player.Flat(t.transform.position - Player.I.Pos);
                if (d > t.ShowRange) continue;
                if (!ToGui(t.transform.position + Vector3.up * t.Height, out var g)) continue;
                var st = t.Big ? tagBig : tag;
                float tw = t.Big ? 700f : 320f;
                var c = t.Col; c.a = Mathf.Clamp01((t.ShowRange - d) / (t.ShowRange * 0.3f));
                Shadowed(new Rect(g.x - tw / 2f, g.y - 14, tw, 28), t.Text, st, c);
            }
        }

        void DrawWaypoint()
        {
            var tp = Story.TargetPos; if (tp == null || Player.I == null) return;
            var w = new Vector3(tp.Value.x, 2.6f, tp.Value.y);
            bool front = ToGui(w, out var p);
            float dist = Player.Flat(w - Player.I.Pos);
            Color y = new Color(1f, 0.85f, 0.1f);
            var screen = new Rect(30, 30, W - 60, H - 60);
            if (front && screen.Contains(p))
            {
                float bob = Mathf.Sin(Time.time * 4f) * 5f;
                var m = GUI.matrix; GUIUtility.RotateAroundPivot(45, (p + new Vector2(0, bob)) * S);
                Fill(new Rect(p.x - 10, p.y - 10 + bob, 20, 20), y); GUI.matrix = m;
                Shadowed(new Rect(p.x - 50, p.y + 14 + bob, 100, 24), Mathf.RoundToInt(dist) + " m", tag, y);
                return;
            }
            var inner = new Rect(80, 120, W - 160, H - 340);
            Vector2 c = inner.center, d = p - c; if (!front) d = -d; d.Normalize();
            float tx = Mathf.Abs(d.x) > 1e-4f ? inner.width / 2f / Mathf.Abs(d.x) : float.MaxValue;
            float ty = Mathf.Abs(d.y) > 1e-4f ? inner.height / 2f / Mathf.Abs(d.y) : float.MaxValue;
            Vector2 e = c + d * Mathf.Min(tx, ty);
            var mm = GUI.matrix; GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, e * S);
            Fill(new Rect(e.x - 18, e.y - 5, 36, 10), y); Fill(new Rect(e.x + 8, e.y - 12, 7, 24), y); GUI.matrix = mm;
            Shadowed(new Rect(e.x - 50, e.y + 16, 100, 24), Mathf.RoundToInt(dist) + " m", tag, y);
        }

        void DrawHud()
        {
            var st = Story.Cur;
            Fill(new Rect(14, 14, 480, 80), new Color(0, 0, 0, 0.5f));
            GUI.Label(new Rect(24, 16, 460, 24), "OBJECTIVE", Colored(small, new Color(1f, 0.85f, 0.1f)));
            GUI.Label(new Rect(24, 36, 460, 56), st != null ? st.Obj : "", label);
            Shadowed(new Rect(W - 260, 14, 246, 34), Game.Naira(Game.Cash), new GUIStyle(label) { alignment = TextAnchor.MiddleRight, fontSize = 26, fontStyle = FontStyle.Bold }, new Color(0.45f, 1f, 0.3f));
            if (Radio.Current >= 0) Shadowed(new Rect(W - 360, 48, 346, 24), Radio.Stations[Radio.Current].Freq + " " + Radio.Stations[Radio.Current].Name + "   ·   R / T", new GUIStyle(small) { alignment = TextAnchor.MiddleRight }, new Color(1f, 0.85f, 0.4f));
            if (Time.unscaledTime < Radio.CaptionUntil) { float cw = Mathf.Min(640f, W - 520f - 280f); Fill(new Rect(510, 14, cw, 64), new Color(0, 0, 0, 0.5f)); GUI.Label(new Rect(518, 18, cw - 16, 58), Radio.Caption, new GUIStyle(small) { fontSize = 15, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.9f, 0.65f) } }); }

            float f = Mathf.Clamp01(Game.Inf / 100f);
            Fill(new Rect(14, H - 60, 270, 22), new Color(0, 0, 0, 0.6f));
            Fill(new Rect(16, H - 58, 266 * f, 18), Color.Lerp(new Color(0.4f, 0.85f, 0.3f), new Color(0.9f, 0.15f, 0.1f), f));
            Fill(new Rect(16 + 266 * Game.ShowingAt / 100f, H - 62, 2, 26), Color.white);
            Shadowed(new Rect(14, H - 90, 420, 26), (Game.Bitten ? "FEVER" : "FEVER  ·  cured with ZX") + "   ·   Doses " + Game.Doses + "   ·   Q to take one", small, Game.IsCovered ? new Color(0.75f, 0.95f, 0.65f) : new Color(1f, 0.45f, 0.35f));
            var p = Player.I;
            if (!Game.IsCovered) Shadowed(new Rect(14, H - 34, 400, 24), "Your veins are showing", small, new Color(1f, 0.45f, 0.35f));
            else if (p != null && p.Sneaking) Shadowed(new Rect(14, H - 34, 400, 24), "Sneaking", small, new Color(0.7f, 0.8f, 1f));
            if (p != null && p.Driving != null)
                Shadowed(new Rect(W - 520, H - 90, 250, 40), Mathf.RoundToInt(Mathf.Abs(p.Driving.Speed) * 3.6f) + " km/h", new GUIStyle(label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight }, Color.white);

            Minimap(new Rect(W - 234, H - 234, 220, 220));
        }

        // ── Maps ─────────────────────────────────────────────────────────────
        void MapContent(System.Func<Vector2, Vector2> M, bool labels)
        {
            if (labels)
                foreach (var (pt, name) in City.MapLabels) { var q = M(pt); Shadowed(new Rect(q.x - 90, q.y - 12, 180, 24), name, new GUIStyle(tag) { fontSize = 15 }, Color.white); }
            float dot = labels ? 14f : 9f;
            // Doses waiting on the street: a white tile with a green medical cross.
            if (Game.Bitten)
                foreach (var d in Pickup.All)
                {
                    if (!d.Ready) continue;
                    var q = M(new Vector2(d.transform.position.x, d.transform.position.z));
                    float s = labels ? 13f : 9f, bar = Mathf.Max(2f, s * 0.24f);
                    Fill(new Rect(q.x - s / 2 - 1, q.y - s / 2 - 1, s + 2, s + 2), new Color(0.05f, 0.2f, 0.05f));
                    Fill(new Rect(q.x - s / 2, q.y - s / 2, s, s), Color.white);
                    var g = new Color(0.1f, 0.75f, 0.2f);
                    Fill(new Rect(q.x - bar / 2, q.y - s / 2 + 1.5f, bar, s - 3f), g);
                    Fill(new Rect(q.x - s / 2 + 1.5f, q.y - bar / 2, s - 3f, bar), g);
                }
            var tp = Story.TargetPos;
            if (tp != null && Mathf.Sin(Time.unscaledTime * 6f) > -0.3f) { var q = M(tp.Value); Fill(new Rect(q.x - dot / 2 - 1, q.y - dot / 2 - 1, dot + 2, dot + 2), Color.black); Fill(new Rect(q.x - dot / 2, q.y - dot / 2, dot, dot), new Color(1f, 0.85f, 0.1f)); }
            if (Player.I != null)
            {
                var q = M(new Vector2(Player.I.Pos.x, Player.I.Pos.z));
                float yaw = Player.I.Driving != null ? Player.I.Driving.transform.eulerAngles.y : Player.I.transform.eulerAngles.y;
                // A dot with a nub pointing the way you face (north is up).
                Vector2 head = new Vector2(Mathf.Sin(yaw * Mathf.Deg2Rad), -Mathf.Cos(yaw * Mathf.Deg2Rad)) * dot * 0.9f;
                Fill(new Rect(q.x - dot / 2 - 1, q.y - dot / 2 - 1, dot + 2, dot + 2), Color.black);
                Fill(new Rect(q.x - dot / 2, q.y - dot / 2, dot, dot), new Color(0.5f, 1f, 0.4f));
                Fill(new Rect(q.x + head.x - dot / 4, q.y + head.y - dot / 4, dot / 2, dot / 2), Color.white);
            }
        }

        void Minimap(Rect area)
        {
            Fill(new Rect(area.x - 3, area.y - 3, area.width + 6, area.height + 6), new Color(0, 0, 0, 0.75f));
            GUI.BeginGroup(area); GuiOffset = area.position;
            Fill(new Rect(0, 0, area.width, area.height), Mats.Hex("1f4f60"));
            var inside = Player.I != null ? Interior.At(Player.I.Pos) : null;
            if (inside != null)
            {
                GUI.EndGroup(); GuiOffset = Vector2.zero;
                GUI.Label(area, "INSIDE\n" + inside.Name, new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
                return;
            }
            const float span = 320f; float k = area.width / span;
            Vector2 c = Player.I != null ? new Vector2(Player.I.Pos.x, Player.I.Pos.z) : Vector2.zero;
            if (MapShot.Tex != null)
            {
                var B = City.MapBounds;
                GUI.DrawTextureWithTexCoords(new Rect(0, 0, area.width, area.height), MapShot.Tex,
                    new Rect((c.x - span / 2f - B.xMin) / B.width, (c.y - span / 2f - B.yMin) / B.height, span / B.width, span / B.height));
            }
            MapContent(w => new Vector2(area.width / 2f + (w.x - c.x) * k, area.height / 2f - (w.y - c.y) * k), false);
            GUI.EndGroup(); GuiOffset = Vector2.zero;
            GUI.Label(new Rect(area.x, area.yMax + 2, area.width, 18), "M  map   ·   scroll  zoom", new GUIStyle(small) { fontSize = 12, alignment = TextAnchor.MiddleRight });
        }

        static float mapZoom = 1f;

        void BigMap()
        {
            Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.85f));
            var B = City.MapBounds;
            float w = Mathf.Min(W - 60, (H - 130) * B.width / B.height), h = w * B.height / B.width;
            var area = new Rect((W - w) / 2, 64, w, h);
            Shadowed(new Rect(area.x, 16, w, 44), "LAGOS", new GUIStyle(big) { fontSize = 34 }, new Color(0.9f, 0.15f, 0.1f));
            Fill(area, Mats.Hex("1f4f60"));
            // Scroll zooms in on Dayo; the view never leaves the city.
            mapZoom = Mathf.Clamp(mapZoom * (1f + Controls.Zoom * 0.15f), 1f, 6f);
            Vector2 me = Player.I != null && !Interior.Inside(Player.I.Pos) ? new Vector2(Player.I.Pos.x, Player.I.Pos.z) : B.center;
            float vw = B.width / mapZoom, vh = B.height / mapZoom;
            var V = new Rect(Mathf.Clamp(me.x - vw / 2f, B.xMin, B.xMax - vw), Mathf.Clamp(me.y - vh / 2f, B.yMin, B.yMax - vh), vw, vh);
            if (MapShot.Tex != null)
                GUI.DrawTextureWithTexCoords(area, MapShot.Tex, new Rect((V.xMin - B.xMin) / B.width, (V.yMin - B.yMin) / B.height, vw / B.width, vh / B.height));
            GUI.BeginGroup(area); GuiOffset = area.position;
            MapContent(p => new Vector2((p.x - V.xMin) / V.width * w, (1f - (p.y - V.yMin) / V.height) * h), true);
            GUI.EndGroup(); GuiOffset = Vector2.zero;
            GUI.Label(new Rect(area.x, area.yMax + 8, w, 50), "You (green)   ·   Doses (white cross)   ·   Objective (yellow): " + (Story.Cur != null ? Story.Cur.Obj : "") + "\nScroll or +/- to zoom   ·   M or Esc to close", label);
        }

        // ── Screens ──────────────────────────────────────────────────────────
        bool Btn(string t, bool enabled = true)
        {
            var old = GUI.enabled; GUI.enabled = enabled && old;
            bool r = GUILayout.Button(t, button); GUI.enabled = old; return r;
        }

        void Intro()
        {
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.72f));
            GUILayout.BeginArea(new Rect(W / 2 - 380, 70, 760, 600));
            GUILayout.Label("EKO LOCKDOWN", new GUIStyle(big) { fontSize = 64 });
            GUILayout.Label("Lagos, the early days of Eko Fever. The government says it is under control. The Taskforce takes anyone with dark veins to the quarantine camp in Ikorodu, and nobody comes back.", label);
            GUILayout.Space(12);
            GUILayout.Label("You are Dayo, a danfo driver, and you are infected. Work odd jobs across the city to raise the money for ZX, the one dose that stops the fever, before the Taskforce sees your arm.", label);
            GUILayout.Space(8);
            GUILayout.Label("Your fever climbs on its own. Past the white line, your veins show: take a street dose (Q) to push it down. Taskforce officers in orange vests chase anyone showing. They are slower than your sprint and can't catch a car. If they catch you, it's game over.", small);
            GUILayout.Space(14);
            GUILayout.Label("WASD  move      Shift  run      C  sneak      Space  jump      E  talk / use\nF  get in or out of a vehicle      Q  take a dose      M  map      R / T  radio      H  horn\nRight mouse drag or arrow keys  turn the camera      Scroll  zoom", small);
            GUILayout.Space(24);
            if (Btn("Begin")) { Mode = Mode.None; Bootstrap.StartCampaign(); }
            GUILayout.Space(10);
            GUILayout.Label(Radio.Credits, new GUIStyle(small) { fontSize = 12 });
            GUILayout.EndArea();
        }

        static Color SpeakerColor(string n)
        {
            switch (n)
            {
                case "Dayo": return new Color(1f, 0.85f, 0.25f);
                case "Ada": return new Color(1f, 0.6f, 0.8f);
                case "Mama Put": return new Color(1f, 0.55f, 0.25f);
                case "Baba Ijebu": return new Color(0.6f, 0.75f, 1f);
                case "Tunde": return new Color(0.4f, 0.9f, 0.85f);
                case "The Plug": return new Color(0.55f, 1f, 0.55f);
                case "Chief Adeyemi": return new Color(1f, 0.35f, 0.35f);
                case "Soldier": return new Color(0.65f, 0.75f, 0.45f);
                default: return new Color(0.75f, 0.75f, 0.75f);
            }
        }

        void Dialogue()
        {
            if (lines == null) return;
            string raw = lines[li]; int sep = raw.IndexOf('|');
            string who = raw.Substring(0, sep), text = raw.Substring(sep + 1);
            Fill(new Rect(0, H - 220, W, 220), new Color(0, 0, 0, 0.78f));
            var col = SpeakerColor(who);
            Fill(new Rect(60, H - 190, 86, 86), col);
            GUI.Label(new Rect(60, H - 190, 86, 86), who.Substring(0, 1), new GUIStyle(huge) { normal = { textColor = new Color(0, 0, 0, 0.75f) } });
            GUI.Label(new Rect(170, H - 196, 800, 34), who.ToUpper(), Colored(new GUIStyle(label) { fontStyle = FontStyle.Bold, fontSize = 22 }, col));
            int n = Mathf.Min(text.Length, Mathf.FloorToInt(shown));
            GUI.Label(new Rect(170, H - 160, W - 260, 140), text.Substring(0, n), new GUIStyle(label) { fontSize = 22 });
            GUI.Label(new Rect(W - 300, H - 40, 280, 28), shown >= text.Length ? "E · continue" : "E · skip", new GUIStyle(small) { alignment = TextAnchor.MiddleRight });
        }

        void ChoiceMenu()
        {
            Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.7f));
            GUILayout.BeginArea(new Rect(W / 2 - 340, 190, 680, 400));
            GUILayout.Label("THE CHOICE", big);
            GUILayout.Label("The Chief's hand is on his radio. Ada is behind you. The bridge closes at dawn.", label);
            GUILayout.Space(14);
            for (int i = 0; i < choiceOpts.Length; i++) if (Btn(choiceOpts[i])) { Mode = Mode.None; onChoice?.Invoke(i); break; }
            GUILayout.EndArea();
        }

        void ArrestedScreen()
        {
            Fill(new Rect(0, 0, W, H), new Color(0.06f, 0.02f, 0.02f, 0.94f));
            GUILayout.BeginArea(new Rect(W / 2 - 360, 120, 720, 520));
            GUILayout.Label("ARRESTED", new GUIStyle(big) { fontSize = 60, normal = { textColor = new Color(0.95f, 0.25f, 0.1f) } });
            GUILayout.Label(Game.Ending ?? "", new GUIStyle(label) { fontSize = 20 });
            GUILayout.Space(10);
            GUILayout.Label("GAME OVER", new GUIStyle(small) { fontSize = 18, normal = { textColor = new Color(1f, 0.6f, 0.5f) } });
            GUILayout.Space(16);
            if (Btn("Try again from the last checkpoint")) { Mode = Mode.None; Story.Retry(); }
            if (Btn("Start over")) Bootstrap.Restart();
            GUILayout.EndArea();
        }

        void End()
        {
            Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.03f, 0.02f, 0.92f));
            GUILayout.BeginArea(new Rect(W / 2 - 360, 110, 720, 520));
            GUILayout.Label(Game.EndingTitle ?? "THE END", new GUIStyle(big) { fontSize = 54, normal = { textColor = new Color(0.85f, 0.2f, 0.1f) } });
            GUILayout.Label(Game.Ending ?? "", new GUIStyle(label) { fontSize = 20 });
            GUILayout.Space(20);
            GUILayout.Label("Thanks for playing EKO LOCKDOWN.", small);
            GUILayout.Label(Radio.Credits, new GUIStyle(small) { fontSize = 12 });
            GUILayout.Space(14);
            if (Btn("Play again")) Bootstrap.Restart();
            GUILayout.EndArea();
        }
    }
}
