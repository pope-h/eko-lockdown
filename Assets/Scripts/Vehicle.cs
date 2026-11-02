using System.Collections.Generic;
using UnityEngine;

// Eko Lockdown: vehicles. Built from code; each one can be parked, city traffic, or driven by you.
namespace Eko
{
    public enum VType { Danfo, Sedan, Suv, Keke, Okada, Truck, Jeep }

    public class Car : MonoBehaviour
    {
        public enum Mode { Parked, Traffic, Player }
        public static readonly List<Car> All = new List<Car>();

        public VType Type; public Mode State; public float Len, Width, MaxSpeed;
        public string Label;
        readonly List<Transform> wheels = new List<Transform>();
        float wheelR = 0.35f, spin, speed, yaw;
        BoxCollider box; Rigidbody rb;
        AudioSource engine; float hornWait;

        // Traffic state: driving along one grid line, in the right-hand lane.
        public Grid3 G; Vector3 dir; float line; Vector3 logical;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        static readonly Dictionary<string, List<(Mesh, Material)>> bodies = new Dictionary<string, List<(Mesh, Material)>>();

        public static Car Build(Transform parent, VType t, Color body, Vector3 pos, float yaw, Mode mode)
        {
            var go = new GameObject(t.ToString()); go.transform.SetParent(parent, false);
            go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var car = go.AddComponent<Car>(); car.Type = t; car.yaw = yaw;
            string key = t + ColorUtility.ToHtmlStringRGB(body);
            if (!bodies.TryGetValue(key, out var meshes)) { var g = new Geo(); Body(g, t, body); meshes = g.Meshes(); bodies[key] = meshes; }
            foreach (var (m, mat) in meshes) Geo.Attach(go.transform, m, mat);

            Vector3 size; float r; Vector3[] w;
            switch (t)
            {
                case VType.Danfo: size = new Vector3(2.0f, 2.1f, 4.9f); r = 0.4f; w = new[] { new Vector3(-0.95f, 0, 1.6f), new Vector3(0.95f, 0, 1.6f), new Vector3(-0.95f, 0, -1.6f), new Vector3(0.95f, 0, -1.6f) }; car.MaxSpeed = 20f; car.Label = "Danfo"; break;
                case VType.Keke: size = new Vector3(1.35f, 1.8f, 2.7f); r = 0.3f; w = new[] { new Vector3(0, 0, 1.05f), new Vector3(-0.6f, 0, -0.8f), new Vector3(0.6f, 0, -0.8f) }; car.MaxSpeed = 13f; car.Label = "Keke"; break;
                case VType.Okada: size = new Vector3(0.7f, 1.4f, 2.0f); r = 0.33f; w = new[] { new Vector3(0, 0, 0.72f), new Vector3(0, 0, -0.72f) }; car.MaxSpeed = 22f; car.Label = "Okada"; break;
                case VType.Truck: size = new Vector3(2.5f, 3.4f, 7.6f); r = 0.5f; w = new[] { new Vector3(-1.15f, 0, 2.8f), new Vector3(1.15f, 0, 2.8f), new Vector3(-1.15f, 0, -1.4f), new Vector3(1.15f, 0, -1.4f), new Vector3(-1.15f, 0, -2.6f), new Vector3(1.15f, 0, -2.6f) }; car.MaxSpeed = 15f; car.Label = "Truck"; break;
                case VType.Suv: size = new Vector3(1.95f, 1.8f, 4.7f); r = 0.42f; w = new[] { new Vector3(-0.92f, 0, 1.5f), new Vector3(0.92f, 0, 1.5f), new Vector3(-0.92f, 0, -1.5f), new Vector3(0.92f, 0, -1.5f) }; car.MaxSpeed = 26f; car.Label = "SUV"; break;
                case VType.Jeep: size = new Vector3(1.9f, 2.0f, 4.3f); r = 0.42f; w = new[] { new Vector3(-0.9f, 0, 1.35f), new Vector3(0.9f, 0, 1.35f), new Vector3(-0.9f, 0, -1.35f), new Vector3(0.9f, 0, -1.35f) }; car.MaxSpeed = 22f; car.Label = "Army jeep"; break;
                default: size = new Vector3(1.8f, 1.45f, 4.4f); r = 0.35f; w = new[] { new Vector3(-0.88f, 0, 1.4f), new Vector3(0.88f, 0, 1.4f), new Vector3(-0.88f, 0, -1.4f), new Vector3(0.88f, 0, -1.4f) }; car.MaxSpeed = 24f; car.Label = "Car"; break;
            }
            car.Len = size.z; car.Width = size.x; car.wheelR = r;
            foreach (var wp in w)
            {
                var piv = new GameObject("wheel").transform; piv.SetParent(go.transform, false); piv.localPosition = wp + Vector3.up * r;
                var tyre = new GameObject("tyre"); tyre.transform.SetParent(piv, false); tyre.transform.localRotation = Quaternion.Euler(0, 0, 90);
                float wd = t == VType.Okada ? 0.12f : 0.26f;
                tyre.transform.localScale = new Vector3(r * 2f, wd, r * 2f);
                tyre.AddComponent<MeshFilter>().sharedMesh = Prim.Cyl; tyre.AddComponent<MeshRenderer>().sharedMaterial = Mats.Solid(Mats.Hex("161618"), 0.2f);
                var hub = new GameObject("hub"); hub.transform.SetParent(piv, false); hub.transform.localRotation = Quaternion.Euler(0, 0, 90);
                hub.transform.localScale = new Vector3(r * 1.0f, wd + 0.02f, r * 1.0f);
                hub.AddComponent<MeshFilter>().sharedMesh = Prim.Cyl; hub.AddComponent<MeshRenderer>().sharedMaterial = Mats.Metal;
                car.wheels.Add(piv);
            }
            if (mode == Mode.Traffic) car.SeatDriver(Looks.Citizen(new System.Random(go.GetInstanceID())));

            // Collider sits above kerb height so the car rides over kerbs and is stopped by walls.
            car.box = go.AddComponent<BoxCollider>();
            car.box.center = new Vector3(0, 0.35f + (size.y - 0.35f) / 2f, 0); car.box.size = new Vector3(size.x, size.y - 0.35f, size.z);
            // No interpolation for AI cars: they are moved through the transform, and an interpolating
            // body would keep pulling them back to the last physics pose.
            car.rb = go.AddComponent<Rigidbody>(); car.rb.isKinematic = true; car.rb.interpolation = RigidbodyInterpolation.None;
            // The engine: a 3D loop that only runs when the car is moving near you.
            car.engine = go.AddComponent<AudioSource>(); car.engine.clip = Sounds.Engine(t); car.engine.loop = true;
            car.engine.spatialBlend = 1f; car.engine.rolloffMode = AudioRolloffMode.Linear; car.engine.minDistance = 3f; car.engine.maxDistance = 55f;
            car.engine.dopplerLevel = 0.6f; car.engine.volume = 0f; car.hornWait = Random.Range(2f, 12f);
            car.State = mode;
            return car;
        }

