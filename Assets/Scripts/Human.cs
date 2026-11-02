using UnityEngine;

// Eko Lockdown: 3D people. Jointed bodies with hair, clothes and faces,
// animated in code: walk, run, crouch, idle, dance, sit.
namespace Eko
{
    public enum HairStyle { Short, Bald, Afro, Braids, Cap, Gele, Locs }

    public class Look
    {
        public Color Skin, Shirt, Pants, Hair, Shoe, Accent;
        public HairStyle Style;
        public bool Glasses, Beard, Vest, Skirt, LongSleeves, Infected, Mask;
    }

    public static class Looks
    {
        public static readonly Color[] Skins =
        {
            new Color(0.36f, 0.23f, 0.15f), new Color(0.45f, 0.29f, 0.19f), new Color(0.55f, 0.36f, 0.24f),
            new Color(0.63f, 0.43f, 0.29f), new Color(0.28f, 0.18f, 0.12f),
        };
        static Color H(string h) => Mats.Hex(h);
        static readonly Color Black = H("15100c"), Grey = H("8d8a85");

        public static Look Dayo => new Look { Skin = Skins[2], Shirt = H("e0a91c"), Pants = H("2c3d5c"), Hair = Black, Shoe = H("e9e5da"), Accent = H("26221f"), Style = HairStyle.Short, Vest = true };
        public static Look Ada => new Look { Skin = Skins[3], Shirt = H("e693b4"), Pants = H("d77fa2"), Hair = Black, Shoe = H("f2f2f2"), Accent = H("ffffff"), Style = HairStyle.Braids };
        public static Look Mama => new Look { Skin = Skins[1], Shirt = H("2f8f5a"), Pants = H("e07b24"), Hair = Black, Shoe = H("6b4226"), Accent = H("f0a030"), Style = HairStyle.Gele, Skirt = true };
        public static Look Baba => new Look { Skin = Skins[4], Shirt = H("3f6fb5"), Pants = H("3f6fb5"), Hair = Grey, Shoe = H("3a2a1a"), Accent = H("ece4d2"), Style = HairStyle.Cap, Beard = true, LongSleeves = true };
        public static Look Tunde => new Look { Skin = Skins[2], Shirt = H("1fa79a"), Pants = H("23232b"), Hair = Black, Shoe = H("d6d6d6"), Accent = H("111111"), Style = HairStyle.Afro, Glasses = true };
        public static Look Plug => new Look { Skin = Skins[0], Shirt = H("1f4d2b"), Pants = H("181818"), Hair = Black, Shoe = H("101010"), Accent = H("0f2e18"), Style = HairStyle.Cap, Glasses = true, Beard = true, LongSleeves = true };
        public static Look Chief => new Look { Skin = Skins[1], Shirt = H("7a1530"), Pants = H("7a1530"), Hair = Grey, Shoe = H("2a1a10"), Accent = H("d4a017"), Style = HairStyle.Bald, Beard = true, Vest = true, LongSleeves = true };
        public static Look Guard(System.Random r) => new Look { Skin = Skins[r.Next(Skins.Length)], Shirt = H("22305a"), Pants = H("1a2240"), Hair = Black, Shoe = H("0e0e0e"), Accent = H("141c36"), Style = HairStyle.Cap, LongSleeves = true };
        public static Look Soldier(System.Random r) => new Look { Skin = Skins[r.Next(Skins.Length)], Shirt = H("4b5a2a"), Pants = H("3e4a22"), Hair = Black, Shoe = H("1b1b12"), Accent = H("3a4620"), Style = HairStyle.Cap, LongSleeves = true };
        public static Look Sule => new Look { Skin = Skins[4], Shirt = H("f0e6d0"), Pants = H("2a2a2a"), Hair = Black, Shoe = H("2a1d14"), Accent = H("8a1a1a"), Style = HairStyle.Bald, Beard = true, Vest = true };
        public static Look Bello => new Look { Skin = Skins[3], Shirt = H("f6f8fa"), Pants = H("2a3a5a"), Hair = Grey, Shoe = H("1a1a1a"), Accent = H("f6f8fa"), Style = HairStyle.Short, Glasses = true, LongSleeves = true, Vest = true };
        public static Look Nurse => new Look { Skin = Skins[2], Shirt = H("7ac8d0"), Pants = H("7ac8d0"), Hair = Black, Shoe = H("f2f2f2"), Accent = H("ffffff"), Style = HairStyle.Braids };

