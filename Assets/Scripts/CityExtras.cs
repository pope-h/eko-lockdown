using UnityEngine;

// Eko Lockdown: the places you walk into (Kilimanjaro Bar, the Island Hospital), the waterfront
// (sea walls, rocks, foam, Lekki beach, boats) and the Taskforce posts.
namespace Eko
{
    public static partial class City
    {
        // ── Interiors ────────────────────────────────────────────────────────
        static readonly Vector3 BarOrigin = new Vector3(3000f, 0f, 0f), HospitalOrigin = new Vector3(3000f, 0f, 200f);

        static void Interiors()
        {
            Interior.All.Clear(); Story.Via.Clear();
            Bar();
            Hospital();
        }

        /// A room: floor, ceiling, four walls with a doorway in the south wall, and warm lights.
        static Interior Room(string name, Vector3 o, float w, float d, float h, Material floor, Material wall, Color light, int lights)
        {
            var g = G(Special);
            g.Ground(floor, new Rect(o.x - w / 2f, o.z - d / 2f, w, d), 0.01f, 3f);
            Col(o + Vector3.down * 0.25f, new Vector3(w, 0.5f, d));
            g.Box(o + Vector3.up * (h + 0.1f), new Vector3(w + 0.8f, 0.2f, d + 0.8f), Mats.Solid(H("e8e4dc")), Mats.Solid(H("e8e4dc")), null, true);
            void W(Vector3 c, Vector3 s) { g.Box(c, s, wall, wall); Col(c, s); }
            W(o + new Vector3(0, h / 2f, d / 2f + 0.2f), new Vector3(w + 0.8f, h, 0.4f));
            W(o + new Vector3(-w / 2f - 0.2f, h / 2f, 0), new Vector3(0.4f, h, d));
            W(o + new Vector3(w / 2f + 0.2f, h / 2f, 0), new Vector3(0.4f, h, d));
            float half = (w - 1.8f) / 2f;
            W(o + new Vector3(-w / 2f + half / 2f, h / 2f, -d / 2f - 0.2f), new Vector3(half, h, 0.4f));
            W(o + new Vector3(w / 2f - half / 2f, h / 2f, -d / 2f - 0.2f), new Vector3(half, h, 0.4f));
            W(o + new Vector3(0, h - 0.6f, -d / 2f - 0.2f), new Vector3(1.8f, 1.2f, 0.4f));
            // The street door behind you: solid, so the camera stays in the room.
            var dp = o + new Vector3(0, (h - 1.2f) / 2f, -d / 2f - 0.3f);
            g.Box(dp, new Vector3(1.8f, h - 1.2f, 0.2f), Mats.Solid(H("3a2416")), Mats.Solid(H("3a2416"))); Col(dp, new Vector3(1.8f, h - 1.2f, 0.2f));
            for (int i = 0; i < lights; i++)
            {
                var l = new GameObject("lamp").AddComponent<Light>(); l.transform.SetParent(root, false);
                l.transform.position = o + new Vector3((i - (lights - 1) / 2f) * (w / lights), h - 0.5f, 0);
                l.type = LightType.Point; l.range = Mathf.Max(w / lights, d) * 0.9f; l.intensity = 1.6f; l.color = light; l.shadows = LightShadows.None;
                g.Ball(l.transform.position + Vector3.up * 0.3f, new Vector3(0.25f, 0.12f, 0.25f), Mats.Glow("bulb" + name, Color.Lerp(light, Color.white, 0.5f)), 8, 4);
            }
            var room = new Interior { Name = name, Area = new Rect(o.x - w / 2f - 1f, o.z - d / 2f - 1f, w + 2f, d + 2f), Exit = o + new Vector3(0, 0, -d / 2f + 0.6f) };
            Interior.All.Add(room);
            return room;
        }