        /// Where a driver's feet go, relative to the vehicle. Nigeria drives on the right, so the wheel is on the left.
        public Vector3 Seat
        {
            get
            {
                switch (Type)
                {
                    case VType.Okada: return new Vector3(0f, -0.05f, -0.15f);
                    case VType.Keke: return new Vector3(0f, -0.05f, 0.35f);
                    case VType.Danfo: return new Vector3(-0.5f, 0.3f, 1.45f);
                    case VType.Truck: return new Vector3(-0.55f, 0.85f, 3.05f);
                    case VType.Suv: return new Vector3(-0.45f, 0.06f, 0.15f);
                    case VType.Jeep: return new Vector3(-0.45f, 0.1f, 0.2f);
                    default: return new Vector3(-0.4f, -0.08f, 0.05f);
                }
            }
        }

        public Human Driver;

        void SeatDriver(Look look)
        {
            Driver = Human.Build(transform, look, transform.position, "driver", transform.eulerAngles.y);
            Driver.transform.localPosition = Seat; Driver.transform.localRotation = Quaternion.identity;
            Driver.Seated = true; Driver.Driving = true; Driver.AutoSpeed = false;
        }

        /// The driver climbs out on the left and runs. Returns them, now on foot, or null if nobody was driving.
        public Human Eject()
        {
            var h = Driver; Driver = null;
            if (h == null) return null;
            h.transform.SetParent(transform.parent, true);
            h.transform.position = transform.position - transform.right * (Width / 2f + 0.7f) + Vector3.up * 0.1f;
            h.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y - 90f, 0);
            h.Seated = false; h.Driving = false; h.AutoSpeed = true;
            return h;
        }

