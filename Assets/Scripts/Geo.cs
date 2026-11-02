using System.Collections.Generic;
using UnityEngine;

// Eko Lockdown: geometry and surfaces, all generated in code.
// Geo merges many boxes, cylinders and balls into one mesh per material, so a whole
// district draws in a handful of calls. That is what keeps it playable on phones.
namespace Eko
{
    public class Geo
    {
        class Part { public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> t = new List<int>(); }
        readonly Dictionary<Material, Part> parts = new Dictionary<Material, Part>();
        Part P(Material m) { if (!parts.TryGetValue(m, out var p)) { p = new Part(); parts[m] = p; } return p; }
        public bool Empty => parts.Count == 0;

        /// a,b,c,d go round the quad in either direction; the winding is fixed to face `outward`.
        public void Quad(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            var p = P(m);
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0) { (b, d) = (d, b); (ub, ud) = (ud, ub); }
            int i = p.v.Count;
            p.v.Add(a); p.v.Add(b); p.v.Add(c); p.v.Add(d);
            var nn = outward.normalized; p.n.Add(nn); p.n.Add(nn); p.n.Add(nn); p.n.Add(nn);
            p.uv.Add(ua); p.uv.Add(ub); p.uv.Add(uc); p.uv.Add(ud);
            p.t.Add(i); p.t.Add(i + 1); p.t.Add(i + 2); p.t.Add(i); p.t.Add(i + 2); p.t.Add(i + 3);
        }

