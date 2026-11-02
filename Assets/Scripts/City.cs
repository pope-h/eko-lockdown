using System.Collections.Generic;
using UnityEngine;

// Eko Lockdown: builds Lagos. Six districts at street scale on a road grid, the lagoon,
// two bridges to the island, and every story location.
// Coordinates are metres; x is east, z is north.
namespace Eko
{
    /// A district's street grid: lines of road every 75 m, arterials every 150 m.
    public class Grid3
    {
        public string Name; public float[] Xs, Zs; public Rect Land;
        public readonly HashSet<float> NoTrafficX = new HashSet<float>(), NoTrafficZ = new HashSet<float>();
        public Grid3(string name, float x0, float x1, float z0, float z1, float step)
        {
            Name = name; Xs = Range(x0, x1, step); Zs = Range(z0, z1, step);
            Land = new Rect(x0 - 7f, z0 - 7f, x1 - x0 + 14f, z1 - z0 + 14f);
        }
        static float[] Range(float a, float b, float s) { var l = new List<float>(); for (float v = a; v <= b + 0.01f; v += s) l.Add(v); return l.ToArray(); }
        public static float HW(float c) => Mathf.Abs(c % 150f) < 0.01f ? 7f : 4.5f;
    }

    public class Region { public string Name, Sub; public Rect R; public int Style; }

    public static partial class City
    {
        public const int Ikeja = 0, Mushin = 1, Yaba = 2, Ajegunle = 3, Apapa = 4, Lekki = 5, Waterside = 6, Special = 7;
        public static Grid3 Main, Isl;
        public static readonly List<Region> Regions = new List<Region>();
        public static readonly List<(Rect r, Color c)> MapRects = new List<(Rect, Color)>();
        public static readonly List<(Vector2 p, string name)> MapLabels = new List<(Vector2, string)>();
        public static readonly Rect MapBounds = new Rect(-480, -360, 1500, 620);
        public static readonly Rect PortYard = new Rect(160, -290, 130, 58), Estate = new Rect(760, 10, 130, 130), PartyStreet = new Rect(222, 218, 82, 16);
        public static readonly Vector2 Start = new Vector2(24f, 80.2f);

        static System.Random rng;
        static Transform root, cols;
        static readonly Dictionary<int, Geo> geo = new Dictionary<int, Geo>();
        static Geo G(int k) { if (!geo.TryGetValue(k, out var g)) { g = new Geo(); geo[k] = g; } return g; }
        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static T Pick<T>(T[] a) => a[rng.Next(a.Length)];
        static Color H(string h) => Mats.Hex(h);

        public static Grid3 GridAt(Vector2 p) => Main.Land.Contains(p) ? Main : Isl.Land.Contains(p) ? Isl : null;
        public static Region RegionAt(Vector2 p) { foreach (var r in Regions) if (r.R.Contains(p)) return r; return null; }

        public static Color VehicleColour(VType t, System.Random r)
        {
            switch (t)
            {
                case VType.Danfo: return H("f2c018");
                case VType.Keke: return r.Next(2) == 0 ? H("2f8f3a") : H("f2c018");
                case VType.Truck: return H(new[] { "b23a2a", "2a5fa8", "c9822a", "e6e6e6" }[r.Next(4)]);
                case VType.Okada: return H(new[] { "c0302a", "1d1d22", "2a5fa8" }[r.Next(3)]);
                default: return H(new[] { "c8c8c8", "1f1f22", "8a1c1c", "1c3f8a", "ececec", "3b5d3b", "a37a1c", "6b6b70" }[r.Next(8)]);
            }
        }

        public static GameObject Build()
        {
            var go = new GameObject("City"); root = go.transform;
            cols = new GameObject("Colliders").transform; cols.SetParent(root, false);
            rng = new System.Random(20261102);
            geo.Clear(); MapRects.Clear(); MapLabels.Clear(); Regions.Clear();

            Main = new Grid3("Mainland", -450, 450, -300, 300, 75f);
            Isl = new Grid3("Island", 600, 975, -150, 225, 75f);
            Main.NoTrafficX.Add(225f); Main.NoTrafficZ.Add(225f);   // port yard road, party street
            Isl.NoTrafficX.Add(825f); Isl.NoTrafficZ.Add(75f);      // estate roads

            AddRegion("IKEJA", "Mainland · Offices, billboards and go-slow", new Rect(-457, 0, 307, 307), Ikeja, H("8a8f96"));
            AddRegion("MUSHIN", "Mainland · Market town", new Rect(-150, 0, 300, 307), Mushin, H("b08a5a"));
            AddRegion("YABA", "Mainland · Students, hustlers, generators", new Rect(150, 0, 307, 307), Yaba, H("7fa07a"));
            AddRegion("AJEGUNLE", "Mainland · Jungle city", new Rect(-457, -307, 307, 307), Ajegunle, H("9a7a5a"));
            AddRegion("APAPA", "Mainland · The port", new Rect(-150, -307, 607, 307), Apapa, H("7a7f88"));
            AddRegion("LEKKI", "The Island · Walls and money", new Rect(593, -157, 389, 389), Lekki, H("9fbf8a"));

            Water(); Land(Main); Land(Isl);
            Roads(Main); Roads(Isl);
            Blocks(Main); Blocks(Isl);
            Streetlights(Main); Streetlights(Isl);
            Bridges(); Market(); Party(); PortYardBuild(); Harbour(); EstateBuild(); StoryPeople(); Doses(); TaskforcePosts(); ParkedCars();
            Depot(); Interiors(); Waterfront();

            foreach (var kv in geo) kv.Value.Build(root, "geo" + kv.Key, kv.Key != Waterside);
            return go;
        }

        static void AddRegion(string name, string sub, Rect r, int style, Color mapCol)
        {
            Regions.Add(new Region { Name = name, Sub = sub, R = r, Style = style });
            MapLabels.Add((r.center, name));
            mapCol.a = 0.45f; MapRects.Add((r, mapCol));
        }

        static void Col(Vector3 c, Vector3 size)
        {
            var o = new GameObject("c"); o.transform.SetParent(cols, false); o.transform.position = c;
            o.AddComponent<BoxCollider>().size = size;
        }
        static void Label(string text, Vector3 p, Color c, float range = 80f, bool big = true)
        {
            var o = new GameObject("label"); o.transform.SetParent(root, false); o.transform.position = p;
            Tag.Add(o, text, 0f, c, range, big);
        }

        // ── Ground ───────────────────────────────────────────────────────────
        static void Water()
        {
            G(Waterside).Ground(Mats.Water, new Rect(-1000, -900, 2600, 1800), -0.6f, 30f);
            if (!TouchControls.Lite) G(Waterside).Ground(Mats.Glint, new Rect(-1000, -900, 2600, 1800), -0.57f, 45f);
        }

        static void Land(Grid3 g)
        {
            var c = new Vector3(g.Land.center.x, -1.5f, g.Land.center.y);
            var size = new Vector3(g.Land.width, 3f, g.Land.height);
            G(Special).Box(c, size, Mats.Quay, Mats.Dirt, new Vector2(6f, 6f));
            Col(c, size);
            MapRects.Insert(0, (g.Land, H("5a5a52")));
        }

        static int ChunkAt(float x, float z) { var r = RegionAt(new Vector2(x, z)); return r != null ? r.Style : Special; }

        static void Roads(Grid3 g)
        {
            var white = Mats.Solid(H("e8e8e2")); var yellow = Mats.Solid(H("e0b020"));
            // Roads along x (fixed z), split into 75 m pieces so each district draws its own.
            foreach (var z in g.Zs)
            {
                float hw = Grid3.HW(z);
                for (int i = 0; i < g.Xs.Length - 1; i++)
                {
                    float a = i == 0 ? g.Land.xMin : g.Xs[i], b = i == g.Xs.Length - 2 ? g.Land.xMax : g.Xs[i + 1];
                    var geo = G(ChunkAt((a + b) / 2f, z));
                    geo.Ground(Mats.Asphalt, new Rect(a, z - hw, b - a, hw * 2f), 0.02f, 8f);
                    Markings(geo, true, z, g.Xs[i], g.Xs[i + 1], hw > 5f ? yellow : white, hw > 5f);
                }
                MapRects.Add((new Rect(g.Land.xMin, z - hw, g.Land.width, hw * 2f), H("c8c8c0")));
            }
            foreach (var x in g.Xs)
            {
                float hw = Grid3.HW(x);
                for (int j = 0; j < g.Zs.Length - 1; j++)
                {
                    float a = j == 0 ? g.Land.yMin : g.Zs[j], b = j == g.Zs.Length - 2 ? g.Land.yMax : g.Zs[j + 1];
                    var geo = G(ChunkAt(x, (a + b) / 2f));
                    geo.Ground(Mats.Asphalt, new Rect(x - hw, a, hw * 2f, b - a), 0.03f, 8f);
                    Markings(geo, false, x, g.Zs[j], g.Zs[j + 1], hw > 5f ? yellow : white, hw > 5f);
                }
                MapRects.Add((new Rect(x - hw, g.Land.yMin, hw * 2f, g.Land.height), H("c8c8c0")));
            }
        }