        /// The pair of doors joining a street doorway to a room.
        static void Link(Interior room, Vector3 street, float streetYaw, string streetPrompt, string arrive)
        {
            var inside = new GameObject("door in"); inside.transform.SetParent(root, false); inside.transform.position = street;
            var a = inside.AddComponent<Interactable>(); a.Kind = Kind.Door; a.Prompt = streetPrompt;
            var da = inside.AddComponent<Door>(); da.To = room.Exit + Vector3.forward * 1.2f + Vector3.up * 0.05f; da.Yaw = 0f; da.Arrive = arrive;
            Tag.Add(inside, "Door", 2.6f, new Color(1f, 0.9f, 0.55f), 12f);

            var outside = new GameObject("door out"); outside.transform.SetParent(root, false); outside.transform.position = room.Exit;
            var b = outside.AddComponent<Interactable>(); b.Kind = Kind.Door; b.Prompt = "Go back outside";
            var db = outside.AddComponent<Door>(); db.To = street + Quaternion.Euler(0, streetYaw, 0) * Vector3.forward * 0.9f + Vector3.up * 0.05f; db.Yaw = streetYaw;
            Tag.Add(outside, "Exit", 2.6f, new Color(1f, 0.9f, 0.55f), 12f);
        }

        static Human Seated(System.Random r, Look look, Vector3 p, float yaw)
        {
            var h = Human.Build(root, look, p, "Patron", yaw); h.Seated = true; h.AutoSpeed = false; h.DancePhase = (float)r.NextDouble() * 6f; return h;
        }