        /// Lagos State Fever Taskforce: navy uniform, hi-vis vest, beret, face mask.
        public static Look Taskforce(System.Random r) => new Look { Skin = Skins[r.Next(Skins.Length)], Shirt = H("1c2536"), Pants = H("14161c"), Hair = Black, Shoe = H("0c0c0c"), Accent = H("f0761a"), Style = HairStyle.Cap, Vest = true, LongSleeves = true, Mask = true };

        /// Someone the plague has taken: ashen skin, filthy clothes, red eyes.
        public static Look Infected(System.Random r)
        {
            var l = Citizen(r);
            l.Skin = Color.Lerp(Skins[r.Next(Skins.Length)], H("6f7a62"), 0.55f);
            l.Shirt = Color.Lerp(l.Shirt, H("3a3428"), 0.6f); l.Pants = Color.Lerp(l.Pants, H("2a261e"), 0.5f);
            l.Accent = Color.Lerp(l.Accent, H("3a3428"), 0.5f);
            l.Glasses = false; l.Infected = true;
            return l;
        }

        public static Look Citizen(System.Random r)
        {
            var styles = new[] { HairStyle.Short, HairStyle.Short, HairStyle.Afro, HairStyle.Braids, HairStyle.Cap, HairStyle.Gele, HairStyle.Locs, HairStyle.Bald };
            var st = styles[r.Next(styles.Length)];
            bool woman = st == HairStyle.Gele || st == HairStyle.Braids;
            float F() => (float)r.NextDouble();
            Color shirt = Color.HSVToRGB(F(), 0.45f + F() * 0.35f, 0.55f + F() * 0.4f);
            Color pants = woman && r.Next(3) > 0 ? Color.HSVToRGB(F(), 0.6f, 0.75f) : Color.HSVToRGB(0.6f + F() * 0.1f, 0.2f + F() * 0.3f, 0.15f + F() * 0.3f);
            return new Look
            {
                Skin = Skins[r.Next(Skins.Length)], Shirt = shirt, Pants = pants, Hair = r.Next(8) == 0 ? Grey : Black,
                Shoe = r.Next(2) == 0 ? H("e6e2d8") : H("2a1d14"), Accent = Color.HSVToRGB(F(), 0.7f, 0.85f),
                Style = st, Skirt = woman && r.Next(3) > 0, Glasses = r.Next(7) == 0, Beard = !woman && r.Next(5) == 0, Vest = r.Next(8) == 0, LongSleeves = r.Next(4) == 0,
            };
        }
    }

    public class Human : MonoBehaviour
    {
        public Transform Hips, Torso, Head, ShL, ShR, ElL, ElR, HipL, HipR, KnL, KnR;
        public float Speed;
        public bool AutoSpeed = true, Crouch, Dancing, Seated, Driving, Shamble, Down;
        public float DancePhase;
        public static readonly System.Collections.Generic.List<Human> All = new System.Collections.Generic.List<Human>();
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        float phase, idleT; Vector3 lastPos;
        const float HipH = 0.95f;

        static Transform Part(Transform parent, Mesh mesh, Color c, Vector3 pos, Vector3 scale, float smooth = 0.15f)
        {
            var go = new GameObject("p"); go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = Mats.Solid(c, smooth);
            return go.transform;
        }
        static Color H2(string hex) => Mats.Hex(hex);
        static Transform Pivot(Transform parent, string name, Vector3 pos) { var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = pos; return t; }

        public static Human Build(Transform parent, Look L, Vector3 pos, string name, float yaw = 0f, float height = 0.96f)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            root.transform.position = pos; root.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var h = root.AddComponent<Human>();
            var body = Pivot(root.transform, "body", Vector3.zero); body.localScale = Vector3.one * height;
            Mesh cube = Prim.Cube, ball = Prim.Ball, cyl = Prim.Cyl;
            Color skin = L.Skin, shirt = L.Shirt, pants = L.Pants;

            h.Hips = Pivot(body, "hips", new Vector3(0, HipH, 0));
            Part(h.Hips, Prim.Soft, pants, new Vector3(0, 0.02f, 0), new Vector3(0.36f, 0.24f, 0.24f));
            if (L.Skirt) Part(h.Hips, Prim.Flare, pants, new Vector3(0, -0.27f, 0), new Vector3(0.52f, 0.56f, 0.42f));