        /// Centre-line paint between two crossings, plus zebra crossings on arterials.
        static void Markings(Geo geo, bool alongX, float line, float a, float b, Material m, bool arterial)
        {
            float a0 = a + Grid3.HW(a) + 1.5f, b0 = b - Grid3.HW(b) - 1.5f;
            Vector3 P(float along, float off, float y) => alongX ? new Vector3(along, y, line + off) : new Vector3(line + off, y, along);
            void Strip(float s, float e, float off, float w, float y, Material mat)
            {
                var p0 = P(s, off - w / 2f, y); var p1 = P(e, off + w / 2f, y);
                geo.Ground(mat, Rect.MinMaxRect(Mathf.Min(p0.x, p1.x), Mathf.Min(p0.z, p1.z), Mathf.Max(p0.x, p1.x), Mathf.Max(p0.z, p1.z)), y, 4f);
            }
            if (arterial) { Strip(a0, b0, -0.18f, 0.14f, 0.045f, m); Strip(a0, b0, 0.18f, 0.14f, 0.045f, m); }
            else for (float s = a0; s + 3f < b0; s += 6f) Strip(s, s + 3f, 0f, 0.14f, 0.045f, m);
            if (!arterial) return;
            var zebra = Mats.Solid(H("f2f2ee"));
            float hw = Grid3.HW(line);
            foreach (var (s0, dirn) in new[] { (a + Grid3.HW(a) + 0.5f, 1f), (b - Grid3.HW(b) - 0.5f, -1f) })
                for (float o = -hw + 0.6f; o < hw - 0.4f; o += 1.1f) Strip(s0, s0 + dirn * 2.6f, o + 0.3f, 0.55f, 0.05f, zebra);
        }

        // ── Blocks and buildings ─────────────────────────────────────────────
        static void Blocks(Grid3 g)
        {
            for (int i = 0; i < g.Xs.Length - 1; i++)
                for (int j = 0; j < g.Zs.Length - 1; j++)
                {
                    float x0 = g.Xs[i] + Grid3.HW(g.Xs[i]), x1 = g.Xs[i + 1] - Grid3.HW(g.Xs[i + 1]);
                    float z0 = g.Zs[j] + Grid3.HW(g.Zs[j]), z1 = g.Zs[j + 1] - Grid3.HW(g.Zs[j + 1]);
                    var block = Rect.MinMaxRect(x0, z0, x1, z1);
                    int style = ChunkAt(block.center.x, block.center.y);
                    var geo = G(style);
                    var c = new Vector3(block.center.x, 0.08f, block.center.y);
                    geo.Box(c, new Vector3(block.width, 0.16f, block.height), Mats.Kerb, Mats.Paving, new Vector2(2f, 2f));
                    Col(c, new Vector3(block.width, 0.16f, block.height));
                    var lot = Rect.MinMaxRect(x0 + 3f, z0 + 3f, x1 - 3f, z1 - 3f);
                    if (lot.Overlaps(PortYard) || lot.Overlaps(Estate)) continue;
                    Lot(geo, lot, style);
                    SidewalkTrees(geo, block, style);
                }
        }

        static Material GroundFor(int style) => style == Lekki || style == Yaba ? Mats.Grass : style == Ikeja || style == Apapa ? Mats.Concrete : Mats.Dirt;

        static readonly string[] Bright = { "e8d5a8", "c9e0e8", "f0b98a", "b8d8b0", "e8a8a8", "f4f0e0", "d8c060", "a8b8e0", "e0e0e0", "c8a080", "d9b8e0", "9fd0c8" };
        static readonly string[] Office = { "d6d2ca", "bcb8b0", "c8cdd2", "a9aeb5", "e2ddd2" };
        static readonly string[] Signs = { "c0302a", "2a5fa8", "2f8f3a", "e0a020", "8a2a8a", "e05a20", "1a8a8a" };

        static void Lot(Geo g, Rect L, int style)
        {
            g.Ground(GroundFor(style), L, 0.165f, 6f);
            if (style == Lekki) { Villas(g, L); return; }
            if (style == Apapa) { Warehouses(g, L); return; }

            float dMin, dMax, wMin, wMax, gap; int fMin, fMax;
            switch (style)
            {
                case Ikeja: dMin = 14; dMax = 22; wMin = 14; wMax = 26; gap = 2.5f; fMin = 3; fMax = 12; break;
                case Yaba: dMin = 12; dMax = 18; wMin = 10; wMax = 18; gap = 2f; fMin = 2; fMax = 6; break;
                case Ajegunle: dMin = 6; dMax = 10; wMin = 5; wMax = 9; gap = 1.5f; fMin = 1; fMax = 2; break;
                default: dMin = 10; dMax = 15; wMin = 8; wMax = 14; gap = 0.6f; fMin = 1; fMax = 3; break; // Mushin
            }
            float d = Mathf.Min(R(dMin, dMax), L.height / 2f - 3f, L.width / 2f - 3f);
            // Fronts face the street on all four sides of the block.
            Row(g, L.xMin, L.xMax, true, L.yMax - d, d, Vector3.forward, style, wMin, wMax, gap, fMin, fMax);
            Row(g, L.xMin, L.xMax, true, L.yMin, d, Vector3.back, style, wMin, wMax, gap, fMin, fMax);
            Row(g, L.yMin + d + 1f, L.yMax - d - 1f, false, L.xMin, d, Vector3.left, style, wMin, wMax, gap, fMin, fMax);
            Row(g, L.yMin + d + 1f, L.yMax - d - 1f, false, L.xMax - d, d, Vector3.right, style, wMin, wMax, gap, fMin, fMax);

            // The yard in the middle: trees, clutter, laundry.
            var yard = new Rect(L.xMin + d + 2f, L.yMin + d + 2f, L.width - 2 * d - 4f, L.height - 2 * d - 4f);
            if (yard.width > 4 && yard.height > 4)
                for (int i = 0; i < 3; i++)
                {
                    var p = new Vector3(R(yard.xMin, yard.xMax), 0.165f, R(yard.yMin, yard.yMax));
                    if (style == Ajegunle) Clutter(g, p); else if (rng.Next(2) == 0) Tree(g, p); else Palm(g, p);
                }
        }

        static void Row(Geo g, float from, float to, bool alongX, float at, float depth, Vector3 outward, int style, float wMin, float wMax, float gap, int fMin, float fMax)
        {
            float s = from;
            while (s < to - 3f)
            {
                float w = Mathf.Min(R(wMin, wMax), to - s);
                if (w < 4f) break;
                var r = alongX ? new Rect(s, at, w, depth) : new Rect(at, s, depth, w);
                int floors = Mathf.Clamp(Mathf.RoundToInt(R(fMin, fMax + 0.49f)), fMin, (int)fMax);
                Building(g, r, floors, style, outward);
                s += w + R(0f, gap);
            }
        }