        static void Bar()
        {
            var o = BarOrigin; var g = G(Special);
            var room = Room("KILIMANJARO BAR", o, 20f, 15f, 4.2f, Mats.Paving, Mats.Solid(H("7a3048")), H("ffc070"), 3);
            var wood = Mats.Solid(H("8a5a32"), 0.4f); var top = Mats.Solid(H("2a1a12"), 0.7f);
            // Counter and the wall of bottles behind it.
            var cc = o + new Vector3(0, 0.55f, 4.4f);
            g.Box(cc, new Vector3(11f, 1.1f, 0.9f), wood, top); Col(cc, new Vector3(11f, 1.1f, 0.9f));
            for (int s = 0; s < 3; s++) g.Box(o + new Vector3(0, 1.3f + s * 0.6f, 7.25f), new Vector3(10f, 0.06f, 0.4f), wood);
            var br = new System.Random(5);
            for (int s = 0; s < 3; s++)
                for (float x = -4.6f; x <= 4.6f; x += 0.34f)
                {
                    var col = new[] { H("2f8a3a"), H("c87a1a"), H("d8e8e0"), H("6a1a1a"), H("2a4a8a") }[br.Next(5)];
                    g.Cyl(o + new Vector3(x, 1.33f + s * 0.6f, 7.2f), 0.07f, 0.05f, 0.34f + (float)br.NextDouble() * 0.12f, 6, Mats.Glow("bottle" + col, new Color(col.r, col.g, col.b, 0.85f)));
                }
            for (float x = -4f; x <= 4f; x += 1.6f)
            {
                g.Cyl(o + new Vector3(x, 0, 3.4f), 0.05f, 0.05f, 0.75f, 6, Mats.Metal, false);
                g.Cyl(o + new Vector3(x, 0.75f, 3.4f), 0.22f, 0.22f, 0.06f, 10, Mats.Solid(H("8a1a1a")));
            }
            Person("sule", "Sule (barman)", Looks.Sule, o + new Vector3(0, 0, 5.5f), 180f, new Color(1f, 0.75f, 0.45f));
            Story.Via["sule"] = new Vector2(-30f, 81.2f);

            var pr = new System.Random(8);
            Seated(pr, Looks.Citizen(pr), o + new Vector3(-2.4f, 0.1f, 3.4f), 0f);
            Seated(pr, Looks.Citizen(pr), o + new Vector3(2.4f, 0.1f, 3.4f), 0f);
            // Tables. Two of the drinkers are Taskforce, off duty and watching.
            int k = 0;
            foreach (var t in new[] { new Vector3(-6.5f, 0, -2.5f), new Vector3(-6.5f, 0, 2f), new Vector3(6.5f, 0, -2.5f), new Vector3(6.5f, 0, 2f) })
            {
                var tc = o + t;
                g.Cyl(tc, 0.08f, 0.08f, 0.75f, 6, Mats.Metal, false);
                g.Cyl(tc + Vector3.up * 0.75f, 0.65f, 0.65f, 0.05f, 14, wood);
                Col(tc + Vector3.up * 0.4f, new Vector3(1.3f, 0.8f, 1.3f));
                g.Cyl(tc + new Vector3(0.15f, 0.8f, 0.1f), 0.06f, 0.05f, 0.3f, 6, Mats.Glow("bottleH2f8a3a", new Color(0.18f, 0.54f, 0.23f, 0.85f)));
                foreach (var side in new[] { -1f, 1f })
                {
                    var sp = tc + new Vector3(side * 1.05f, 0, 0);
                    g.Box(sp + Vector3.up * 0.25f, new Vector3(0.5f, 0.5f, 0.5f), Mats.Solid(H("6a1a1a")));
                    bool tf = k == 2 || k == 3;
                    if (k % 3 != 1) Seated(pr, tf ? Looks.Taskforce(pr) : Looks.Citizen(pr), sp + new Vector3(0, -0.2f, 0), side < 0 ? 90f : 270f);
                    k++;
                }
            }
            // Pool table, a TV with the news, neon on the walls.
            var pt = o + new Vector3(0, 0.45f, -3f);
            g.Box(pt, new Vector3(2.6f, 0.9f, 1.4f), wood, Mats.Solid(H("1d6a3a"))); Col(pt, new Vector3(2.6f, 0.9f, 1.4f));
            g.Box(o + new Vector3(-9.75f, 2.4f, 0), new Vector3(0.1f, 1.2f, 2.1f), Mats.Glow("tv", H("7ab0ff")));
            Label("BREAKING: TASKFORCE CLEARS MUSHIN MARKET", o + new Vector3(-9.6f, 3.3f, 0), new Color(0.75f, 0.85f, 1f), 14f, false);
            foreach (var (c, s, col) in new[] { (o + new Vector3(0, 3.9f, 7.15f), new Vector3(16f, 0.08f, 0.08f), "ff3aa0"), (o + new Vector3(-9.75f, 3.9f, 0), new Vector3(0.08f, 0.08f, 13f), "3ad0ff"), (o + new Vector3(9.75f, 3.9f, 0), new Vector3(0.08f, 0.08f, 13f), "3ad0ff") })
                g.Box(c, s, Mats.Glow("neon" + col, H(col)));
            Label("KILIMANJARO BAR", o + new Vector3(0, 3.5f, 7.1f), new Color(1f, 0.45f, 0.75f), 25f);

            // The street side: a doorway in a Mushin shopfront, an awning and a neon sign.
            var door = new Vector3(-30f, 0.165f, 82.4f);
            g.Box(door + new Vector3(0, 1.3f, 0), new Vector3(1.8f, 2.6f, 0.14f), Mats.Solid(H("2a1a14")));
            g.Box(door + new Vector3(0, 2.95f, -0.9f), new Vector3(3.6f, 0.12f, 1.9f), Mats.Solid(H("8a1a2a")));
            g.Box(door + new Vector3(0, 3.45f, -0.05f), new Vector3(3.6f, 0.6f, 0.12f), Mats.Glow("barsign", H("ff3aa0")));
            Label("KILIMANJARO BAR", door + new Vector3(0, 4.3f, -0.3f), new Color(1f, 0.5f, 0.8f), 45f, false);
            Link(room, door + new Vector3(0, 0, -1.2f), 180f, "Enter Kilimanjaro Bar", "KILIMANJARO BAR");
            MapLabels.Add((new Vector2(-30f, 90f), "Kilimanjaro Bar"));
        }