        static void Body(Geo g, VType t, Color c)
        {
            Material b = Mats.Solid(c, 0.55f, 0.2f), bd = Mats.Solid(Color.Lerp(c, Color.black, 0.3f), 0.4f), gl = Mats.Glass, dk = Mats.Dark;
            Material head = Mats.Glow("head", Mats.Hex("fff6d8")), tail = Mats.Glow("tail", Mats.Hex("d4221c")), chrome = Mats.Metal;
            switch (t)
            {
                case VType.Danfo:
                    g.Box(new Vector3(0, 0.95f, 0), new Vector3(2.0f, 1.0f, 4.9f), b);
                    Cabin(g, b, new Vector3(0, 1.75f, -0.15f), new Vector3(1.96f, 0.62f, 4.4f));
                    g.Box(new Vector3(0, 1.15f, 0), new Vector3(2.02f, 0.13f, 4.92f), dk);
                    g.Box(new Vector3(0, 0.82f, 0), new Vector3(2.02f, 0.08f, 4.92f), dk);
                    g.Box(new Vector3(0, 2.1f, -0.15f), new Vector3(1.9f, 0.07f, 4.3f), b);
                    g.Box(new Vector3(0, 2.28f, -0.6f), new Vector3(1.5f, 0.3f, 2.0f), Mats.Solid(Mats.Hex("6b4a2e")));
                    g.Box(new Vector3(0, 2.2f, 0.9f), new Vector3(0.9f, 0.2f, 0.9f), Mats.Solid(Mats.Hex("a03a2a")));
                    g.Box(new Vector3(0, 0.55f, 2.47f), new Vector3(2.0f, 0.22f, 0.1f), chrome);
                    g.Box(new Vector3(0, 0.55f, -2.47f), new Vector3(2.0f, 0.22f, 0.1f), chrome);
                    Lights(g, head, tail, 0.75f, 0.95f, 2.46f);
                    break;
                case VType.Keke:
                    g.Box(new Vector3(0, 0.6f, -0.3f), new Vector3(1.3f, 0.55f, 1.9f), b);
                    g.Box(new Vector3(0, 0.75f, 0.95f), new Vector3(0.7f, 0.75f, 0.7f), b);
                    g.Box(new Vector3(0, 1.35f, 0.85f), new Vector3(0.75f, 0.55f, 0.08f), gl);
                    g.Box(new Vector3(0, 1.72f, -0.2f), new Vector3(1.38f, 0.08f, 2.3f), dk);
                    foreach (var p in new[] { new Vector3(-0.62f, 1.25f, 0.85f), new Vector3(0.62f, 1.25f, 0.85f), new Vector3(-0.62f, 1.25f, -1.2f), new Vector3(0.62f, 1.25f, -1.2f) })
                        g.Box(p, new Vector3(0.06f, 0.95f, 0.06f), dk);
                    g.Box(new Vector3(0, 1.0f, -0.6f), new Vector3(1.2f, 0.12f, 0.9f), Mats.Solid(Mats.Hex("2a2a2a")));
                    g.Box(new Vector3(0, 1.3f, -1.05f), new Vector3(1.2f, 0.5f, 0.1f), Mats.Solid(Mats.Hex("2a2a2a")));
                    Lights(g, head, tail, 0.2f, 0.85f, 1.31f);
                    break;
                case VType.Okada:
                    g.Box(new Vector3(0, 0.6f, 0), new Vector3(0.22f, 0.25f, 1.3f), b);
                    g.Box(new Vector3(0, 0.88f, 0.3f), new Vector3(0.34f, 0.24f, 0.5f), b);
                    g.Box(new Vector3(0, 0.88f, -0.25f), new Vector3(0.3f, 0.1f, 0.75f), dk);
                    g.Box(new Vector3(0, 0.95f, 0.66f), new Vector3(0.06f, 0.55f, 0.06f), chrome);
                    g.Box(new Vector3(0, 1.2f, 0.66f), new Vector3(0.7f, 0.04f, 0.04f), dk);
                    g.Box(new Vector3(0, 0.45f, -0.3f), new Vector3(0.12f, 0.12f, 0.7f), chrome);
                    g.Box(new Vector3(0, 0.95f, 0.78f), new Vector3(0.18f, 0.14f, 0.08f), head);
                    break;
                case VType.Truck:
                    g.Box(new Vector3(0, 0.75f, 0), new Vector3(2.3f, 0.4f, 7.6f), dk);
                    var cab = Mats.Solid(Mats.Hex("e8e6e0"), 0.4f);
                    g.Box(new Vector3(0, 1.3f, 2.85f), new Vector3(2.4f, 1.1f, 1.8f), cab);
                    Cabin(g, cab, new Vector3(0, 2.25f, 2.85f), new Vector3(2.4f, 0.8f, 1.8f));
                    g.Box(new Vector3(0, 2.05f, -0.9f), new Vector3(2.5f, 2.6f, 5.6f), b);
                    for (float z = -3.5f; z <= 1.7f; z += 0.65f) g.Box(new Vector3(0, 2.05f, z), new Vector3(2.54f, 2.5f, 0.06f), bd);
                    g.Box(new Vector3(0, 0.6f, 3.8f), new Vector3(2.4f, 0.3f, 0.12f), chrome);
                    Lights(g, head, tail, 0.9f, 0.9f, 3.8f);
                    break;
                case VType.Jeep:
                    g.Box(new Vector3(0, 0.9f, 0), new Vector3(1.9f, 0.8f, 4.3f), b);
                    g.Box(new Vector3(0, 1.65f, -0.5f), new Vector3(1.8f, 0.75f, 2.6f), Mats.Solid(Color.Lerp(c, Color.black, 0.25f)));
                    g.Box(new Vector3(0, 1.6f, 0.95f), new Vector3(1.7f, 0.55f, 0.08f), gl);
                    g.Box(new Vector3(0, 1.1f, -2.2f), new Vector3(0.7f, 0.7f, 0.2f), dk);
                    Lights(g, head, tail, 0.65f, 0.95f, 2.16f);
                    break;
                default: // Sedan and SUV
                    bool suv = t == VType.Suv;
                    float lo = suv ? 0.95f : 0.72f, loH = suv ? 0.8f : 0.6f;
                    g.Box(new Vector3(0, lo, 0), new Vector3(suv ? 1.95f : 1.8f, loH, suv ? 4.7f : 4.4f), b);
                    Cabin(g, b, new Vector3(0, lo + loH / 2f + 0.3f, suv ? -0.3f : -0.2f), new Vector3(suv ? 1.8f : 1.6f, 0.6f, suv ? 3.2f : 2.3f));
                    g.Box(new Vector3(0, lo - loH / 2f + 0.05f, 0), new Vector3(suv ? 1.97f : 1.82f, 0.1f, suv ? 4.72f : 4.42f), dk);
                    Lights(g, head, tail, 0.6f, lo + 0.1f, suv ? 2.36f : 2.21f);
                    break;
            }
        }