        static void Building(Geo g, Rect b, int floors, int style, Vector3 outward)
        {
            float h = floors * 3.2f;
            int facade = style == Ikeja ? (rng.Next(10) < 6 ? 1 : 0) : style == Yaba ? (rng.Next(10) < 2 ? 1 : 0) : style == Ajegunle ? 3 : 0;
            Color wall = H(style == Ikeja && facade == 0 ? Pick(Office) : Pick(Bright));
            bool zinc = style == Ajegunle || (style == Mushin && rng.Next(2) == 0) || (style == Yaba && rng.Next(5) == 0);
            Material side = style == Ajegunle && rng.Next(3) == 0 ? Mats.Zinc : Mats.Facade(facade, wall);
            var c = new Vector3(b.center.x, 0.165f + h / 2f, b.center.y);
            var size = new Vector3(b.width, h, b.height);
            g.Box(c, size, side, Mats.Concrete, new Vector2(16f, 12.8f));
            Col(c, size);
            float top = 0.165f + h;

            if (zinc)
            {
                bool longX = b.width >= b.height;
                var q = Quaternion.Euler(longX ? 10f : 0f, 0f, longX ? 0f : 10f);
                g.Box(new Vector3(c.x, top + 0.5f, c.z), new Vector3(b.width + 0.8f, 0.1f, b.height + 0.8f), q, rng.Next(2) == 0 ? Mats.Rust : Mats.Zinc, null, new Vector2(2f, 2f));
            }
            else
            {
                // Parapet and rooftop kit: a water tank, an AC box, maybe a billboard.
                float pw = 0.25f;
                g.Box(new Vector3(c.x, top + 0.35f, b.yMax - pw / 2), new Vector3(b.width, 0.7f, pw), Mats.Concrete);
                g.Box(new Vector3(c.x, top + 0.35f, b.yMin + pw / 2), new Vector3(b.width, 0.7f, pw), Mats.Concrete);
                g.Box(new Vector3(b.xMin + pw / 2, top + 0.35f, c.z), new Vector3(pw, 0.7f, b.height), Mats.Concrete);
                g.Box(new Vector3(b.xMax - pw / 2, top + 0.35f, c.z), new Vector3(pw, 0.7f, b.height), Mats.Concrete);
                if (rng.Next(10) < 5) { var tp = new Vector3(R(b.xMin + 2, b.xMax - 2), top, R(b.yMin + 2, b.yMax - 2)); g.Box(tp + Vector3.up * 0.4f, new Vector3(1.6f, 0.8f, 1.6f), Mats.Metal); g.Cyl(tp + Vector3.up * 0.8f, 0.8f, 0.8f, 1.6f, 10, Mats.Solid(rng.Next(2) == 0 ? H("1d1d1f") : H("2a5fa8"), 0.3f)); }
                if (rng.Next(10) < 4) g.Box(new Vector3(R(b.xMin + 1.5f, b.xMax - 1.5f), top + 0.5f, R(b.yMin + 1.5f, b.yMax - 1.5f)), new Vector3(1.2f, 1f, 0.9f), Mats.Solid(H("d8d8d4")));
                if (style == Ikeja && floors >= 4 && rng.Next(10) < 3)
                {
                    var bp = new Vector3(c.x, top, c.z);
                    g.Box(bp + Vector3.up * 2.5f, new Vector3(0.3f, 5f, 0.3f), Mats.Metal);
                    var face = Mathf.Abs(outward.x) > 0 ? new Vector3(0.3f, 4f, Mathf.Min(10f, b.height - 1f)) : new Vector3(Mathf.Min(10f, b.width - 1f), 4f, 0.3f);
                    g.Box(bp + Vector3.up * 6.5f, face, Mats.Solid(H(Pick(Signs)), 0.3f));
                    g.Box(bp + Vector3.up * 6.5f + outward * 0.2f, Vector3.Scale(face, new Vector3(0.8f, 0.4f, 0.8f)) + new Vector3(0.05f, 0, 0.05f), Mats.Solid(Color.white));
                }
            }

            // Shopfront on the street side: awning and signboard.
            if (style != Ajegunle && rng.Next(10) < 6)
            {
                float halfDepth = Mathf.Abs(outward.x) > 0 ? b.width / 2f : b.height / 2f;
                float frontW = (Mathf.Abs(outward.x) > 0 ? b.height : b.width) * 0.85f;
                Vector3 face = new Vector3(c.x, 0f, c.z) + outward * halfDepth;
                Vector3 across = Mathf.Abs(outward.x) > 0 ? new Vector3(0.12f, 0, frontW) : new Vector3(frontW, 0, 0.12f);
                Vector3 awn = Mathf.Abs(outward.x) > 0 ? new Vector3(1.6f, 0.12f, frontW) : new Vector3(frontW, 0.12f, 1.6f);
                var sign = Mats.Solid(H(Pick(Signs)), 0.3f);
                g.Box(face + outward * 0.8f + Vector3.up * 2.9f, awn, sign);
                g.Box(face + outward * 0.07f + Vector3.up * 3.6f, across + Vector3.up * 0.9f, Mats.Solid(H("f2efe6")));
                g.Box(face + outward * 0.14f + Vector3.up * 3.6f, Vector3.Scale(across, new Vector3(0.7f, 1, 0.7f)) + Vector3.up * 0.35f, sign);
                g.Box(face + outward * 0.06f + Vector3.up * 1.2f, Vector3.Scale(across, new Vector3(0.5f, 1, 0.5f)) + Vector3.up * 2.2f, Mats.Dark);
            }
            else if (style == Ajegunle && rng.Next(3) == 0)
            {
                // laundry line in front
                Vector3 face = new Vector3(c.x, 0f, c.z) + outward * ((Mathf.Abs(outward.x) > 0 ? b.width : b.height) / 2f + 0.8f);
                Vector3 along = Mathf.Abs(outward.x) > 0 ? Vector3.forward : Vector3.right;
                var cloth = Mathf.Abs(outward.x) > 0 ? new Vector3(0.05f, 0.6f, 0.5f) : new Vector3(0.5f, 0.6f, 0.05f);
                for (int i = -2; i <= 2; i++) g.Box(face + along * i * 0.8f + Vector3.up * 1.7f, cloth, Mats.Solid(Color.HSVToRGB((float)rng.NextDouble(), 0.6f, 0.9f)));
            }
        }

        static void Warehouses(Geo g, Rect L)
        {
            float x = L.xMin;
            while (x < L.xMax - 12f)
            {
                float w = Mathf.Min(R(18f, 30f), L.xMax - x);
                float d = R(18f, Mathf.Min(34f, L.height - 4f));
                bool north = rng.Next(2) == 0;
                var r = new Rect(x, north ? L.yMax - d : L.yMin, w, d);
                float h = R(8f, 12f);
                var c = new Vector3(r.center.x, 0.165f + h / 2f, r.center.y);
                g.Box(c, new Vector3(r.width, h, r.height), Mats.Facade(2, H(Pick(new[] { "9aa0a6", "8a6a4a", "6a7a8a", "a8a090" }))), Mats.Zinc, new Vector2(16f, 12.8f));
                Col(c, new Vector3(r.width, h, r.height));
                Vector3 outward = north ? Vector3.forward : Vector3.back;
                g.Box(new Vector3(c.x, 2.4f, north ? r.yMax + 0.05f : r.yMin - 0.05f), new Vector3(Mathf.Min(8f, w - 4f), 4.6f, 0.15f), Mats.Ribbed(H("50555c")));
                // containers in the other half
                var free = new Rect(x + 1f, north ? L.yMin + 1f : r.yMax + 2f, w - 2f, L.height - d - 4f);
                if (free.height > 3.2f)
                    for (float cz = free.yMin + 1.3f; cz < free.yMax - 1.3f; cz += 2.6f)
                        if (rng.Next(3) > 0) Container(g, new Vector3(free.center.x, 0.165f, cz), 0f, rng.Next(3) + 1, Mathf.Min(12.2f, free.width));
                x += w + R(3f, 7f);
            }
        }

        static readonly string[] Boxes = { "b23a2a", "2a5fa8", "2f7d4a", "c9822a", "6a3d8a", "dcdcd6", "8a2a2a" };
        /// A stack of shipping containers lying along x, standing on p.
        static void Container(Geo g, Vector3 p, float yaw, int stack, float len = 12.2f)
        {
            for (int s = 0; s < stack; s++)
            {
                var c = p + Vector3.up * (1.3f + s * 2.6f);
                g.Box(c, new Vector3(len, 2.55f, 2.44f), Quaternion.Euler(0, yaw, 0), Mats.Ribbed(H(Pick(Boxes))), null, new Vector2(2f, 2.6f));
            }
            var sz = Quaternion.Euler(0, yaw, 0) * new Vector3(len, stack * 2.6f, 2.44f);
            Col(p + Vector3.up * stack * 1.3f, new Vector3(Mathf.Abs(sz.x), Mathf.Abs(sz.y), Mathf.Abs(sz.z)));
        }