        static void Hospital()
        {
            var o = HospitalOrigin; var g = G(Special);
            var room = Room("ISLAND HOSPITAL", o, 26f, 18f, 4f, Mats.Solid(H("cfd8d4"), 0.5f), Mats.Solid(H("e2ece6")), H("eef6ff"), 3);
            var white = Mats.Solid(H("f4f6f6")); var teal = Mats.Solid(H("2a8a8a"));
            // Reception
            var rc = o + new Vector3(-4f, 0.55f, -5.5f);
            g.Box(rc, new Vector3(4f, 1.1f, 0.8f), teal, white); Col(rc, new Vector3(4f, 1.1f, 0.8f));
            var nurse = Human.Build(root, Looks.Nurse, o + new Vector3(-4f, 0, -4.6f), "Nurse", 180f); nurse.gameObject.AddComponent<Idler>();
            Label("RECEPTION", o + new Vector3(-4f, 2.6f, -5.5f), new Color(0.8f, 1f, 0.95f), 14f, false);

            // Ward B: beds with the fevered, curtains between them. Ada is on shift.
            var pr = new System.Random(31);
            for (int i = 0; i < 4; i++)
            {
                var bc = o + new Vector3(4.5f + i * 2.4f, 0, 5.5f);
                g.Box(bc + Vector3.up * 0.35f, new Vector3(1.0f, 0.7f, 2.1f), Mats.Metal, white); Col(bc + Vector3.up * 0.35f, new Vector3(1.0f, 0.7f, 2.1f));
                g.Box(bc + new Vector3(0, 0.78f, 0.8f), new Vector3(0.8f, 0.14f, 0.4f), white);
                g.Box(bc + new Vector3(1.2f, 1.2f, 0), new Vector3(0.04f, 2.2f, 2.2f), Mats.Solid(H("a8d0d8")));
                var p = Human.Build(root, Looks.Citizen(pr), bc + new Vector3(0, 0.55f, -0.3f), "Patient", 180f); p.Down = true; p.AutoSpeed = false;
            }
            Label("WARD B", o + new Vector3(8f, 3.2f, 8.8f), new Color(0.8f, 1f, 0.95f), 18f, false);
            Person("ada", "Ada", Looks.Ada, o + new Vector3(7f, 0, 2.5f), 180f, new Color(1f, 0.75f, 0.85f));
            Story.Via["ada"] = new Vector2(790f, 116.3f);

            // Dr Bello's private consulting room, walled off on the west side.
            var wall = Mats.Solid(H("d4e2dc"));
            void W(Vector3 c, Vector3 s) { g.Box(c, s, wall, wall); Col(c, s); }
            W(o + new Vector3(-3f, 2f, 5f), new Vector3(0.25f, 4f, 8f));
            W(o + new Vector3(-9.5f, 2f, 1f), new Vector3(7f, 4f, 0.25f));
            var dc = o + new Vector3(-8f, 0.45f, 5f);
            g.Box(dc, new Vector3(2.4f, 0.9f, 1.1f), Mats.Solid(H("5a3a20")), Mats.Solid(H("3a2416"))); Col(dc, new Vector3(2.4f, 0.9f, 1.1f));
            g.Box(o + new Vector3(-12.6f, 1.2f, 5f), new Vector3(0.6f, 2.4f, 3.5f), Mats.Metal);
            for (int s = 0; s < 4; s++) for (int v = 0; v < 8; v++)
                g.Cyl(o + new Vector3(-12.25f, 0.35f + s * 0.55f, 3.5f + v * 0.4f), 0.06f, 0.06f, 0.2f, 6, Mats.Glow("zx", new Color(0.35f, 1f, 0.45f, 1f)));
            Person("bello", "Dr Bello", Looks.Bello, o + new Vector3(-8f, 0, 6.3f), 180f, new Color(0.8f, 0.95f, 1f));
            Story.Via["bello"] = new Vector2(790f, 116.3f);
            Label("DR BELLO · PRIVATE CONSULTATION", o + new Vector3(-6.5f, 3.3f, 1.2f), new Color(0.8f, 0.95f, 1f), 18f, false);

            // The street side: glass doors under the existing canopy.
            var door = new Vector3(790f, 0.166f, 117.85f);
            g.Box(door + new Vector3(0, 1.3f, 0), new Vector3(2.4f, 2.6f, 0.1f), Mats.Window);
            g.Box(door + new Vector3(0, 1.3f, 0.02f), new Vector3(0.06f, 2.6f, 0.12f), Mats.Metal);
            Link(room, door + new Vector3(0, 0, -1.5f), 180f, "Enter the Island Hospital", "ISLAND HOSPITAL");
        }