        public void Tri(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            var p = P(m);
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0) (b, c) = (c, b);
            int i = p.v.Count;
            p.v.Add(a); p.v.Add(b); p.v.Add(c);
            var nn = outward.normalized; p.n.Add(nn); p.n.Add(nn); p.n.Add(nn);
            p.uv.Add(Vector2.zero); p.uv.Add(Vector2.right); p.uv.Add(Vector2.up);
            p.t.Add(i); p.t.Add(i + 1); p.t.Add(i + 2);
        }

        /// A flat rectangle on the ground (y), axis-aligned, UVs in metres / tile.
        public void Ground(Material m, Rect r, float y, float tile = 4f)
        {
            Quad(m, new Vector3(r.xMin, y, r.yMin), new Vector3(r.xMax, y, r.yMin), new Vector3(r.xMax, y, r.yMax), new Vector3(r.xMin, y, r.yMax), Vector3.up,
                new Vector2(r.xMin, r.yMin) / tile, new Vector2(r.xMax, r.yMin) / tile, new Vector2(r.xMax, r.yMax) / tile, new Vector2(r.xMin, r.yMax) / tile);
        }

        public void Box(Vector3 c, Vector3 size, Material side, Material top = null, Vector2? tile = null, bool bottom = false) => Box(c, size, Quaternion.identity, side, top, tile, bottom);

        /// tile: metres per texture repeat (x across the face, y up the face).
        public void Box(Vector3 c, Vector3 size, Quaternion q, Material side, Material top = null, Vector2? tile = null, bool bottom = false)
        {
            Vector3 h = size * 0.5f; Vector2 tl = tile ?? new Vector2(4f, 4f);
            Vector3 Pt(float x, float y, float z) => c + q * new Vector3(x * h.x, y * h.y, z * h.z);
            Vector3 R = q * Vector3.right, F = q * Vector3.forward, U = q * Vector3.up;
            Face(side, Pt(-1, -1, 1), Pt(1, -1, 1), Pt(1, 1, 1), Pt(-1, 1, 1), F, size.x / tl.x, size.y / tl.y);
            Face(side, Pt(1, -1, -1), Pt(-1, -1, -1), Pt(-1, 1, -1), Pt(1, 1, -1), -F, size.x / tl.x, size.y / tl.y);
            Face(side, Pt(1, -1, 1), Pt(1, -1, -1), Pt(1, 1, -1), Pt(1, 1, 1), R, size.z / tl.x, size.y / tl.y);
            Face(side, Pt(-1, -1, -1), Pt(-1, -1, 1), Pt(-1, 1, 1), Pt(-1, 1, -1), -R, size.z / tl.x, size.y / tl.y);
            Face(top ?? side, Pt(-1, 1, -1), Pt(1, 1, -1), Pt(1, 1, 1), Pt(-1, 1, 1), U, size.x / tl.x, size.z / tl.x);
            if (bottom) Face(side, Pt(-1, -1, -1), Pt(1, -1, -1), Pt(1, -1, 1), Pt(-1, -1, 1), -U, size.x / tl.x, size.z / tl.x);
        }

        void Face(Material m, Vector3 bl, Vector3 br, Vector3 tr, Vector3 tlp, Vector3 n, float u, float v)
            => Quad(m, bl, br, tr, tlp, n, new Vector2(0, 0), new Vector2(u, 0), new Vector2(u, v), new Vector2(0, v));

        /// Cylinder or cone standing on b. r1 = 0 makes a cone.
        public void Cyl(Vector3 b, float r0, float r1, float h, int seg, Material m, bool caps = true)
        {
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                Vector3 p0 = b + d0 * r0, p1 = b + d1 * r0, q1 = b + d1 * r1 + Vector3.up * h, q0 = b + d0 * r1 + Vector3.up * h;
                float u0 = (float)i / seg, u1 = (float)(i + 1) / seg;
                Quad(m, p0, p1, q1, q0, d0 + d1 + Vector3.up * ((r0 - r1) / Mathf.Max(h, 0.01f)), new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, 1), new Vector2(u0, 1));
                if (caps && r1 > 0.001f) Tri(m, b + Vector3.up * h, q0, q1, Vector3.up);
                if (caps) Tri(m, b, p1, p0, Vector3.down);
            }
        }

        /// Ellipsoid centred on c with radii r.
        public void Ball(Vector3 c, Vector3 r, Material m, int seg = 8, int rings = 6)
        {
            Vector3 S(int i, int j) { float th = j * Mathf.PI / rings, ph = i * Mathf.PI * 2 / seg; return new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)); }
            for (int j = 0; j < rings; j++)
                for (int i = 0; i < seg; i++)
                {
                    Vector3 a = S(i, j), b = S(i + 1, j), cc = S(i + 1, j + 1), d = S(i, j + 1);
                    Quad(m, c + Vector3.Scale(a, r), c + Vector3.Scale(b, r), c + Vector3.Scale(cc, r), c + Vector3.Scale(d, r), a + b + cc + d,
                        Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                }
        }

        public List<(Mesh mesh, Material mat)> Meshes()
        {
            var list = new List<(Mesh, Material)>();
            foreach (var kv in parts)
            {
                var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(kv.Value.v); mesh.SetNormals(kv.Value.n); mesh.SetUVs(0, kv.Value.uv); mesh.SetTriangles(kv.Value.t, 0);
                mesh.RecalculateBounds();
                mesh.UploadMeshData(true); // GPU copy only: nothing reads these back, and phones need the memory
                list.Add((mesh, kv.Key));
            }
            return list;
        }

        public GameObject Build(Transform parent, string name, bool shadows = true)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            foreach (var (mesh, mat) in Meshes()) Attach(root.transform, mesh, mat, shadows);
            return root;
        }

        public static GameObject Attach(Transform parent, Mesh mesh, Material mat, bool shadows = true)
        {
            var go = new GameObject("mesh"); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
    }

    /// Shared unit meshes for the jointed people: each limb is a scaled copy.
    public static class Prim
    {
        static Mesh cube, ball, cyl, flare;
        static Mesh One(System.Action<Geo, Material> f) { var g = new Geo(); var m = Mats.Solid(Color.white); f(g, m); return g.Meshes()[0].mesh; }
        public static Mesh Cube => cube ??= One((g, m) => g.Box(Vector3.zero, Vector3.one, m));
        public static Mesh Ball => ball ??= One((g, m) => g.Ball(Vector3.zero, Vector3.one * 0.5f, m, 10, 7));
        public static Mesh Cyl => cyl ??= One((g, m) => g.Cyl(new Vector3(0, -0.5f, 0), 0.5f, 0.5f, 1f, 10, m));
        public static Mesh Flare => flare ??= One((g, m) => g.Cyl(new Vector3(0, -0.5f, 0), 0.5f, 0.32f, 1f, 10, m));

        static Mesh soft, limb, rock;
        /// A box with well rounded edges, 1 m across: torsos, hips, shoes.
        public static Mesh Soft => soft ??= SuperEllipsoid(0.45f, 0.55f, 16, 12, "soft");
        /// A rounded cylinder, 1 m tall and 1 m across: arms and legs.
        public static Mesh Limb => limb ??= SuperEllipsoid(0.35f, 1f, 12, 12, "limb");
        /// A lumpy boulder for sea walls.
        public static Mesh Rock => rock ??= SuperEllipsoid(0.8f, 0.8f, 8, 6, "rock", 0.18f);

        /// Superellipsoid: e1 shapes the top and bottom (small = flat), e2 the cross-section (1 = round, small = square).
        static Mesh SuperEllipsoid(float e1, float e2, int seg, int rings, string name, float lumps = 0f)
        {
            float P(float v, float e) => Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), e);
            var r = new System.Random(seg * 31 + rings);
            var v3 = new List<Vector3>(); var tris = new List<int>();
            for (int i = 0; i <= rings; i++)
            {
                float lat = -Mathf.PI / 2f + Mathf.PI * i / rings;
                for (int j = 0; j <= seg; j++)
                {
                    float lon = -Mathf.PI + 2f * Mathf.PI * (j % seg) / seg;
                    var p = new Vector3(P(Mathf.Cos(lat), e1) * P(Mathf.Cos(lon), e2), P(Mathf.Sin(lat), e1), P(Mathf.Cos(lat), e1) * P(Mathf.Sin(lon), e2)) * 0.5f;
                    if (lumps > 0f && i > 0 && i < rings && j < seg) p *= 1f + ((float)r.NextDouble() - 0.5f) * lumps;
                    v3.Add(p);
                }
            }
            // Share the seam so lumps and normals match across it.
            for (int i = 0; i <= rings; i++) v3[i * (seg + 1) + seg] = v3[i * (seg + 1)];
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < seg; j++)
                {
                    int a = i * (seg + 1) + j, b = a + seg + 1;
                    tris.Add(a); tris.Add(b); tris.Add(a + 1);
                    tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
                }
            var m = new Mesh { name = name }; m.SetVertices(v3); m.SetTriangles(tris, 0);
            m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
    }

    public static class Mats
    {
        static Material lit;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        public static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }

        static Material Lit
        {
            get
            {
                if (lit == null) { lit = Resources.Load<Material>("EkoLit"); if (lit == null) lit = new Material(Shader.Find("Standard")); }
                return lit;
            }
        }

        public static Material Solid(Color c, float smooth = 0.12f, float metal = 0f)
        {
            string k = "s" + ColorUtility.ToHtmlStringRGBA(c) + smooth.ToString("0.00") + metal.ToString("0.0");
            if (!cache.TryGetValue(k, out var m))
            {
                m = new Material(Lit) { color = c, enableInstancing = true, name = k };
                m.SetFloat("_Glossiness", smooth); m.SetFloat("_Metallic", metal);
                cache[k] = m;
            }
            return m;
        }

        /// Unlit and alpha-blended: lamp bulbs, light pools, vision cones, party lights.
        public static Material Glow(string key, Color c, Texture2D tex = null)
        {
            if (!cache.TryGetValue("g" + key, out var m))
            {
                m = new Material(Shader.Find("Sprites/Default")) { color = c, name = key };
                if (tex != null) m.mainTexture = tex;
                cache["g" + key] = m;
            }
            return m;
        }

        public static Material Tex(string key, System.Func<Texture2D> make, float smooth = 0.08f)
        {
            if (!cache.TryGetValue(key, out var m))
            {
                m = new Material(Lit) { mainTexture = make(), enableInstancing = true, name = key };
                m.SetFloat("_Glossiness", smooth);
                cache[key] = m;
            }
            return m;
        }

        public static Material Asphalt => Tex("asphalt", () => Noise(128, Hex("34353a"), 0.10f, 1, Hex("2a2b2f")));
        public static Material Paving => Tex("paving", () => Tiles(64, Hex("a8a49b"), Hex("8c887f"), 16));
        public static Material Kerb => Solid(Hex("b9b4a8"));
        public static Material Grass => Tex("grass", () => Noise(128, Hex("4d7a34"), 0.18f, 2, Hex("3d6a2a")));
        public static Material Dirt => Tex("dirt", () => Noise(128, Hex("8a6e4e"), 0.16f, 3, Hex("6e563c")));
        public static Material Concrete => Tex("concrete", () => Noise(128, Hex("9a978f"), 0.08f, 4, Hex("85827a")));
        public static Material Sand => Tex("sand", () => Noise(128, Hex("d8c69a"), 0.08f, 5, Hex("c8b488")));
        public static Material Quay => Solid(Hex("6f6c66"));
        /// Lagoon water: a rippled texture that WaterMotion scrolls so the surface moves.
        public static Material Water => Tex("water", () => Ripples(128, Hex("1f5466"), Hex("2f7488"), 3), 0.9f);
        /// Light glinting on the water, scrolled the other way over the top.
        public static Material Glint => Glow("glint", new Color(0.8f, 0.95f, 1f, 0.16f), GlintTex());
        public static Material Foam => Glow("foam", new Color(1f, 1f, 1f, 0.5f), FoamTex());
        public static Material Stone => Solid(Hex("5c5a55"), 0.15f);
        public static Material Zinc => Tex("zinc", () => Corrugated(64, Hex("a3a6a8"), 0.25f));
        public static Material Ribbed(Color c) => Tex("rib" + ColorUtility.ToHtmlStringRGB(c), () => Corrugated(64, c, 0.15f));
        public static Material Rust => Tex("rust", () => Corrugated(64, Hex("8f5a36"), 0.5f));
        public static Material Glass => Solid(Hex("39505c"), 0.92f, 0.35f);
        /// See-through vehicle windows, so you can see who is driving.
        public static Material Window => Glow("window", new Color(0.13f, 0.2f, 0.25f, 0.42f));
        public static Material Dark => Solid(Hex("1d1d20"), 0.3f);
        public static Material Metal => Solid(Hex("6d7177"), 0.55f, 0.6f);
        public static Material Wood => Solid(Hex("6b4a2e"));
        public static Material Trunk => Solid(Hex("6a5038"));
        public static Material Leaf(int i) => Solid(new[] { Hex("3f8a3a"), Hex("2f6f2f"), Hex("4f9a3a"), Hex("5d8f2c") }[i & 3]);
        public static Material Lamp => Glow("lamp", Hex("fff1c8"));
        public static Material Pool => Glow("pool", new Color(1f, 0.82f, 0.5f, 0.1f), Radial());
        public static Material Facade(int style, Color wall) => Tex("f" + style + ColorUtility.ToHtmlStringRGB(wall), () => FacadeTex(style, wall), style == 1 ? 0.6f : 0.08f);

        // ── Procedural textures ──────────────────────────────────────────────
        static Texture2D New(int s) => new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 2 };

        static Texture2D Noise(int s, Color a, float amp, int seed, Color blot)
        {
            var t = New(s); var r = new System.Random(seed); var px = new Color[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = a * (1f - amp + (float)r.NextDouble() * amp * 2f);
            for (int k = 0; k < 14; k++)
            {
                int cx = r.Next(s), cy = r.Next(s), rad = 4 + r.Next(10);
                for (int y = -rad; y <= rad; y++) for (int x = -rad; x <= rad; x++)
                    if (x * x + y * y < rad * rad) { int idx = ((cy + y + s) % s) * s + (cx + x + s) % s; px[idx] = Color.Lerp(px[idx], blot, 0.35f); }
            }
            for (int i = 0; i < px.Length; i++) px[i].a = 1f;
            t.SetPixels(px); t.Apply(); return t;
        }

        static Texture2D Tiles(int s, Color a, Color line, int cell)
        {
            var t = New(s); var px = new Color[s * s]; var r = new System.Random(9);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                px[y * s + x] = (x % cell == 0 || y % cell == 0) ? line : a * (0.94f + (float)r.NextDouble() * 0.12f);
            for (int i = 0; i < px.Length; i++) px[i].a = 1f;
            t.SetPixels(px); t.Apply(); return t;
        }

        static Texture2D Corrugated(int s, Color a, float rust)
        {
            var t = New(s); var px = new Color[s * s]; var r = new System.Random(11);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float rib = 0.82f + 0.18f * Mathf.Abs(Mathf.Sin(x * Mathf.PI / 4f));
                px[y * s + x] = a * rib;
            }
            for (int k = 0; k < (int)(rust * 30); k++)
            {
                int cx = r.Next(s), cy = r.Next(s), rad = 2 + r.Next(6);
                for (int y = -rad; y <= rad; y++) for (int x = -rad; x <= rad; x++)
                    if (x * x + y * y < rad * rad) { int i = ((cy + y + s) % s) * s + (cx + x + s) % s; px[i] = Color.Lerp(px[i], Hex("7a4022"), 0.5f); }
            }
            for (int i = 0; i < px.Length; i++) px[i].a = 1f;
            t.SetPixels(px); t.Apply(); return t;
        }

        /// 4 bays x 4 floors; one texture repeat = 16 m wide, 12.8 m tall.
        /// style 0 homes with burglar bars, 1 glass office, 2 warehouse, 3 shack.
        static Texture2D FacadeTex(int style, Color wall)
        {
            const int s = 128, bay = 32;
            var t = New(s); var px = new Color[s * s];
            var r = new System.Random(style * 7919 + (int)(wall.r * 255) * 31 + (int)(wall.g * 255));
            Color glass = Hex("2c3a44"), frame = Color.Lerp(wall, Color.white, 0.35f), bars = Hex("4a4a4a");
            for (int i = 0; i < px.Length; i++) px[i] = wall * (0.95f + (float)r.NextDouble() * 0.06f);
            for (int fy = 0; fy < 4; fy++)
                for (int bx = 0; bx < 4; bx++)
                {
                    int x0 = bx * bay, y0 = fy * bay;
                    if (style == 1)
                    {
                        Color g = Color.Lerp(Hex("3d6878"), Hex("6a98a8"), (float)r.NextDouble() * 0.5f);
                        for (int y = y0 + 6; y < y0 + bay - 2; y++) for (int x = x0 + 1; x < x0 + bay - 1; x++) px[y * s + x] = g * (0.9f + 0.2f * (y - y0) / bay);
                        for (int y = y0 + 6; y < y0 + bay - 2; y++) px[y * s + x0 + bay / 2] = Hex("2a3a44");
                        continue;
                    }
                    if (style == 2)
                    {
                        for (int y = y0; y < y0 + bay; y++) for (int x = x0; x < x0 + bay; x++) px[y * s + x] *= 0.86f + 0.14f * Mathf.Abs(Mathf.Sin(x * Mathf.PI / 3f));
                        if (fy == 3) for (int y = y0 + 12; y < y0 + 20; y++) for (int x = x0 + 6; x < x0 + 26; x++) px[y * s + x] = glass;
                        continue;
                    }
                    if (style == 3 && r.Next(3) > 0) continue;
                    int wx0 = x0 + 8, wx1 = x0 + 24, wy0 = y0 + 9, wy1 = y0 + 26;
                    bool lit = r.Next(8) == 0; Color curtain = Color.HSVToRGB((float)r.NextDouble(), 0.5f, 0.75f);
                    for (int y = wy0 - 2; y <= wy1 + 2; y++) for (int x = wx0 - 2; x <= wx1 + 2; x++) px[y * s + x] = frame;
                    for (int y = wy0; y <= wy1; y++) for (int x = wx0; x <= wx1; x++)
                    {
                        Color c = lit ? Hex("e8c878") : glass;
                        if (!lit && r.Next(4) == 0 && x < wx0 + 5) c = curtain;
                        px[y * s + x] = c;
                    }
                    if (r.Next(3) > 0) for (int x = wx0; x <= wx1; x += 3) for (int y = wy0; y <= wy1; y++) px[y * s + x] = bars;
                    if (r.Next(4) == 0) for (int y = wy0 - 6; y < wy0 - 2; y++) for (int x = x0 + 3; x < x0 + 9; x++) px[y * s + x] = Hex("d8d8d4"); // AC unit
                }
            for (int i = 0; i < px.Length; i++) px[i].a = 1f;
            t.SetPixels(px); t.Apply(); return t;
        }

        static Texture2D Ripples(int s, Color deep, Color crest, int seed)
        {
            var t = New(s); var px = new Color[s * s]; var r = new System.Random(seed);
            var waves = new List<(float kx, float ky, float ph)>();
            for (int k = 0; k < 6; k++) { int kx = r.Next(1, 5) * (r.Next(2) == 0 ? 1 : -1), ky = r.Next(1, 5); waves.Add((kx, ky, (float)r.NextDouble() * 6.28f)); }
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float v = 0f; foreach (var (kx, ky, ph) in waves) v += Mathf.Sin((kx * x + ky * y) * 2f * Mathf.PI / s + ph);
                v = Mathf.Clamp01(v / 6f + 0.5f);
                px[y * s + x] = Color.Lerp(deep, crest, Mathf.Pow(v, 2.2f));
            }
            for (int i = 0; i < px.Length; i++) px[i].a = 1f;
            t.SetPixels(px); t.Apply(); return t;
        }

        static Texture2D glint;
        static Texture2D GlintTex()
        {
            if (glint != null) return glint;
            const int s = 128; glint = New(s); var px = new Color[s * s]; var r = new System.Random(17);
            for (int i = 0; i < px.Length; i++) px[i] = new Color(1, 1, 1, 0);
            for (int k = 0; k < 220; k++)
            {
                int cx = r.Next(s), cy = r.Next(s), len = 2 + r.Next(6);
                for (int d = -len; d <= len; d++) { int i = (cy % s) * s + (cx + d + s) % s; px[i].a = Mathf.Max(px[i].a, 1f - Mathf.Abs(d) / (float)(len + 1)); }
            }
            glint.SetPixels(px); glint.Apply(); return glint;
        }

        static Texture2D foam;
        static Texture2D FoamTex()
        {
            if (foam != null) return foam;
            const int s = 64; foam = New(s); var px = new Color[s * s]; var r = new System.Random(21);
            for (int i = 0; i < px.Length; i++) px[i] = new Color(1, 1, 1, 0);
            for (int k = 0; k < 60; k++)
            {
                int cx = r.Next(s), cy = r.Next(s), rad = 2 + r.Next(6);
                for (int y = -rad; y <= rad; y++) for (int x = -rad; x <= rad; x++)
                {
                    float d = Mathf.Sqrt(x * x + y * y) / rad; if (d > 1f) continue;
                    int i = ((cy + y + s) % s) * s + (cx + x + s) % s; px[i].a = Mathf.Max(px[i].a, (1f - d) * 0.9f);
                }
            }
            foam.SetPixels(px); foam.Apply(); return foam;
        }

        static Texture2D radial;
        public static Texture2D Radial()
        {
            if (radial != null) return radial;
            const int s = 64; radial = New(s); radial.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s / 2f, s / 2f)) / (s / 2f);
                float a = d < 1 ? (1 - d) * (1 - d) : 0; px[y * s + x] = new Color(1, 1, 1, a);
            }
            radial.SetPixels(px); radial.Apply(); return radial;
        }
    }
}