        static void Villas(Geo g, Rect L)
        {
            var wallMat = Mats.Solid(H("f2ece0"));
            int nx = 2, nz = 2;
            float pw = L.width / nx, ph = L.height / nz;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    var plot = new Rect(L.xMin + i * pw + 0.5f, L.yMin + j * ph + 0.5f, pw - 1f, ph - 1f);
                    // Fence, with a gate gap on the street side.
                    bool northSide = j == nz - 1;
                    float fh = 2.2f, t = 0.3f;
                    foreach (var (a, b2, alongX, at) in new[] { (plot.xMin, plot.xMax, true, plot.yMin), (plot.xMin, plot.xMax, true, plot.yMax), (plot.yMin, plot.yMax, false, plot.xMin), (plot.yMin, plot.yMax, false, plot.xMax) })
                    {
                        bool street = alongX && ((northSide && at == plot.yMax) || (!northSide && at == plot.yMin));
                        float mid = (a + b2) / 2f;
                        var segs = street ? new[] { (a, mid - 2.5f), (mid + 2.5f, b2) } : new[] { (a, b2) };
                        foreach (var (s0, s1) in segs)
                        {
                            var cc = alongX ? new Vector3((s0 + s1) / 2f, 0.165f + fh / 2f, at) : new Vector3(at, 0.165f + fh / 2f, (s0 + s1) / 2f);
                            var sz = alongX ? new Vector3(s1 - s0, fh, t) : new Vector3(t, fh, s1 - s0);
                            g.Box(cc, sz, wallMat); Col(cc, sz);
                        }
                    }
                    // The villa
                    float vw = Mathf.Min(16f, plot.width - 8f), vd = Mathf.Min(11f, plot.height - 10f);
                    var vc = new Vector3(plot.center.x, 0.165f + 3.2f, plot.center.y + (northSide ? -2f : 2f));
                    var vs = new Vector3(vw, 6.4f, vd);
                    g.Box(vc, vs, Mats.Facade(0, H(Pick(new[] { "f6f2ea", "efe6d2", "e8e0d0", "f0e8f0" }))), Mats.Concrete, new Vector2(16f, 12.8f));
                    Col(vc, vs);
                    g.Box(vc + Vector3.up * 3.45f, new Vector3(vw + 0.6f, 0.5f, vd + 0.6f), Mats.Solid(H("d8cfb8")));
                    float front = northSide ? vc.z + vd / 2f : vc.z - vd / 2f;
                    for (float x = vc.x - vw / 2f + 1.5f; x < vc.x + vw / 2f - 1f; x += 3f)
                        g.Cyl(new Vector3(x, 0.165f, front + (northSide ? 1.6f : -1.6f)), 0.22f, 0.22f, 3.2f, 8, Mats.Solid(H("f4f0e8")));
                    g.Box(new Vector3(vc.x, 3.45f, front + (northSide ? 1.7f : -1.7f)), new Vector3(vw - 1f, 0.25f, 3.6f), Mats.Solid(H("efe6d2")));
                    if (rng.Next(3) == 0) g.Box(new Vector3(plot.xMin + 3.5f, 0.2f, plot.center.y), new Vector3(4f, 0.12f, 6f), Mats.Water);
                    Palm(g, new Vector3(plot.xMax - 2f, 0.165f, plot.yMin + 2f));
                    Palm(g, new Vector3(plot.xMin + 2f, 0.165f, plot.yMax - 2f));
                }
        }

        static void SidewalkTrees(Geo g, Rect block, int style)
        {
            if (style == Ajegunle || style == Apapa || style == Mushin) return;
            float step = style == Lekki ? 14f : 22f;
            for (float x = block.xMin + 6f; x < block.xMax - 6f; x += step)
            {
                if (rng.Next(3) == 0) continue;
                var p = new Vector3(x, 0.165f, block.yMin + 1.3f);
                if (style == Ikeja) Tree(g, p); else Palm(g, p);
                p = new Vector3(x + step / 2f, 0.165f, block.yMax - 1.3f);
                if (style == Ikeja) Tree(g, p); else Palm(g, p);
            }
        }

        public static void Palm(Geo g, Vector3 p)
        {
            float h = R(6.5f, 9f);
            g.Cyl(p, 0.24f, 0.15f, h, 6, Mats.Trunk, false);
            Vector3 top = p + Vector3.up * h;
            var leaf = Mats.Leaf(rng.Next(4));
            for (int i = 0; i < 8; i++)
            {
                float yaw = i * 45f + R(-10f, 10f);
                var q = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(R(18f, 32f), 0, 0);
                g.Box(top + q * new Vector3(0, 0, 1.5f), new Vector3(0.55f, 0.05f, 3.2f), q, leaf);
            }
            g.Ball(top + Vector3.down * 0.3f, new Vector3(0.35f, 0.3f, 0.35f), Mats.Solid(H("5a3d1a")), 6, 4);
            Col(p + Vector3.up * 1.5f, new Vector3(0.4f, 3f, 0.4f));
        }

        public static void Tree(Geo g, Vector3 p)
        {
            g.Cyl(p, 0.3f, 0.22f, 3.2f, 6, Mats.Trunk, false);
            var a = Mats.Leaf(rng.Next(4)); var b = Mats.Leaf(rng.Next(4));
            g.Ball(p + new Vector3(0, 4.4f, 0), new Vector3(2.8f, 2.1f, 2.8f), a, 8, 5);
            g.Ball(p + new Vector3(1.1f, 5.3f, 0.4f), new Vector3(1.8f, 1.5f, 1.8f), b, 7, 5);
            g.Ball(p + new Vector3(-0.9f, 5.1f, -0.6f), new Vector3(1.7f, 1.4f, 1.7f), b, 7, 5);
            Col(p + Vector3.up * 1.5f, new Vector3(0.6f, 3f, 0.6f));
        }

        static void Clutter(Geo g, Vector3 p)
        {
            for (int i = 0; i < 3; i++)
            {
                var q = p + new Vector3(R(-1.5f, 1.5f), 0, R(-1.5f, 1.5f));
                if (rng.Next(2) == 0) g.Cyl(q, 0.3f, 0.3f, 0.9f, 8, Mats.Solid(H(Pick(new[] { "2a5fa8", "e0b020", "2f7d4a" }))));
                else g.Cyl(q, 0.42f, 0.42f, 0.25f, 10, Mats.Dark);
            }
        }

        static void Streetlights(Grid3 grid)
        {
            void Along(bool alongX, float line, float[] cross)
            {
                float hw = Grid3.HW(line);
                for (int i = 0; i < cross.Length - 1; i++)
                    for (float s = cross[i] + Grid3.HW(cross[i]) + 6f; s < cross[i + 1] - Grid3.HW(cross[i + 1]) - 4f; s += 30f)
                        foreach (float side in new[] { -1f, 1f })
                        {
                            float a = s + (side > 0 ? 15f : 0f);
                            if (a > cross[i + 1] - Grid3.HW(cross[i + 1]) - 4f) continue;
                            Vector3 basePt = alongX ? new Vector3(a, 0.165f, line + side * (hw + 0.6f)) : new Vector3(line + side * (hw + 0.6f), 0.165f, a);
                            if (Estate.Contains(new Vector2(basePt.x, basePt.z)) || PortYard.Contains(new Vector2(basePt.x, basePt.z))) continue;
                            Vector3 toRoad = alongX ? new Vector3(0, 0, -side) : new Vector3(-side, 0, 0);
                            Lamp(G(ChunkAt(basePt.x, basePt.z)), basePt, toRoad);
                        }
            }
            foreach (var z in grid.Zs) if (Grid3.HW(z) > 5f) Along(true, z, grid.Xs);
            foreach (var x in grid.Xs) if (Grid3.HW(x) > 5f) Along(false, x, grid.Zs);
        }

        static void Lamp(Geo g, Vector3 p, Vector3 toRoad)
        {
            g.Cyl(p, 0.11f, 0.08f, 7f, 6, Mats.Solid(H("3a3d42"), 0.4f), false);
            g.Box(p + Vector3.up * 7f + toRoad * 0.9f, new Vector3(Mathf.Abs(toRoad.x) * 1.8f + 0.1f, 0.1f, Mathf.Abs(toRoad.z) * 1.8f + 0.1f), Mats.Solid(H("3a3d42"), 0.4f));
            g.Box(p + Vector3.up * 6.88f + toRoad * 1.7f, new Vector3(0.5f, 0.12f, 0.5f), Mats.Lamp);
            var c = p + toRoad * 2.4f;
            g.Ground(Mats.Pool, new Rect(c.x - 5f, c.z - 5f, 10f, 10f), 0.06f, 10f);
            Col(p + Vector3.up * 1.5f, new Vector3(0.3f, 3f, 0.3f));
        }