        // ── Black-market diesel in Apapa ─────────────────────────────────────
        static void Depot()
        {
            var g = G(Apapa);
            var p = new Vector3(-60f, 0.165f, -141.5f);
            UseProp("diesel", "Diesel jerrycans", p, new Vector3(1.6f, 0.6f, 0.8f), H("d8b020"));
            for (int i = 0; i < 6; i++) g.Cyl(p + new Vector3(-2.6f + (i % 3) * 0.7f, 0, 0.2f + (i / 3) * 0.7f), 0.3f, 0.3f, 0.9f, 10, Mats.Solid(H(i % 2 == 0 ? "2a5fa8" : "c0302a")));
            g.Box(p + new Vector3(2.4f, 1.1f, 0.6f), new Vector3(2.6f, 0.08f, 2f), Mats.Zinc);
            Label("BLACK MARKET DIESEL", p + new Vector3(0, 2.6f, 0), new Color(1f, 0.85f, 0.3f), 40f, false);
        }

        // ── Taskforce posts ──────────────────────────────────────────────────
        /// Officers who hold a corner on the roads between the story stops, so every trip has a risk.
        static void TaskforcePosts()
        {
            var r = new System.Random(9);
            foreach (var (x, z) in new[] { (150f, 157f), (228f, 190f), (-75f, -158f), (-232f, -158f), (-300f, -88f), (98f, -158f), (150f, 82f), (300f, 157f), (420f, 141.5f), (-82f, 75f) })
            {
                var h = Human.Build(root, Looks.Taskforce(r), new Vector3(x, 0.17f, z), "Taskforce", (float)r.NextDouble() * 360f);
                h.gameObject.AddComponent<Enforcer>().Home = h.transform.position;
            }
        }

        // ── Waterfront ───────────────────────────────────────────────────────
        static bool InGap(float v, float[] gaps) { foreach (var c in gaps) if (Mathf.Abs(v - c) < 9.5f) return true; return false; }