            h.Torso = Pivot(h.Hips, "torso", new Vector3(0, 0.1f, 0));
            // Chest wider than the waist, shoulders rounded off.
            Part(h.Torso, Prim.Soft, shirt, new Vector3(0, 0.17f, 0), new Vector3(0.37f, 0.36f, 0.22f));
            Part(h.Torso, Prim.Soft, shirt, new Vector3(0, 0.36f, 0.005f), new Vector3(0.45f, 0.3f, 0.25f));
            Part(h.Torso, Prim.Soft, Color.Lerp(shirt, Color.black, 0.1f), new Vector3(0, 0.49f, 0), new Vector3(0.5f, 0.12f, 0.24f));
            if (L.Vest)
            {
                Part(h.Torso, Prim.Soft, L.Accent, new Vector3(-0.12f, 0.28f, 0.01f), new Vector3(0.19f, 0.48f, 0.26f));
                Part(h.Torso, Prim.Soft, L.Accent, new Vector3(0.12f, 0.28f, 0.01f), new Vector3(0.19f, 0.48f, 0.26f));
                Part(h.Torso, Prim.Soft, L.Accent, new Vector3(0, 0.28f, -0.1f), new Vector3(0.44f, 0.48f, 0.07f));
            }
            Part(h.Torso, cyl, skin, new Vector3(0, 0.56f, 0), new Vector3(0.11f, 0.1f, 0.11f));
            if (L.Infected)
            {
                // Blood down the front of the shirt.
                Part(h.Torso, cube, H2("5a0f0c"), new Vector3(0.05f, 0.36f, 0.121f), new Vector3(0.2f, 0.26f, 0.01f));
                Part(h.Torso, cube, H2("5a0f0c"), new Vector3(-0.08f, 0.15f, 0.121f), new Vector3(0.1f, 0.14f, 0.01f));
            }

            // Head, face, hair
            h.Head = Pivot(h.Torso, "head", new Vector3(0, 0.6f, 0));
            Part(h.Head, ball, skin, new Vector3(0, 0.13f, 0), new Vector3(0.23f, 0.27f, 0.25f));
            Part(h.Head, ball, Color.Lerp(skin, Color.black, 0.08f), new Vector3(0, 0.1f, 0.122f), new Vector3(0.06f, 0.06f, 0.06f));
            Part(h.Head, ball, Color.Lerp(skin, Color.black, 0.45f), new Vector3(0, 0.045f, 0.112f), new Vector3(0.085f, 0.022f, 0.03f));
            Part(h.Head, ball, skin, new Vector3(0, 0.02f, 0.06f), new Vector3(0.17f, 0.12f, 0.16f)); // jaw
            if (L.Mask) Part(h.Head, Prim.Soft, new Color(0.92f, 0.93f, 0.95f), new Vector3(0, 0.06f, 0.1f), new Vector3(0.19f, 0.11f, 0.08f));
            Part(h.Head, ball, skin, new Vector3(-0.118f, 0.13f, 0), new Vector3(0.04f, 0.07f, 0.05f));
            Part(h.Head, ball, skin, new Vector3(0.118f, 0.13f, 0), new Vector3(0.04f, 0.07f, 0.05f));
            if (L.Glasses) Part(h.Head, cube, new Color(0.05f, 0.05f, 0.06f), new Vector3(0, 0.155f, 0.115f), new Vector3(0.21f, 0.045f, 0.03f), 0.8f);
            else if (L.Infected)
            {
                foreach (var x in new[] { -0.048f, 0.048f })
                {
                    var eye = Part(h.Head, ball, Color.white, new Vector3(x, 0.155f, 0.112f), new Vector3(0.042f, 0.032f, 0.02f));
                    eye.GetComponent<MeshRenderer>().sharedMaterial = Mats.Glow("infectedEye", new Color(1f, 0.12f, 0.05f));
                }
                Part(h.Head, cube, H2("3a0c0a"), new Vector3(0, 0.035f, 0.112f), new Vector3(0.1f, 0.04f, 0.03f));
            }
            else
            {
                Part(h.Head, ball, Color.white, new Vector3(-0.048f, 0.155f, 0.108f), new Vector3(0.045f, 0.035f, 0.02f));
                Part(h.Head, ball, Color.white, new Vector3(0.048f, 0.155f, 0.108f), new Vector3(0.045f, 0.035f, 0.02f));
                Part(h.Head, ball, new Color(0.08f, 0.05f, 0.04f), new Vector3(-0.048f, 0.155f, 0.117f), new Vector3(0.022f, 0.028f, 0.01f));
                Part(h.Head, ball, new Color(0.08f, 0.05f, 0.04f), new Vector3(0.048f, 0.155f, 0.117f), new Vector3(0.022f, 0.028f, 0.01f));
                Part(h.Head, cube, L.Hair, new Vector3(-0.05f, 0.19f, 0.112f), new Vector3(0.055f, 0.012f, 0.02f));
                Part(h.Head, cube, L.Hair, new Vector3(0.05f, 0.19f, 0.112f), new Vector3(0.055f, 0.012f, 0.02f));
            }
            if (L.Beard) Part(h.Head, ball, L.Hair, new Vector3(0, 0.05f, 0.035f), new Vector3(0.21f, 0.13f, 0.2f));
            Hair(h.Head, L);