        // ── Bridges ──────────────────────────────────────────────────────────
        static void Bridges()
        {
            var g = G(Special);
            foreach (var (z, w, name, closed) in new[] { (150f, 16f, "LEKKI LINK BRIDGE", false), (-75f, 14f, "THIRD MAINLAND BRIDGE · CLOSED", true) })
            {
                var c = new Vector3(525f, -0.5f, z);
                g.Box(c, new Vector3(140f, 1f, w), Mats.Quay, Mats.Asphalt, new Vector2(8f, 8f));
                Col(c, new Vector3(140f, 1f, w));
                foreach (float side in new[] { -1f, 1f })
                {
                    var rc = new Vector3(525f, 0.55f, z + side * (w / 2f - 0.2f));
                    g.Box(rc, new Vector3(140f, 1.1f, 0.35f), Mats.Concrete); Col(rc, new Vector3(140f, 1.1f, 0.35f));
                    for (float x = 470f; x <= 590f; x += 30f) Lamp(g, new Vector3(x, 0f, z + side * (w / 2f - 0.6f)), new Vector3(0, 0, -side));
                }
                for (float x = 465f; x <= 590f; x += 20f) g.Box(new Vector3(x, -4f, z), new Vector3(2.2f, 7f, w - 3f), Mats.Quay);
                for (float x = 460f; x < 590f; x += 6f) g.Ground(Mats.Solid(H("e8e8e2")), new Rect(x, z - 0.07f, 3f, 0.14f), 0.01f);
                MapRects.Add((new Rect(457f, z - w / 2f, 136f, w), H("c8c8c0")));
                MapLabels.Add((new Vector2(525f, z + 14f), closed ? "Third Mainland (closed)" : "Lekki Link Bridge"));
                Label(name, new Vector3(470f, 5f, z), closed ? new Color(1f, 0.55f, 0.3f) : new Color(0.85f, 0.9f, 1f), 70f);
                if (!closed) continue;
                // Army checkpoint
                for (float o = -5.5f; o <= 5.5f; o += 1.6f) { g.Box(new Vector3(482f, 0.45f, z + o), new Vector3(0.6f, 0.9f, 1.5f), Mats.Concrete); }
                g.Box(new Vector3(483.2f, 1.1f, z), new Vector3(0.12f, 0.5f, w - 1f), Mats.Solid(H("c0302a")));
                Col(new Vector3(482f, 1.5f, z), new Vector3(1.2f, 3f, w));
                for (int i = 0; i < 6; i++) g.Box(new Vector3(479.5f, 0.3f + (i / 3) * 0.4f, z - 4f + (i % 3) * 0.9f), new Vector3(0.6f, 0.38f, 0.85f), Mats.Solid(H("a89a6a")));
                var jeep = Car.Build(root, VType.Jeep, H("4b5a2a"), new Vector3(490f, 0f, z + 2.5f), 270f, Car.Mode.Parked);
                var r = new System.Random(3);
                foreach (var o in new[] { -3f, 3.5f })
                {
                    var s = Human.Build(root, Looks.Soldier(r), new Vector3(479f, 0f, z + o), "Soldier", 270f);
                    s.gameObject.AddComponent<Idler>(); Tag.Add(s.gameObject, "Soldier", 2.2f, new Color(0.75f, 0.85f, 0.55f), 20f);
                }
            }
        }

        // ── Mushin market and Mama Put's buka ────────────────────────────────
        static void Market()
        {
            var g = G(Mushin);
            var vr = new System.Random(41);
            for (float x = 30f; x <= 70f; x += 6.5f)
                foreach (var (zt, zv, yaw) in new[] { (80.9f, 82.1f, 180f), (69.1f, 67.9f, 0f) })
                {
                    Stall(g, new Vector3(x, 0.165f, zt), H(Pick(Signs)));
                    var v = Human.Build(root, Looks.Citizen(vr), new Vector3(x, 0.165f, zv), "Vendor", yaw);
                    v.gameObject.AddComponent<Idler>(); v.DancePhase = (float)vr.NextDouble() * 6f;
                }
            // Mama Put's buka: bigger canopy, pots on the fire, a bench.
            var p = new Vector3(14f, 0.165f, 80.9f);
            g.Cyl(p + new Vector3(0, 0, 0), 0.06f, 0.06f, 2.6f, 6, Mats.Metal, false);
            g.Cyl(p + Vector3.up * 2.4f, 2.3f, 0.1f, 0.7f, 12, Mats.Solid(H("e07b24")));
            g.Box(p + new Vector3(0, 0.45f, 0), new Vector3(2.4f, 0.9f, 0.9f), Mats.Wood); Col(p + new Vector3(0, 0.45f, 0), new Vector3(2.4f, 0.9f, 0.9f));
            g.Cyl(p + new Vector3(-0.6f, 0.9f, 0), 0.32f, 0.3f, 0.45f, 10, Mats.Dark);
            g.Cyl(p + new Vector3(0.5f, 0.9f, 0), 0.28f, 0.26f, 0.4f, 10, Mats.Metal);
            g.Box(p + new Vector3(3.2f, 0.25f, -0.6f), new Vector3(1.6f, 0.5f, 0.4f), Mats.Wood);
            Label("MAMA PUT · BUKA", p + new Vector3(0, 3.6f, 0), new Color(1f, 0.7f, 0.35f), 30f, false);
        }

        static void Stall(Geo g, Vector3 p, Color canopy)
        {
            g.Cyl(p + new Vector3(0, 0, 0.3f), 0.05f, 0.05f, 2.3f, 6, Mats.Metal, false);
            g.Cyl(p + new Vector3(0, 2.1f, 0.3f), 1.6f, 0.08f, 0.55f, 10, Mats.Solid(canopy));
            g.Box(p + new Vector3(0, 0.42f, 0), new Vector3(1.8f, 0.84f, 0.8f), Mats.Wood);
            Col(p + new Vector3(0, 0.42f, 0), new Vector3(1.8f, 0.84f, 0.8f));
            for (int i = 0; i < 5; i++) g.Box(p + new Vector3(-0.7f + i * 0.35f, 0.95f, 0), new Vector3(0.25f, 0.2f, 0.3f), Mats.Solid(Color.HSVToRGB((float)rng.NextDouble(), 0.7f, 0.9f)));
        }