        /// Sea walls with a rail, rocks at the waterline, a line of foam, and a beach on Lekki's south shore.
        static void Waterfront()
        {
            var g = G(Waterside);
            var wallMat = Mats.Concrete; var rail = Mats.Solid(H("d8d4c8"), 0.4f);
            var rr = new System.Random(77);

            void Edge(Vector3 a, Vector3 b, Vector3 outward, float[] gaps, bool wall)
            {
                Vector3 dir = (b - a).normalized; float len = Vector3.Distance(a, b);
                // Wall and rail, broken where the bridges land.
                float s = 0f;
                while (s < len)
                {
                    float e = s; while (e < len && !InGap(Along(a, dir, e), gaps)) e += 1f;
                    if (wall && e - s > 1f)
                    {
                        Vector3 c = a + dir * ((s + e) / 2f) - outward * 0.25f;
                        Vector3 size = Mathf.Abs(dir.x) > 0.5f ? new Vector3(e - s, 0.9f, 0.4f) : new Vector3(0.4f, 0.9f, e - s);
                        g.Box(c + Vector3.up * 0.45f, size, wallMat, wallMat); Col(c + Vector3.up * 0.45f, size);
                        g.Box(c + Vector3.up * 1.15f, Vector3.Scale(size, new Vector3(1, 0, 1)) + new Vector3(0, 0.08f, 0) + (Mathf.Abs(dir.x) > 0.5f ? new Vector3(0, 0, -0.3f) : new Vector3(-0.3f, 0, 0)), rail);
                        for (float t = s; t < e; t += 3f) g.Box(a + dir * t - outward * 0.25f + Vector3.up * 1.0f, new Vector3(0.08f, 0.3f, 0.08f), rail);
                    }
                    s = e; while (s < len && InGap(Along(a, dir, s), gaps)) s += 1f;
                }
                // Rocks and foam all along, gaps or not (the bridges stand on piers).
                for (float t = 1f; t < len; t += 3.2f)
                {
                    Vector3 p = a + dir * t + outward * (0.6f + (float)rr.NextDouble() * 1.4f) + Vector3.up * -0.7f;
                    float rad = 0.6f + (float)rr.NextDouble() * 0.7f;
                    g.Ball(p, new Vector3(rad * 1.3f, rad * 0.8f, rad), Mats.Stone, 6, 4);
                }
                Vector3 f0 = a + outward * 1.2f, f1 = b + outward * 3.6f;
                var fr = Rect.MinMaxRect(Mathf.Min(f0.x, f1.x), Mathf.Min(f0.z, f1.z), Mathf.Max(f0.x, f1.x), Mathf.Max(f0.z, f1.z));
                G(Waterside).Ground(Mats.Foam, fr, -0.56f, 6f);
            }
            float Along(Vector3 a, Vector3 dir, float t) { var p = a + dir * t; return Mathf.Abs(dir.x) > 0.5f ? p.x : p.z; }

            var M = Main.Land; var I = Isl.Land;
            Edge(new Vector3(M.xMin, 0, M.yMax), new Vector3(M.xMax, 0, M.yMax), Vector3.forward, new float[0], true);
            Edge(new Vector3(M.xMin, 0, M.yMin), new Vector3(M.xMin, 0, M.yMax), Vector3.left, new float[0], true);
            Edge(new Vector3(M.xMax, 0, M.yMin), new Vector3(M.xMax, 0, M.yMax), Vector3.right, new[] { 150f, -75f }, true);
            Edge(new Vector3(M.xMin, 0, M.yMin), new Vector3(-150f, 0, M.yMin), Vector3.back, new float[0], true);
            Edge(new Vector3(-150f, 0, M.yMin), new Vector3(M.xMax, 0, M.yMin), Vector3.back, new float[0], false); // the port quay: bollards, no wall
            for (float x = -140f; x < M.xMax; x += 12f) g.Cyl(new Vector3(x, 0f, M.yMin + 0.6f), 0.25f, 0.18f, 0.6f, 8, Mats.Solid(H("2a2a2a")));
            Edge(new Vector3(I.xMin, 0, I.yMax), new Vector3(I.xMax, 0, I.yMax), Vector3.forward, new float[0], true);
            Edge(new Vector3(I.xMin, 0, I.yMin), new Vector3(I.xMin, 0, I.yMax), Vector3.left, new[] { 150f, -75f }, true);
            Edge(new Vector3(I.xMax, 0, I.yMin), new Vector3(I.xMax, 0, I.yMax), Vector3.right, new float[0], true);
            Beach(I);
            Boats();
        }

        /// Lekki's south shore slopes into the water as sand, with palms and umbrellas.
        static void Beach(Rect I)
        {
            var g = G(Lekki);
            const float depth = 26f, drop = 1.2f;
            float ang = Mathf.Atan2(drop, depth) * Mathf.Rad2Deg;
            var c = new Vector3(I.center.x, -drop / 2f - 0.5f, I.yMin - depth / 2f);
            var q = Quaternion.Euler(-ang, 0, 0);
            g.Box(c, new Vector3(I.width, 1f, depth), q, Mats.Sand, Mats.Sand, new Vector2(8f, 8f));
            var col = new GameObject("beach"); col.transform.SetParent(cols, false); col.transform.position = c; col.transform.rotation = q;
            col.AddComponent<BoxCollider>().size = new Vector3(I.width, 1f, depth);
            var br = new System.Random(12);
            for (float x = I.xMin + 12f; x < I.xMax - 10f; x += 22f)
            {
                float t = 3f + (float)br.NextDouble() * 6f;
                Palm(g, new Vector3(x + (float)br.NextDouble() * 6f, -t * drop / depth, I.yMin - t));
                var u = new Vector3(x + 9f, -9f * drop / depth, I.yMin - 9f);
                g.Cyl(u, 0.04f, 0.04f, 2.3f, 6, Mats.Metal, false);
                g.Cyl(u + Vector3.up * 2.1f, 1.4f, 0.05f, 0.45f, 10, Mats.Solid(H(new[] { "e0302a", "f0c020", "2a8ad0" }[br.Next(3)])));
            }
            Label("LEKKI BEACH", new Vector3(I.center.x, 4f, I.yMin - 8f), new Color(1f, 0.95f, 0.75f), 120f, false);
            MapLabels.Add((new Vector2(I.center.x, I.yMin - 12f), "Lekki Beach"));
        }