            // Arms
            h.ShL = Pivot(h.Torso, "shL", new Vector3(-0.27f, 0.47f, 0));
            h.ShR = Pivot(h.Torso, "shR", new Vector3(0.27f, 0.47f, 0));
            h.ElL = Arm(h.ShL, L); h.ElR = Arm(h.ShR, L);

            // Legs
            h.HipL = Pivot(h.Hips, "hipL", new Vector3(-0.1f, -0.05f, 0));
            h.HipR = Pivot(h.Hips, "hipR", new Vector3(0.1f, -0.05f, 0));
            h.KnL = Leg(h.HipL, L); h.KnR = Leg(h.HipR, L);

            h.lastPos = pos; h.Shamble = L.Infected;
            return h;
        }

        static Transform Arm(Transform sh, Look L)
        {
            Part(sh, Prim.Ball, L.Shirt, new Vector3(0, -0.02f, 0), new Vector3(0.15f, 0.14f, 0.15f));
            Part(sh, Prim.Limb, L.Shirt, new Vector3(0, -0.15f, 0), new Vector3(0.13f, 0.34f, 0.13f));
            var el = Pivot(sh, "elbow", new Vector3(0, -0.29f, 0));
            Part(el, Prim.Limb, L.LongSleeves ? L.Shirt : L.Skin, new Vector3(0, -0.13f, 0), new Vector3(0.11f, 0.3f, 0.11f));
            Part(el, Prim.Ball, L.Skin, new Vector3(0, -0.29f, 0.01f), new Vector3(0.09f, 0.11f, 0.075f));
            return el;
        }

        static Transform Leg(Transform hip, Look L)
        {
            Color upper = L.Skirt ? L.Skin : L.Pants;
            Part(hip, Prim.Limb, upper, new Vector3(0, -0.21f, 0), new Vector3(0.17f, 0.5f, 0.18f));
            var kn = Pivot(hip, "knee", new Vector3(0, -0.43f, 0));
            Part(kn, Prim.Limb, L.Skirt ? L.Skin : L.Pants, new Vector3(0, -0.2f, 0), new Vector3(0.14f, 0.45f, 0.15f));
            Part(kn, Prim.Soft, L.Shoe, new Vector3(0, -0.42f, 0.045f), new Vector3(0.14f, 0.1f, 0.28f));
            return kn;
        }