        // ── Yaba owambe ──────────────────────────────────────────────────────
        static void Party()
        {
            var g = G(Yaba);
            float z = 225f;
            foreach (var (x, a, b) in new[] { (237f, "d4a017", "ffffff"), (262f, "7b2d8b", "ffffff"), (287f, "d4a017", "ffffff") })
            {
                for (int i = 0; i < 8; i++) g.Box(new Vector3(x - 5.25f + i * 1.5f, 3.4f, z), new Vector3(1.5f, 0.18f, 9f), Mats.Solid(H(i % 2 == 0 ? a : b)));
                foreach (var (dx, dz) in new[] { (-5.8f, -4.3f), (5.8f, -4.3f), (-5.8f, 4.3f), (5.8f, 4.3f) }) g.Cyl(new Vector3(x + dx, 0.02f, z + dz), 0.07f, 0.07f, 3.4f, 6, Mats.Solid(H("d8d8d8")), false);
            }
            for (float x = 226f; x < 299f; x += 1.2f) g.Box(new Vector3(x, 4.1f + Mathf.Sin(x) * 0.15f, z + 4.4f), new Vector3(0.5f, 0.4f, 0.04f), Mats.Solid(Color.HSVToRGB((x * 0.37f) % 1f, 0.75f, 0.95f)));
            // DJ booth and speakers
            g.Box(new Vector3(262f, 0.55f, 229.2f), new Vector3(3.4f, 1.1f, 1.2f), Mats.Dark); Col(new Vector3(262f, 0.55f, 229.2f), new Vector3(3.4f, 1.1f, 1.2f));
            g.Box(new Vector3(262f, 1.15f, 229.2f), new Vector3(1.6f, 0.08f, 0.7f), Mats.Metal);
            foreach (var x in new[] { 258.6f, 265.4f })
            {
                g.Box(new Vector3(x, 1.1f, 229.6f), new Vector3(1.1f, 2.2f, 1f), Mats.Solid(H("141416"))); Col(new Vector3(x, 1.1f, 229.6f), new Vector3(1.1f, 2.2f, 1f));
                g.Cyl(new Vector3(x, 0.9f, 229.08f), 0.35f, 0.35f, 0.04f, 10, Mats.Metal);
            }
            foreach (var x in new[] { 228.5f, 295.5f })
            {
                g.Box(new Vector3(x, 0.45f, 225f), new Vector3(1.2f, 0.9f, 4f), Mats.Solid(H("f4f2ec"))); Col(new Vector3(x, 0.45f, 225f), new Vector3(1.2f, 0.9f, 4f));
                for (int i = 0; i < 5; i++) g.Cyl(new Vector3(x, 0.9f, 223.4f + i * 0.8f), 0.25f, 0.22f, 0.15f, 8, Mats.Solid(Color.HSVToRGB(i * 0.12f, 0.75f, 0.9f)));
            }
            var gr = new System.Random(19);
            Color gold = H("d4a017"), purple = H("7b2d8b");
            for (int i = 0; i < 22; i++)
            {
                var look = Looks.Citizen(gr);
                var c = i % 2 == 0 ? gold : purple; look.Shirt = c; if (look.Skirt) look.Pants = c; look.Accent = c;
                var d = Human.Build(root, look, new Vector3(232f + (float)gr.NextDouble() * 58f, 0.02f, 221f + (float)gr.NextDouble() * 6f), "Guest", 180f + (float)gr.NextDouble() * 60f - 30f);
                d.Dancing = true; d.DancePhase = (float)gr.NextDouble() * 4f;
            }
            for (int i = 0; i < 6; i++)
            {
                var l = new GameObject("partylight"); l.transform.SetParent(root, false);
                l.transform.position = new Vector3(236f + i * 10f, 0.08f, 223.5f + (i % 2) * 2f); l.transform.localScale = Vector3.one * 7f;
                var mf = l.AddComponent<MeshFilter>(); var q = new Geo(); q.Ground(Mats.Solid(Color.white), new Rect(-0.5f, -0.5f, 1, 1), 0, 1); mf.sharedMesh = q.Meshes()[0].mesh;
                var mr = l.AddComponent<MeshRenderer>(); mr.material = new Material(Shader.Find("Sprites/Default")) { mainTexture = Mats.Radial() };
                l.AddComponent<BeatLight>().Phase = i * 0.37f;
            }
            var party = new GameObject("Owambe"); party.transform.SetParent(root, false); party.transform.position = new Vector3(262f, 2f, z);
            party.AddComponent<Owambe>();
            Label("OWAMBE", new Vector3(262f, 6.5f, z), new Color(1f, 0.82f, 0.3f), 90f);
            MapRects.Add((PartyStreet, H("c050a0")));
        }

        // ── Apapa port yard: the stealth level ───────────────────────────────
        static void PortYardBuild()
        {
            var g = G(Apapa);
            var y = PortYard;
            g.Ground(Mats.Concrete, new Rect(y.xMin, y.yMin, y.width, y.height), 0.17f, 6f);
            var fence = Mats.Solid(H("5a5e64"), 0.4f);
            void Fence(Vector3 a, Vector3 b)
            {
                var c = (a + b) / 2f + Vector3.up * 1.4f; var len = Vector3.Distance(a, b);
                var size = Mathf.Abs(a.x - b.x) > 0.1f ? new Vector3(len, 2.8f, 0.12f) : new Vector3(0.12f, 2.8f, len);
                g.Box(c, size, fence); Col(c, size);
                for (float t = 0; t <= len; t += 4f) g.Cyl(Vector3.Lerp(a, b, t / len), 0.07f, 0.07f, 3f, 6, Mats.Metal, false);
            }
            Fence(new Vector3(y.xMin, 0, y.yMin), new Vector3(y.xMax, 0, y.yMin));
            Fence(new Vector3(y.xMin, 0, y.yMin), new Vector3(y.xMin, 0, y.yMax));
            Fence(new Vector3(y.xMax, 0, y.yMin), new Vector3(y.xMax, 0, y.yMax));
            Fence(new Vector3(y.xMin, 0, y.yMax), new Vector3(220f, 0, y.yMax));
            Fence(new Vector3(230f, 0, y.yMax), new Vector3(y.xMax, 0, y.yMax));
            Label("APAPA PORT YARD", new Vector3(225f, 5f, y.yMax + 2f), new Color(1f, 0.7f, 0.4f), 60f);
            MapRects.Add((y, H("a06a3a")));
            MapLabels.Add((y.center + new Vector2(0, -6), "Port Yard"));

            foreach (var x in new[] { 168f, 181f, 194f, 207f, 241f, 254f, 267f, 280f }) { Container(g, new Vector3(x, 0.17f, -241f), 0f, rng.Next(3) + 1); Container(g, new Vector3(x, 0.17f, -243.5f), 0f, rng.Next(2) + 1); }
            foreach (var x in new[] { 168f, 181f, 207f, 241f, 267f, 280f }) { Container(g, new Vector3(x, 0.17f, -259.8f), 0f, rng.Next(3) + 1); Container(g, new Vector3(x, 0.17f, -262.3f), 0f, rng.Next(2) + 1); }
            foreach (var x in new[] { 168f, 181f, 194f, 207f, 241f, 254f }) Container(g, new Vector3(x, 0.17f, -279f), 0f, rng.Next(2) + 1);
            var oc = new Vector3(279f, 0.17f + 1.8f, -284f);
            g.Box(oc, new Vector3(10f, 3.6f, 6f), Mats.Facade(0, H("e8e6e0")), Mats.Concrete, new Vector2(16f, 12.8f)); Col(oc, new Vector3(10f, 3.6f, 6f));
            UseProp("desk", "Stamp desk", new Vector3(279f, 0.17f, -279.6f), new Vector3(1.6f, 0.9f, 0.7f), H("8a2a2a"));

            var gr = new System.Random(5);
            MakeGuard(gr, new[] { new Vector3(225, 0.17f, -236), new Vector3(225, 0.17f, -287) }, 1.8f);
            MakeGuard(gr, new[] { new Vector3(165, 0.17f, -252), new Vector3(288, 0.17f, -252) }, 1.7f);
            MakeGuard(gr, new[] { new Vector3(288, 0.17f, -270.5f), new Vector3(165, 0.17f, -270.5f) }, 1.6f);
            MakeGuard(gr, new[] { new Vector3(262, 0.17f, -276), new Vector3(288, 0.17f, -276), new Vector3(288, 0.17f, -288.3f), new Vector3(262, 0.17f, -288.3f) }, 1.5f);
        }

        static void Harbour()
        {
            var g = G(Special);
            foreach (var x in new[] { 60f, 330f })
            {
                foreach (var (dx, dz) in new[] { (-6f, -305f), (6f, -305f), (-6f, -318f), (6f, -318f) }) g.Box(new Vector3(x + dx, 18f, dz), new Vector3(1.2f, 37f, 1.2f), Mats.Solid(H("d8a020"), 0.4f));
                g.Box(new Vector3(x, 37f, -325f), new Vector3(14f, 2.2f, 50f), Mats.Solid(H("d8a020"), 0.4f));
                g.Box(new Vector3(x, 35f, -312f), new Vector3(3f, 3f, 4f), Mats.Solid(H("e8e6e0")));
            }
            // A cargo ship waiting outside, unloaded since the lockdown.
            var sc = new Vector3(195f, 0f, -350f);
            g.Box(sc, new Vector3(150f, 10f, 22f), Mats.Solid(H("2b2b30")), Mats.Solid(H("7a2a24")));
            g.Box(sc + new Vector3(0, -3.5f, 0), new Vector3(150.2f, 3f, 22.2f), Mats.Solid(H("8a2a24")));
            g.Box(sc + new Vector3(-62f, 13f, 0), new Vector3(16f, 16f, 18f), Mats.Facade(0, H("f2f2ee")), Mats.Concrete, new Vector2(16f, 12.8f));
            for (float x = -48f; x < 66f; x += 12.6f)
                for (float z = -8f; z <= 8f; z += 2.6f) Container(g, sc + new Vector3(x, 5f, z), 0f, 1 + rng.Next(3));
            Label("Cargo ship · held at anchor", sc + Vector3.up * 26f, new Color(0.8f, 0.85f, 0.9f), 160f, false);
        }