        /// Fishing canoes and a speedboat rocking on the lagoon.
        static void Boats()
        {
            var br = new System.Random(4);
            foreach (var (x, z, yaw, speed) in new[] { (500f, 60f, 20f, false), (540f, 20f, 110f, false), (560f, 100f, 70f, true), (480f, -150f, 160f, false), (520f, -200f, 40f, false), (700f, 270f, 90f, true), (-200f, 330f, 10f, false), (300f, 340f, 75f, false) })
            {
                var go = new GameObject(speed ? "Speedboat" : "Canoe"); go.transform.SetParent(root, false);
                go.transform.position = new Vector3(x, -0.6f, z); go.transform.rotation = Quaternion.Euler(0, yaw, 0);
                var g = new Geo();
                if (speed)
                {
                    g.Box(new Vector3(0, 0.25f, 0), new Vector3(2.2f, 0.7f, 6.5f), Mats.Solid(H("f2f2f0")), Mats.Solid(H("d8d8d4")));
                    g.Box(new Vector3(0, 0.25f, 3.6f), new Vector3(1.2f, 0.7f, 1.2f), Quaternion.Euler(0, 45f, 0), Mats.Solid(H("f2f2f0")));
                    g.Box(new Vector3(0, 0.95f, 0.6f), new Vector3(1.8f, 0.6f, 0.08f), Mats.Window);
                    g.Box(new Vector3(0, 0.1f, 0), new Vector3(2.25f, 0.15f, 6.55f), Mats.Solid(H("1a4a8a")));
                }
                else
                {
                    var wood = Mats.Solid(H(br.Next(2) == 0 ? "6b4a2e" : "2a6a8a"));
                    g.Box(new Vector3(0, 0.15f, 0), new Vector3(1.1f, 0.5f, 6f), wood, Mats.Solid(H("3a2a1a")));
                    g.Box(new Vector3(0, 0.15f, 3.2f), new Vector3(0.6f, 0.45f, 0.9f), Quaternion.Euler(0, 45f, 0), wood);
                    g.Box(new Vector3(0, 0.15f, -3.2f), new Vector3(0.6f, 0.45f, 0.9f), Quaternion.Euler(0, 45f, 0), wood);
                }
                foreach (var (m, mat) in g.Meshes()) Geo.Attach(go.transform, m, mat);
                if (!speed)
                {
                    var f = Human.Build(go.transform, Looks.Citizen(br), go.transform.position + Vector3.up * 0.1f, "Fisherman", yaw);
                    f.transform.localPosition = new Vector3(0, -0.25f, -1.5f); f.transform.localRotation = Quaternion.identity; f.Seated = true; f.AutoSpeed = false;
                }
                go.AddComponent<Bob>();
            }
        }
    }

    /// Rocks gently on the water.
    public class Bob : MonoBehaviour
    {
        Vector3 basePos; Quaternion baseRot; float ph;
        void Start() { basePos = transform.position; baseRot = transform.rotation; ph = Random.value * 10f; }
        void Update()
        {
            float t = Time.time + ph;
            transform.position = basePos + Vector3.up * Mathf.Sin(t * 1.1f) * 0.12f;
            transform.rotation = baseRot * Quaternion.Euler(Mathf.Sin(t * 0.8f) * 2.5f, 0, Mathf.Sin(t * 1.3f) * 4f);
        }
    }

    /// Scrolls the water and the glints on it, and makes the foam breathe.
    public class WaterMotion : MonoBehaviour
    {
        void Update()
        {
            float t = Time.time;
            Mats.Water.mainTextureOffset = new Vector2(t * 0.012f, t * 0.007f);
            Mats.Glint.mainTextureOffset = new Vector2(-t * 0.02f, t * 0.011f);
            var f = Mats.Foam.color; f.a = 0.35f + 0.2f * Mathf.Sin(t * 1.4f); Mats.Foam.color = f;
        }
    }
}