        static void Hair(Transform head, Look L)
        {
            Color c = L.Hair;
            switch (L.Style)
            {
                case HairStyle.Bald: break;
                case HairStyle.Afro: Part(head, Prim.Ball, c, new Vector3(0, 0.2f, -0.02f), new Vector3(0.36f, 0.33f, 0.36f)); break;
                case HairStyle.Gele:
                    Part(head, Prim.Ball, L.Accent, new Vector3(0, 0.27f, -0.01f), new Vector3(0.36f, 0.2f, 0.33f));
                    Part(head, Prim.Cube, Color.Lerp(L.Accent, Color.white, 0.15f), new Vector3(0, 0.34f, -0.08f), new Vector3(0.34f, 0.15f, 0.05f));
                    Part(head, Prim.Ball, Color.Lerp(L.Accent, Color.black, 0.15f), new Vector3(0.12f, 0.32f, 0.04f), new Vector3(0.1f, 0.08f, 0.08f));
                    break;
                default:
                    Part(head, Prim.Ball, c, new Vector3(0, 0.17f, -0.012f), new Vector3(0.245f, 0.21f, 0.265f));
                    if (L.Style == HairStyle.Braids) Part(head, Prim.Soft, c, new Vector3(0, 0.0f, -0.11f), new Vector3(0.22f, 0.36f, 0.08f));
                    if (L.Style == HairStyle.Locs) for (int i = -2; i <= 2; i++) Part(head, Prim.Limb, c, new Vector3(i * 0.045f, 0.0f, -0.11f + Mathf.Abs(i) * 0.02f), new Vector3(0.04f, 0.3f, 0.04f));
                    if (L.Style == HairStyle.Cap)
                    {
                        Part(head, Prim.Cyl, L.Accent, new Vector3(0, 0.25f, -0.005f), new Vector3(0.255f, 0.09f, 0.27f));
                        Part(head, Prim.Cube, Color.Lerp(L.Accent, Color.black, 0.2f), new Vector3(0, 0.215f, 0.15f), new Vector3(0.21f, 0.02f, 0.12f));
                    }
                    break;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (AutoSpeed)
            {
                var p = transform.position;
                float inst = new Vector2(p.x - lastPos.x, p.z - lastPos.z).magnitude / Mathf.Max(dt, 1e-4f);
                Speed = Mathf.Lerp(Speed, inst, Mathf.Clamp01(10f * dt)); lastPos = p;
            }
            if (Down) { Fallen(); return; }
            if (Seated) { Sit(); return; }
            if (Dancing) { Dance(); return; }

            float k = Mathf.Clamp01(Speed / 4f), run = Mathf.Clamp01((Speed - 4.6f) / 3f);
            phase += dt * (Speed * 2.0f);
            float swing = 30f * k + run * 22f;
            float s = Mathf.Sin(phase);
            float crouchHip = Crouch ? 55f : 0f;
            HipL.localRotation = Quaternion.Euler(-s * swing - crouchHip, 0, 0);
            HipR.localRotation = Quaternion.Euler(s * swing - crouchHip, 0, 0);
            KnL.localRotation = Quaternion.Euler(Mathf.Max(0f, Mathf.Sin(phase + 1.3f)) * swing * 1.5f + (Crouch ? 85f : 0f), 0, 0);
            KnR.localRotation = Quaternion.Euler(Mathf.Max(0f, Mathf.Sin(phase + 1.3f + Mathf.PI)) * swing * 1.5f + (Crouch ? 85f : 0f), 0, 0);
            ShL.localRotation = Quaternion.Euler(s * swing * 0.9f, 0, -5f);
            ShR.localRotation = Quaternion.Euler(-s * swing * 0.9f, 0, 5f);
            ElL.localRotation = Quaternion.Euler(-(12f + run * 70f + k * 10f), 0, 0);
            ElR.localRotation = Quaternion.Euler(-(12f + run * 70f + k * 10f), 0, 0);
            Hips.localPosition = new Vector3(0, HipH - (Crouch ? 0.33f : 0f) + Mathf.Abs(Mathf.Cos(phase)) * 0.045f * k, 0);
            Torso.localRotation = Quaternion.Euler(run * 14f + (Crouch ? 25f : 0f), Mathf.Sin(phase) * 4f * k, 0);

            if (Shamble)
            {
                // Arms reaching, head lolling, a dragging limp.
                float sway = Mathf.Sin(phase * 0.5f + DancePhase);
                ShL.localRotation = Quaternion.Euler(-78f + s * 10f, 0, -6f); ShR.localRotation = Quaternion.Euler(-72f - s * 10f, 0, 6f);
                ElL.localRotation = Quaternion.Euler(-12f, 0, 0); ElR.localRotation = Quaternion.Euler(-18f, 0, 0);
                Torso.localRotation = Quaternion.Euler(16f + run * 10f, sway * 6f, sway * 7f);
                Head.localRotation = Quaternion.Euler(18f, Mathf.Sin(phase * 0.3f) * 15f, 22f + sway * 10f);
                HipR.localRotation = Quaternion.Euler(s * swing * 0.6f, 0, 0);
                Hips.localPosition = new Vector3(0, HipH - 0.04f + Mathf.Max(0f, s) * 0.06f * k, 0);
                return;
            }

            idleT += dt;
            if (k < 0.05f)
            {
                Torso.localScale = new Vector3(1f, 1f + Mathf.Sin(idleT * 2.2f) * 0.012f, 1f);
                Head.localRotation = Quaternion.Euler(0, Mathf.Sin(idleT * 0.37f + DancePhase) * 25f, 0);
            }
            else { Torso.localScale = Vector3.one; Head.localRotation = Quaternion.Slerp(Head.localRotation, Quaternion.identity, 6f * dt); }
        }

        void Dance()
        {
            float t = Owambe.Beat + DancePhase, b = t * Mathf.PI;
            Hips.localPosition = new Vector3(Mathf.Sin(b * 0.5f) * 0.05f, HipH - 0.05f + Mathf.Abs(Mathf.Sin(b)) * 0.07f, 0);
            Hips.localRotation = Quaternion.Euler(0, Mathf.Sin(b * 0.5f) * 20f, Mathf.Sin(b * 0.5f) * 6f);
            Torso.localRotation = Quaternion.Euler(10f + Mathf.Sin(b) * 6f, 0, -Mathf.Sin(b * 0.5f) * 8f);
            HipL.localRotation = Quaternion.Euler(-10f - Mathf.Max(0, Mathf.Sin(b)) * 20f, 0, 0);
            HipR.localRotation = Quaternion.Euler(-10f - Mathf.Max(0, -Mathf.Sin(b)) * 20f, 0, 0);
            KnL.localRotation = Quaternion.Euler(25f + Mathf.Max(0, Mathf.Sin(b)) * 30f, 0, 0);
            KnR.localRotation = Quaternion.Euler(25f + Mathf.Max(0, -Mathf.Sin(b)) * 30f, 0, 0);
            bool up = Mathf.FloorToInt(t / 4f) % 2 == 0;
            ShL.localRotation = Quaternion.Euler(up ? -150f + Mathf.Sin(b) * 25f : -40f + Mathf.Sin(b) * 30f, 0, -15f);
            ShR.localRotation = Quaternion.Euler(up ? -150f - Mathf.Sin(b) * 25f : -40f - Mathf.Sin(b) * 30f, 0, 15f);
            ElL.localRotation = Quaternion.Euler(-40f, 0, 0); ElR.localRotation = Quaternion.Euler(-40f, 0, 0);
            Head.localRotation = Quaternion.Euler(Mathf.Sin(b) * 8f, Mathf.Sin(b * 0.25f) * 20f, 0);
        }

        /// Knocked flat on their back.
        void Fallen()
        {
            Hips.localPosition = new Vector3(0, 0.18f, 0);
            Hips.localRotation = Quaternion.Euler(-90f, 0, 0);
            HipL.localRotation = Quaternion.Euler(-8f, 0, -10f); HipR.localRotation = Quaternion.Euler(-4f, 0, 12f);
            KnL.localRotation = Quaternion.Euler(20f, 0, 0); KnR.localRotation = Quaternion.identity;
            ShL.localRotation = Quaternion.Euler(-160f, 0, -30f); ShR.localRotation = Quaternion.Euler(-20f, 0, 40f);
            ElL.localRotation = Quaternion.identity; ElR.localRotation = Quaternion.identity;
            Torso.localRotation = Quaternion.identity; Head.localRotation = Quaternion.Euler(0, 40f, 0);
        }

        void Sit()
        {
            Hips.localPosition = new Vector3(0, HipH - 0.15f, 0);
            HipL.localRotation = Quaternion.Euler(-80f, 0, -6f); HipR.localRotation = Quaternion.Euler(-80f, 0, 6f);
            // At a wheel the legs reach forward to the pedals instead of dropping through the floor.
            float knee = Driving ? 22f : 75f;
            KnL.localRotation = Quaternion.Euler(knee, 0, 0); KnR.localRotation = Quaternion.Euler(knee, 0, 0);
            ShL.localRotation = Quaternion.Euler(-55f, 0, -12f); ShR.localRotation = Quaternion.Euler(-55f, 0, 12f);
            ElL.localRotation = Quaternion.Euler(-20f, 0, 0); ElR.localRotation = Quaternion.Euler(-20f, 0, 0);
            Torso.localRotation = Quaternion.Euler(12f, 0, 0);
        }
    }
}