        // ── Lekki estate ─────────────────────────────────────────────────────
        static void EstateBuild()
        {
            var g = G(Lekki);
            var e = Estate;
            var lawn = Mats.Grass;
            foreach (var r in new[] { new Rect(760, 10, 60.5f, 60.5f), new Rect(829.5f, 10, 60.5f, 60.5f), new Rect(760, 79.5f, 60.5f, 60.5f), new Rect(829.5f, 79.5f, 60.5f, 60.5f) })
                g.Ground(lawn, r, 0.166f, 6f);
            var wallMat = Mats.Solid(H("efe6d2"));
            void Wall(Vector3 a, Vector3 b)
            {
                var c = (a + b) / 2f + Vector3.up * 1.6f; float len = Vector3.Distance(a, b);
                var size = Mathf.Abs(a.x - b.x) > 0.1f ? new Vector3(len, 3.2f, 0.5f) : new Vector3(0.5f, 3.2f, len);
                g.Box(c, size, wallMat); Col(c, size);
                g.Box(c + Vector3.up * 1.7f, Vector3.Scale(size, new Vector3(1, 0, 1)) + new Vector3(0, 0.2f, 0), Mats.Metal);
            }
            Wall(new Vector3(e.xMin, 0, e.yMin), new Vector3(e.xMax, 0, e.yMin));
            Wall(new Vector3(e.xMin, 0, e.yMax), new Vector3(e.xMax, 0, e.yMax));
            Wall(new Vector3(e.xMax, 0, e.yMin), new Vector3(e.xMax, 0, e.yMax));
            Wall(new Vector3(e.xMin, 0, e.yMin), new Vector3(e.xMin, 0, 70.5f));
            Wall(new Vector3(e.xMin, 0, 79.5f), new Vector3(e.xMin, 0, e.yMax));
            foreach (var z in new[] { 70f, 80f }) { g.Box(new Vector3(e.xMin, 2f, z), new Vector3(1.1f, 4f, 1.1f), Mats.Solid(H("d8cfb8"))); Col(new Vector3(e.xMin, 2f, z), new Vector3(1.1f, 4f, 1.1f)); }
            g.Box(new Vector3(e.xMin + 4f, 1.6f, 86f), new Vector3(3f, 3.2f, 3f), Mats.Facade(0, H("efe6d2")), Mats.Concrete, new Vector2(16f, 12.8f));

            // The gate: bars that slide away once the residents vote you in.
            var gate = new GameObject("EstateGate"); gate.transform.SetParent(root, false); gate.transform.position = new Vector3(e.xMin, 0f, 75f);
            var gg = new Geo();
            for (float z = -4.2f; z <= 4.2f; z += 0.45f) gg.Box(new Vector3(0, 1.5f, z), new Vector3(0.08f, 3f, 0.08f), Mats.Solid(H("1d1d20"), 0.6f, 0.6f));
            gg.Box(new Vector3(0, 2.9f, 0), new Vector3(0.12f, 0.15f, 8.8f), Mats.Solid(H("d4a017"), 0.7f, 0.7f));
            gg.Box(new Vector3(0, 0.3f, 0), new Vector3(0.12f, 0.15f, 8.8f), Mats.Solid(H("1d1d20"), 0.6f, 0.6f));
            foreach (var (m, mat) in gg.Meshes()) Geo.Attach(gate.transform, m, mat);
            var bc = gate.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 1.5f, 0); bc.size = new Vector3(0.4f, 3f, 9f);
            gate.AddComponent<EstateGate>().Col = bc;
            var knock = new GameObject("knock"); knock.transform.SetParent(root, false); knock.transform.position = new Vector3(e.xMin - 6f, 0f, 75f);
            var it = knock.AddComponent<Interactable>(); it.Kind = Kind.Gate; it.Id = "gate"; it.Prompt = "Talk to the soldiers at the checkpoint";
            Story.Pos["gate"] = new Vector2(knock.transform.position.x, 75f);
            Label("LEKKI GARDENS ESTATE", new Vector3(e.xMin - 1f, 5.5f, 75f), new Color(1f, 0.88f, 0.6f), 90f);
            MapRects.Add((e, H("e8dcb8")));
            MapLabels.Add((e.center, "Lekki Gardens"));

            // Island Hospital
            var hc = new Vector3(790f, 0.166f + 4.8f, 125f);
            g.Box(hc, new Vector3(30f, 9.6f, 14f), Mats.Facade(0, H("f6f8fa")), Mats.Concrete, new Vector2(16f, 12.8f)); Col(hc, new Vector3(30f, 9.6f, 14f));
            var red = Mats.Solid(H("c8202a"));
            g.Box(new Vector3(790f, 7.6f, 117.9f), new Vector3(3f, 0.9f, 0.2f), red); g.Box(new Vector3(790f, 7.6f, 117.9f), new Vector3(0.9f, 3f, 0.2f), red);
            g.Box(new Vector3(790f, 3.3f, 116.8f), new Vector3(10f, 0.3f, 2.6f), Mats.Solid(H("e8e8e4")));
            Label("ISLAND HOSPITAL", new Vector3(790f, 11.5f, 118f), new Color(0.95f, 0.97f, 1f), 50f, false);
            Car.Build(root, VType.Suv, H("f4f4f4"), new Vector3(808f, 0.17f, 110f), 0f, Car.Mode.Parked);

            // Chief's villa, pool, gazebo with the desk
            var vc = new Vector3(864f, 0.166f + 3.6f, 124f);
            g.Box(vc, new Vector3(32f, 7.2f, 16f), Mats.Facade(0, H("efe2c4")), Mats.Concrete, new Vector2(16f, 12.8f)); Col(vc, new Vector3(32f, 7.2f, 16f));
            g.Box(vc + Vector3.up * 3.85f, new Vector3(33f, 0.5f, 17f), Mats.Solid(H("d4a017"), 0.6f, 0.4f));
            for (float x = 850f; x <= 878f; x += 4f) g.Cyl(new Vector3(x, 0.166f, 114.6f), 0.3f, 0.3f, 6.6f, 10, Mats.Solid(H("f6f0e4")));
            g.Box(new Vector3(864f, 6.9f, 114.6f), new Vector3(31f, 0.4f, 3f), Mats.Solid(H("efe2c4")));
            g.Box(new Vector3(878f, 0.2f, 98f), new Vector3(12f, 0.12f, 6f), Mats.Water);
            g.Box(new Vector3(878f, 0.18f, 98f), new Vector3(13f, 0.08f, 7f), Mats.Solid(H("e8e2d4")));
            foreach (var (dx, dz) in new[] { (-1.8f, -1.8f), (1.8f, -1.8f), (-1.8f, 1.8f), (1.8f, 1.8f) }) g.Cyl(new Vector3(842f + dx, 0.166f, 100f + dz), 0.1f, 0.1f, 2.8f, 6, Mats.Wood, false);
            g.Cyl(new Vector3(842f, 2.9f, 100f), 3f, 0.1f, 1.1f, 8, Mats.Solid(H("7a3b2a")));
            UseProp("key", "Chief's desk", new Vector3(842f, 0.166f, 100f), new Vector3(1.6f, 0.85f, 0.8f), H("5a3a20"));
            Label("CHIEF'S VILLA", new Vector3(864f, 9.5f, 116f), new Color(1f, 0.8f, 0.5f), 50f, false);
            Car.Build(root, VType.Suv, H("111114"), new Vector3(836f, 0.17f, 125f), 0f, Car.Mode.Parked);
            Car.Build(root, VType.Suv, H("111114"), new Vector3(836f, 0.17f, 131.5f), 0f, Car.Mode.Parked);

            // Vault bunker
            var bk = new Vector3(872f, 0.166f + 1.75f, 40f);
            g.Box(bk, new Vector3(12f, 3.5f, 10f), Mats.Concrete); Col(bk, new Vector3(12f, 3.5f, 10f));
            g.Box(new Vector3(865.9f, 1.4f, 40f), new Vector3(0.25f, 2.6f, 2.2f), Mats.Metal);
            UseProp("vault", "Vault door", new Vector3(864.8f, 0.166f, 40f), new Vector3(0.3f, 0.4f, 0.3f), H("50545e"));
            Label("VAULT", new Vector3(872f, 5f, 40f), new Color(0.8f, 0.85f, 0.9f), 40f, false);