        /// A roof on four pillars with see-through glass between them.
        static void Cabin(Geo g, Material paint, Vector3 c, Vector3 s)
        {
            float roof = 0.08f, px = s.x / 2f - 0.05f, pz = s.z / 2f - 0.06f;
            g.Box(c + Vector3.up * (s.y / 2f - roof / 2f), new Vector3(s.x, roof, s.z), paint);
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 0f, 1f })
                    g.Box(c + new Vector3(sx * px, -roof / 2f, sz * pz), new Vector3(0.1f, s.y - roof, sz == 0f ? 0.12f : 0.14f), paint);
            g.Box(c + Vector3.down * (roof / 2f), new Vector3(s.x - 0.02f, s.y - roof, s.z - 0.02f), Mats.Window);
        }

        static void Lights(Geo g, Material head, Material tail, float x, float y, float z)
        {
            g.Box(new Vector3(-x, y, z), new Vector3(0.32f, 0.16f, 0.05f), head); g.Box(new Vector3(x, y, z), new Vector3(0.32f, 0.16f, 0.05f), head);
            g.Box(new Vector3(-x, y, -z), new Vector3(0.28f, 0.14f, 0.05f), tail); g.Box(new Vector3(x, y, -z), new Vector3(0.28f, 0.14f, 0.05f), tail);
        }

        // ── Traffic ──────────────────────────────────────────────────────────
        static float Lane(float c) => Grid3.HW(c) > 5f ? 3.6f : 2.2f;

        /// Drop this car onto a grid line, facing d (one of ±X, ±Z).
        public void PlaceOn(Grid3 g, Vector3 d, float lineCoord, float along)
        {
            G = g; dir = d; line = lineCoord; State = Mode.Traffic;
            speed = MaxSpeed * 0.5f;
            logical = PosFor(along);
            transform.position = logical; yaw = Quaternion.LookRotation(dir).eulerAngles.y; transform.rotation = Quaternion.Euler(0, yaw, 0);
        }

        Vector3 PosFor(float along)
        {
            Vector3 right = new Vector3(dir.z, 0, -dir.x);
            Vector3 p = dir.x != 0 ? new Vector3(along, 0, line) : new Vector3(line, 0, along);
            return p + right * Lane(line) + Vector3.up * 0.02f;
        }

        float Along => dir.x != 0 ? logical.x : logical.z;
        float[] Cross => dir.x != 0 ? G.Xs : G.Zs;
        float[] Parallel => dir.x != 0 ? G.Zs : G.Xs;

        void TrafficTick(float dt)
        {
            // Brake for anything ahead: you, other cars, walls.
            Vector3 fwd = transform.forward;
            Vector3 o = transform.position + Vector3.up * 0.9f + fwd * (Len / 2f + 0.3f);
            float look = 3f + speed * 0.7f;
            bool blocked = Physics.SphereCast(o, 0.9f, fwd, out _, look, ~0, QueryTriggerInteraction.Ignore);
            if (!blocked && Player.I != null && Player.I.Driving == null)
            {
                Vector3 to = Player.I.transform.position - transform.position;
                if (Vector3.Dot(to, fwd) > 0 && Vector3.Dot(to, fwd) < look + Len / 2f && Mathf.Abs(Vector3.Dot(to, transform.right)) < 1.6f) blocked = true;
            }
            speed = Mathf.MoveTowards(speed, blocked ? 0f : MaxSpeed * 0.55f, (blocked ? 18f : 3f) * dt);
            Stuck = blocked && speed < 0.5f ? Stuck + dt : 0f;

            float a0 = Along, a1 = a0 + Sign() * speed * dt;
            // Crossing an intersection: carry on, turn, or turn back at the edge of the map.
            foreach (var c in Cross)
            {
                if ((a0 - c) * (a1 - c) > 0 || Mathf.Approximately(a0, c)) continue;
                var opts = new List<Vector3>();
                Vector3 l = new Vector3(-dir.z, 0, dir.x), r = new Vector3(dir.z, 0, -dir.x);
                if (CanDrive(dir, line, c)) { opts.Add(dir); opts.Add(dir); opts.Add(dir); }
                if (CanTurnOnto(c, l)) opts.Add(l);
                if (CanTurnOnto(c, r)) opts.Add(r);
                Vector3 nd = opts.Count > 0 ? opts[Random.Range(0, opts.Count)] : -dir;
                if (nd == -dir)
                {
                    // Dead end: swing round into the other lane of the same road.
                    dir = nd; logical = PosFor(c);
                    return;
                }
                if (nd != dir)
                {
                    float oldLine = line;
                    dir = nd; line = c; logical = PosFor(oldLine);
                    return;
                }
                break;
            }
            logical = PosFor(a1);
        }

        float Sign() => dir.x + dir.z;

        bool CanDrive(Vector3 d, float lineCoord, float at)
        {
            var cross = d.x != 0 ? G.Xs : G.Zs;
            float next = at + (d.x + d.z) * 75f;
            return next >= cross[0] - 0.1f && next <= cross[cross.Length - 1] + 0.1f;
        }

        bool CanTurnOnto(float lineCoord, Vector3 nd)
        {
            bool alongX = nd.x != 0;
            if (alongX ? G.NoTrafficZ.Contains(lineCoord) : G.NoTrafficX.Contains(lineCoord)) return false;
            return CanDrive(nd, lineCoord, line);
        }

        // ── Driving it yourself ──────────────────────────────────────────────
        public void TakeOver()
        {
            State = Mode.Player; yaw = transform.eulerAngles.y; speed = 0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public void Park() { State = Mode.Parked; speed = 0f; }

        void PlayerTick(float dt)
        {
            float thr = Controls.Throttle, steer = Controls.Steer;
            if (GameUI.Mode != Eko.Mode.None) { thr = 0; steer = 0; }
            if (Mathf.Abs(thr) > 0.01f) speed += thr * (thr * speed < 0 ? 16f : (speed < 0 ? 5f : 7f)) * dt;
            else speed = Mathf.MoveTowards(speed, 0f, 5f * dt);
            if (Controls.Handbrake) speed = Mathf.MoveTowards(speed, 0f, 20f * dt);
            speed = Mathf.Clamp(speed, -6f, MaxSpeed);
            float turn = steer * 75f * Mathf.Clamp(speed / 5f, -1f, 1f);
            yaw += turn * dt;
            var rot = Quaternion.Euler(0, yaw, 0);
            Vector3 delta = rot * Vector3.forward * speed * dt;

            if (delta.sqrMagnitude > 1e-6f && rb.SweepTest(delta.normalized, out var hit, delta.magnitude + 0.05f, QueryTriggerInteraction.Ignore))
            {
                delta = delta.normalized * Mathf.Max(0f, hit.distance - 0.05f);
                if (Mathf.Abs(speed) > 6f) GameUI.Toast("Crash!", true);
                speed *= -0.15f;
            }
            Vector3 p = transform.position + delta;
            // Follow the ground: road, kerb, bridge. No ground means water.
            if (Physics.Raycast(p + Vector3.up * 0.32f, Vector3.down, out var g, 4f, ~0, QueryTriggerInteraction.Ignore))
                p.y = Mathf.Lerp(p.y, g.point.y, 12f * dt);
            else p.y -= 6f * dt;
            rb.MovePosition(p); rb.MoveRotation(rot);
            if (p.y < -1.5f) Story.Drowned();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (GameUI.Mode == Eko.Mode.Intro) return;
            if (State == Mode.Traffic && GameUI.Mode == Eko.Mode.None)
            {
                TrafficTick(dt);
                Vector3 before = transform.position;
                transform.position = Vector3.MoveTowards(before, logical, (speed + 4f) * dt);
                // Face the way the car is really moving through a turn, then settle onto the lane.
                Vector3 moved = transform.position - before; moved.y = 0;
                var want = moved.sqrMagnitude > 1e-5f && Vector3.Distance(transform.position, logical) > 0.5f
                    ? Quaternion.LookRotation(moved).eulerAngles.y : Quaternion.LookRotation(dir).eulerAngles.y;
                yaw = Mathf.MoveTowardsAngle(yaw, want, 220f * dt);
                transform.rotation = Quaternion.Euler(0, yaw, 0);
            }
            else if (State == Mode.Player) PlayerTick(dt);
            Sound(dt);
            spin += speed * dt / wheelR * Mathf.Rad2Deg;
            foreach (var w in wheels) w.localRotation = Quaternion.Euler(spin, 0, 0);
        }

        void Sound(float dt)
        {
            var cam = Camera.main;
            bool mine = State == Mode.Player;
            bool on = State != Mode.Parked && cam != null && (mine || (cam.transform.position - transform.position).sqrMagnitude < 60f * 60f) && GameUI.Mode != Eko.Mode.Intro;
            if (on && !engine.isPlaying) { engine.time = Random.value * engine.clip.length * 0.9f; engine.Play(); }
            else if (!on && engine.isPlaying) engine.Stop();
            if (!on) return;
            float k = Mathf.Clamp01(Mathf.Abs(speed) / MaxSpeed);
            engine.pitch = 0.75f + k * 1.25f + (mine ? Mathf.Abs(Controls.Throttle) * 0.15f : 0f);
            engine.volume = mine ? 0.32f + k * 0.25f : 0.22f + k * 0.2f;
            engine.spatialBlend = mine ? 0.4f : 1f;

            // Horns: you on H; traffic when stuck behind something; danfos just because.
            hornWait -= dt;
            if (mine) { if (Controls.Horn && GameUI.Mode == Eko.Mode.None) engine.PlayOneShot(Sounds.Horn(Type == VType.Danfo), 0.9f); return; }
            if (hornWait > 0f) return;
            if (Stuck > 1.2f || (Type == VType.Danfo && Random.value < 0.25f))
            {
                engine.PlayOneShot(Sounds.Horn(Type == VType.Danfo || Random.value < 0.4f), 0.7f);
                hornWait = Random.Range(4f, 10f);
            }
            else hornWait = Random.Range(3f, 8f);
        }

        public float Speed => speed;
        public float Stuck;
    }
}