            for (int i = 0; i < 10; i++) Palm(g, new Vector3(R(763f, 818f), 0.166f, R(13f, 66f)));
            for (int i = 0; i < 6; i++) Palm(g, new Vector3(R(832f, 858f), 0.166f, R(84f, 108f)));
            var rr = new System.Random(23);
            for (int i = 0; i < 4; i++)
            {
                var h = Human.Build(root, Looks.Citizen(rr), new Vector3(R(770f, 815f), 0.17f, R(20f, 60f)), "Resident", R(0, 360));
                h.gameObject.AddComponent<Idler>();
            }

            var gr = new System.Random(11);
            MakeGuard(gr, new[] { new Vector3(785, 0.17f, 75), new Vector3(816, 0.17f, 75) }, 1.6f);
            MakeGuard(gr, new[] { new Vector3(825, 0.17f, 20), new Vector3(825, 0.17f, 130) }, 1.8f);
            MakeGuard(gr, new[] { new Vector3(836, 0.17f, 105.5f), new Vector3(886, 0.17f, 105.5f) }, 1.5f);
            MakeGuard(gr, new[] { new Vector3(850, 0.17f, 28), new Vector3(850, 0.17f, 56) }, 1.5f);
        }

        static void MakeGuard(System.Random r, Vector3[] path, float speed)
        {
            var h = Human.Build(root, Looks.Guard(r), path[0], "Guard");
            Tag.Add(h.gameObject, "Guard", 2.2f, new Color(0.65f, 0.75f, 1f), 25f);
            var g = h.gameObject.AddComponent<Guard>(); g.Path = path; g.Speed = speed; g.Init();
        }

        static void UseProp(string id, string name, Vector3 p, Vector3 size, Color col)
        {
            var g = G(Special);
            var c = p + Vector3.up * size.y / 2f;
            g.Box(c, size, Mats.Solid(col)); Col(c, size);
            var o = new GameObject(name); o.transform.SetParent(root, false); o.transform.position = p;
            Tag.Add(o, name, size.y + 0.6f, new Color(1f, 0.9f, 0.5f), 18f);
            var it = o.AddComponent<Interactable>(); it.Kind = Kind.Person; it.Id = id; it.Prompt = "Use the " + name.ToLower();
            Story.Pos[id] = new Vector2(p.x, p.z);
        }

        // ── People in the story ──────────────────────────────────────────────
        static Human Person(string id, string name, Look look, Vector3 p, float yaw, Color tagCol)
        {
            var h = Human.Build(root, look, p, name, yaw);
            h.gameObject.AddComponent<Idler>();
            Tag.Add(h.gameObject, name, 2.2f, tagCol, 30f);
            var it = h.gameObject.AddComponent<Interactable>(); it.Kind = Kind.Person; it.Id = id; it.Prompt = "Talk to " + name;
            Story.Pos[id] = new Vector2(p.x, p.z);
            return h;
        }

        static void StoryPeople()
        {
            Person("mama", "Mama Put", Looks.Mama, new Vector3(14f, 0.165f, 82.05f), 180f, new Color(1f, 0.65f, 0.35f));
            Person("baba", "Baba Ijebu", Looks.Baba, new Vector3(81f, 0.165f, 141.5f), 225f, new Color(0.65f, 0.8f, 1f));
            G(Mushin).Box(new Vector3(83f, 0.8f, 139.8f), new Vector3(1.6f, 1.3f, 0.9f), Mats.Solid(H("2a5fa8")));
            Label("BABA IJEBU · POOLS", new Vector3(83f, 2.6f, 139.8f), new Color(0.65f, 0.8f, 1f), 30f, false);
            Person("tunde", "Tunde (DJ)", Looks.Tunde, new Vector3(262f, 0.02f, 230.3f), 180f, new Color(0.4f, 0.95f, 0.9f));
            Person("plug", "The Plug", Looks.Plug, new Vector3(-308.5f, 0.165f, -141.5f), 135f, new Color(0.6f, 1f, 0.6f));
            var chief = Human.Build(root, Looks.Chief, new Vector3(873f, 0.166f, 108f), "Chief Adeyemi", 200f);
            chief.gameObject.AddComponent<Idler>(); Tag.Add(chief.gameObject, "Chief Adeyemi", 2.2f, new Color(1f, 0.45f, 0.45f), 30f);
        }

        static void Doses()
        {
            var vial = Mats.Glow("vial", new Color(0.35f, 1f, 0.3f, 1f));
            var pool = Mats.Glow("dosepool", new Color(0.35f, 1f, 0.3f, 0.35f), Mats.Radial());
            foreach (var (x, z) in new[] { (40f, 82f), (120f, 156f), (220f, 180f), (300f, 156f), (0f, 7.5f), (-150f, -80f), (-300f, -70f), (-230f, -158f), (225f, -220f), (150f, -230f), (450f, 158f), (680f, 160f), (745f, 60f), (780f, 60f), (850f, 80f), (-75f, 160f) })
            {
                var o = new GameObject("Dose"); o.transform.SetParent(root, false); o.transform.position = new Vector3(x, 0.17f, z);
                var v = new GameObject("vial"); v.transform.SetParent(o.transform, false); v.transform.localScale = new Vector3(0.22f, 0.35f, 0.22f);
                v.AddComponent<MeshFilter>().sharedMesh = Prim.Cyl; v.AddComponent<MeshRenderer>().sharedMaterial = vial;
                var q = new Geo(); q.Ground(pool, new Rect(-1.2f, -1.2f, 2.4f, 2.4f), 0.03f, 2.4f);
                Geo.Attach(o.transform, q.Meshes()[0].mesh, pool, false);
                Tag.Add(o, "Dose", 1.5f, new Color(0.5f, 1f, 0.4f), 14f);
                o.AddComponent<Pickup>();
            }
        }

        static void ParkedCars()
        {
            var types = new[] { VType.Sedan, VType.Sedan, VType.Danfo, VType.Suv, VType.Keke, VType.Okada };
            foreach (var g in new[] { Main, Isl })
            {
                int placed = 0;
                for (int tries = 0; tries < 400 && placed < (g == Main ? 46 : 14); tries++)
                {
                    bool alongX = rng.Next(2) == 0;
                    var lines = alongX ? g.Zs : g.Xs; var cross = alongX ? g.Xs : g.Zs;
                    float line = lines[rng.Next(lines.Length)];
                    if (Grid3.HW(line) < 5f) continue;
                    float along = R(cross[0] + 10f, cross[cross.Length - 1] - 10f);
                    bool nearCross = false; foreach (var c in cross) if (Mathf.Abs(c - along) < 13f) nearCross = true;
                    if (nearCross) continue;
                    float side = rng.Next(2) == 0 ? 1f : -1f;
                    var p = alongX ? new Vector3(along, 0.02f, line + side * 5.9f) : new Vector3(line + side * 5.9f, 0.02f, along);
                    var p2 = new Vector2(p.x, p.z);
                    if (Estate.Contains(p2) || PortYard.Contains(p2) || PartyStreet.Contains(p2) || Vector2.Distance(p2, Start) < 15f) continue;
                    float yaw = alongX ? (side < 0 ? 90f : 270f) : (side > 0 ? 0f : 180f);
                    var t = Pick(types);
                    Car.Build(root, t, VehicleColour(t, rng), p, yaw, Car.Mode.Parked);
                    placed++;
                }
            }
            // One right by the start, so a car is never far away.
            Car.Build(root, VType.Sedan, H("8a1c1c"), new Vector3(5.9f, 0.02f, 96f), 0f, Car.Mode.Parked);
        }
    }

    /// A coloured light pool at the party that pulses on the beat.
    public class BeatLight : MonoBehaviour
    {
        public float Phase; Material m;
        void Start() { m = GetComponent<MeshRenderer>().material; }
        void Update()
        {
            float t = Owambe.Beat + Phase, pulse = 1f - (t - Mathf.Floor(t));
            var c = Color.HSVToRGB(Mathf.Repeat(t * 0.13f + Phase * 0.3f, 1f), 0.8f, 1f); c.a = 0.25f + pulse * 0.45f;
            m.color = c; transform.localScale = Vector3.one * (6f + pulse * 2f);
        }
    }
}
