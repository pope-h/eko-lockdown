using System.Collections.Generic;
using UnityEngine;

// Eko Lockdown: controls, camera, the player, and everyone else on the streets.
namespace Eko
{
    /// All input goes through here: keyboard and mouse, plus the on-screen controls on phones (Touch).
    public static class Controls
    {
        static float K(KeyCode a) => Input.GetKey(a) ? 1f : 0f;
        public static Vector2 Move => Vector2.ClampMagnitude(new Vector2(K(KeyCode.D) - K(KeyCode.A), K(KeyCode.W) - K(KeyCode.S)) + Touch.Move, 1f);
        public static bool Run => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Touch.Run;
        public static bool Crouch => Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl) || Touch.Sneak;
        public static bool Jump => Input.GetKeyDown(KeyCode.Space) || Touch.Take(ref Touch.JumpBtn);
        public static bool Interact => Input.GetKeyDown(KeyCode.E) || Touch.Take(ref Touch.Act);
        public static bool CarKey => Input.GetKeyDown(KeyCode.F) || Touch.Take(ref Touch.CarBtn);
        public static bool Dose => Input.GetKeyDown(KeyCode.Q) || Touch.Take(ref Touch.DoseBtn);
        public static bool MapKey => Input.GetKeyDown(KeyCode.M) || Touch.Take(ref Touch.MapBtn);
        public static bool RadioNext => Input.GetKeyDown(KeyCode.R) || Touch.Take(ref Touch.RadioBtn);
        public static bool RadioPrev => Input.GetKeyDown(KeyCode.T);
        public static bool Horn => Input.GetKeyDown(KeyCode.H) || Touch.Take(ref Touch.HornBtn);
        public static float Throttle => Move.y;
        public static float Steer => Move.x;
        public static bool Handbrake => Input.GetKey(KeyCode.Space) || Touch.Brake;
        public static float CamYaw => (K(KeyCode.RightArrow) - K(KeyCode.LeftArrow)) * 100f * Time.deltaTime + (Input.GetMouseButton(1) ? Input.GetAxis("Mouse X") * 4f : 0f) + Touch.Take(ref Touch.LookX);
        public static float CamPitch => (K(KeyCode.DownArrow) - K(KeyCode.UpArrow)) * 60f * Time.deltaTime - (Input.GetMouseButton(1) ? Input.GetAxis("Mouse Y") * 2.5f : 0f) + Touch.Take(ref Touch.LookY);
        public static float Zoom => Mathf.Clamp(Input.mouseScrollDelta.y, -1f, 1f) + (K(KeyCode.Equals) + K(KeyCode.KeypadPlus) - K(KeyCode.Minus) - K(KeyCode.KeypadMinus)) * 3f * Time.deltaTime + Touch.Take(ref Touch.Zoom);

        /// Filled in every frame by TouchControls. One-shot presses are consumed by whoever reads them.
        public static class Touch
        {
            public static Vector2 Move; public static bool Run, Sneak, Brake, Act, CarBtn, DoseBtn, JumpBtn, MapBtn, RadioBtn, HornBtn;
            public static float LookX, LookY, Zoom;
            public static bool Take(ref bool b) { bool v = b; b = false; return v; }
            public static float Take(ref float f) { float v = f; f = 0f; return v; }
        }
    }

    public class CameraRig : MonoBehaviour
    {
        public static float Yaw, Pitch = 16f, Dist = 6.5f;
        public static bool Snap; // jump straight to the new view (after a door or a teleport)
        float manualT;

        void LateUpdate()
        {
            var p = Player.I; if (p == null) return;
            bool driving = p.Driving != null;
            float dt = Time.deltaTime;
            if (GameUI.Mode == Mode.None)
            {
                float dy = Controls.CamYaw; if (Mathf.Abs(dy) > 0.001f) { Yaw += dy; manualT = 2f; }
                Pitch = Mathf.Clamp(Pitch + Controls.CamPitch, 2f, 80f);
                Dist = Mathf.Clamp(Dist * (1f - Controls.Zoom * 0.14f), 2.5f, 90f);
            }
            manualT -= dt;
            if (driving && manualT <= 0f && Mathf.Abs(p.Driving.Speed) > 1f)
                Yaw = Mathf.LerpAngle(Yaw, p.Driving.transform.eulerAngles.y + (p.Driving.Speed < 0 ? 180f : 0f), 2.5f * dt);

            float d = driving ? Mathf.Max(Dist, 7.5f + p.Driving.Len * 0.6f) : Dist;
            float pitch = Mathf.Max(Pitch, Mathf.Lerp(2f, 55f, Mathf.InverseLerp(12f, 90f, d)));
            Vector3 focus = p.Pos + Vector3.up * (driving ? 1.9f : 1.55f);
            var rot = Quaternion.Euler(pitch, Yaw, 0);
            Vector3 want = focus - rot * Vector3.forward * d;
            if (Physics.SphereCast(focus, 0.25f, (want - focus).normalized, out var hit, d, ~0, QueryTriggerInteraction.Ignore))
                want = focus + (want - focus).normalized * Mathf.Max(1.2f, hit.distance - 0.15f);
            transform.position = Snap ? want : Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-14f * dt));
            Snap = false;
            transform.rotation = Quaternion.LookRotation(focus - transform.position);
        }
    }

    public class Tag : MonoBehaviour
    {
        public static readonly List<Tag> All = new List<Tag>();
        public string Text; public Color Col = Color.white; public float Height = 2.1f; public float ShowRange = 18f; public bool Big;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        public static Tag Add(GameObject go, string text, float height, Color col, float range = 18f, bool big = false)
        {
            var t = go.AddComponent<Tag>(); t.Text = text; t.Height = height; t.Col = col; t.ShowRange = range; t.Big = big; return t;
        }
    }

    public enum Kind { Gate, Person, Door }

    public class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();
        public Kind Kind; public string Id; public string Prompt;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        public bool Available => Kind != Kind.Gate || Game.Member != Game.Lekki;
    }

    public class Player : MonoBehaviour
    {
        public static Player I;
        public Human Body; public CharacterController CC; public Car Driving;
        public Interactable Near; public Car NearCar;
        public bool Sneaking, Running;
        float vy;
        public Vector3 Pos => Driving != null ? Driving.transform.position : transform.position;

        public static Player Create(Transform parent, Vector3 pos, float yaw)
        {
            var h = Human.Build(parent, Looks.Dayo, pos, "Dayo", yaw);
            Tag.Add(h.gameObject, "Dayo", 2.15f, new Color(0.6f, 1f, 0.5f), 40f);
            return h.gameObject.AddComponent<Player>();
        }

        void Awake()
        {
            I = this; Body = GetComponent<Human>(); Body.AutoSpeed = false;
            CC = gameObject.AddComponent<CharacterController>();
            CC.height = 1.75f; CC.radius = 0.32f; CC.center = new Vector3(0, 0.9f, 0); CC.stepOffset = 0.4f; CC.skinWidth = 0.04f; CC.slopeLimit = 55f;
        }

        public void Teleport(Vector2 xz)
        {
            if (Driving != null) ExitCar();
            CC.enabled = false;
            float y = Physics.Raycast(new Vector3(xz.x, 3f, xz.y), Vector3.down, out var h, 10f, ~0, QueryTriggerInteraction.Ignore) ? h.point.y : 0.2f;
            transform.position = new Vector3(xz.x, y + 0.05f, xz.y);
            CC.enabled = true; vy = 0;
        }

        /// Put Dayo exactly here, facing yaw. Used by doors.
        public void Place(Vector3 p, float yaw)
        {
            if (Driving != null) ExitCar();
            CC.enabled = false; transform.position = p; transform.rotation = Quaternion.Euler(0, yaw, 0); CC.enabled = true; vy = 0;
        }

        void Update()
        {
            bool busy = GameUI.Mode != Mode.None;
            if (Driving != null)
            {
                Near = null; NearCar = null;
                if (!busy && Controls.CarKey) ExitCar();
                return;
            }

            Vector2 inp = busy ? Vector2.zero : Controls.Move;
            Sneaking = !busy && Controls.Crouch; Running = !busy && !Sneaking && Controls.Run;
            float speed = Sneaking ? 2.2f : Running ? 7.6f : 4.2f;
            var yawRot = Quaternion.Euler(0, CameraRig.Yaw, 0);
            Vector3 move = yawRot * new Vector3(inp.x, 0, inp.y);
            if (CC.isGrounded) { vy = -2f; if (!busy && Controls.Jump) vy = 5.2f; } else vy += Physics.gravity.y * Time.deltaTime;
            CC.Move((move * speed + Vector3.up * vy + Crowd.Push(Body, transform.position, 0.6f)) * Time.deltaTime);
            if (move.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 12f * Time.deltaTime);
            Body.Speed = Mathf.Lerp(Body.Speed, move.magnitude * speed, 12f * Time.deltaTime);
            Body.Crouch = Sneaking;
            if (transform.position.y < -1.2f) { Story.Drowned(); return; }

            Near = null; float best = 2.8f;
            foreach (var it in Interactable.All)
            {
                if (!it.Available) continue;
                float d = Flat(it.transform.position - transform.position);
                if (d < best) { best = d; Near = it; }
            }
            NearCar = null; float bc = 2.2f;
            foreach (var c in Car.All)
            {
                if (c.State == Car.Mode.Player) continue;
                float d = Flat(c.transform.position - transform.position) - c.Len * 0.45f;
                if (d < bc) { bc = d; NearCar = c; }
            }
            if (busy) return;
            if (Near != null && Controls.Interact) GameUI.Open(Near);
            else if (NearCar != null && Controls.CarKey) EnterCar(NearCar);
        }

        public static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        void EnterCar(Car c)
        {
            bool stolen = c.State == Car.Mode.Traffic;
            if (stolen) Population.DriverFlees(c);
            Driving = c; c.TakeOver(); CC.enabled = false;
            transform.SetParent(c.transform, false); transform.localPosition = c.Seat; transform.localRotation = Quaternion.identity;
            Body.Seated = true; Body.Driving = true;
            GameUI.Toast((stolen ? "\"Ah! Take am, take am!\" " : "") + "You take the " + c.Label.ToLower() + (TouchControls.On ? ". Left stick drives and steers." : ". W/S drive, A/D steer, Space brake, H horn, F to get out."));
        }

        public void ExitCar()
        {
            var c = Driving; Driving = null; c.Park();
            transform.SetParent(Bootstrap.World, true);
            Body.Seated = false; Body.Driving = false;
            Vector3 side = c.transform.position - c.transform.right * (c.Width / 2f + 0.8f);
            side.y = c.transform.position.y + 0.1f;
            transform.position = side; transform.rotation = Quaternion.Euler(0, c.transform.eulerAngles.y, 0);
            CC.enabled = true; vy = 0;
        }
    }

    /// Someone standing in one place: a vendor, a story character. Turns to face you when you come close.
    public class Idler : MonoBehaviour
    {
        float baseYaw;
        void Start() { baseYaw = transform.eulerAngles.y; }
        void Update()
        {
            if (Player.I == null) return;
            Vector3 to = Player.I.Pos - transform.position; to.y = 0;
            float want = to.magnitude < 6f ? Quaternion.LookRotation(to).eulerAngles.y : baseYaw;
            transform.rotation = Quaternion.Euler(0, Mathf.MoveTowardsAngle(transform.eulerAngles.y, want, 160f * Time.deltaTime), 0);
        }
    }

    /// Pedestrians walk the sidewalks and turn at corners.
    public class Walker : MonoBehaviour
    {
        public Grid3 G; Vector3 dir; float line, side, speed, pause, dodge; bool alongX;
        Human body;
        void Awake() { body = GetComponent<Human>(); }
        float along;

        public void Place(Grid3 g, bool onX, float lineCoord, float pos, float sideSign)
        {
            G = g; alongX = onX; line = lineCoord; side = sideSign; along = pos;
            dir = (Random.value < 0.5f ? 1f : -1f) * (onX ? Vector3.right : Vector3.forward);
            speed = Random.Range(1.1f, 1.7f); pause = 0f;
            transform.position = Pos(); transform.rotation = Quaternion.LookRotation(dir);
        }

        Vector3 Pos()
        {
            float off = line + side * (Grid3.HW(line) + 1.6f);
            return alongX ? new Vector3(along, 0.16f, off) : new Vector3(off, 0.16f, along);
        }

        void Update()
        {
            if (GameUI.Mode != Mode.None) return;
            float dt = Time.deltaTime;
            if (pause > 0f) { pause -= dt; return; }
            if (Random.value < 0.0015f) { pause = Random.Range(2f, 5f); return; }

            float s = alongX ? dir.x : dir.z, a0 = along, a1 = along + s * speed * dt;
            var cross = alongX ? G.Xs : G.Zs;
            foreach (var c in cross)
            {
                float stop = c - s * (Grid3.HW(c) + 1.6f);
                if ((a0 - stop) * (a1 - stop) > 0 || Mathf.Approximately(a0, stop)) continue;
                int choice = Random.Range(0, 10);
                bool edge = c <= cross[0] + 0.1f || c >= cross[cross.Length - 1] - 0.1f;
                if (choice < 3 || (edge && choice < 7))
                {
                    // Turn the corner onto the cross street, staying on this side of it.
                    float perpPos = line + side * (Grid3.HW(line) + 1.6f);
                    alongX = !alongX; line = c; side = -s; along = perpPos;
                    dir = (alongX ? Vector3.right : Vector3.forward) * (Random.value < 0.5f ? 1f : -1f);
                    break;
                }
                if (edge || choice == 9) { dir = -dir; a1 = a0; break; }
                break;
            }
            along = a1;
            Vector3 p = Pos();
            // Step aside for anyone in the way, then drift back to the line.
            Vector3 right = new Vector3(dir.z, 0, -dir.x);
            bool blocked = false;
            foreach (var h in Human.All)
            {
                if (h == body || h.Seated) continue;
                Vector3 o = h.transform.position - p; o.y = 0;
                float f = Vector3.Dot(o, dir);
                if (f > -0.2f && f < 1.8f && Mathf.Abs(Vector3.Dot(o, right) - dodge) < 0.7f) { blocked = true; break; }
            }
            dodge = Mathf.MoveTowards(dodge, blocked ? 0.95f : 0f, dt * 1.6f);
            p += right * dodge;
            Vector3 d = p - transform.position; d.y = 0;
            transform.position = p;
            if (d.sqrMagnitude > 1e-5f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 10f * dt);
        }

    }

    /// The Lagos State Fever Taskforce. They walk a beat and ignore you, until they see your veins.
    /// Then they blow the whistle and come for you: faster than a walk, slower than a sprint. They
    /// need line of sight, give up once they lose you, cannot catch a car, and never go into the
    /// estate or the port. If they lay hands on you, it is the quarantine camp.
    public class Enforcer : MonoBehaviour
    {
        const float ChaseSpeed = 6.3f, Sight = 18f, Forget = 34f, GrabTime = 0.5f, LoseAfter = 4f;
        public Vector3 Home; public bool Roamer;
        Vector3 target; float wait, grab, lost; bool chasing;
        Human body; AudioSource voice; Tag tag;
        public bool Chasing => chasing;
        public static int Hunting;

        void Awake()
        {
            body = GetComponent<Human>();
            voice = gameObject.AddComponent<AudioSource>(); voice.clip = Sfx.Whistle; voice.spatialBlend = 1f; voice.rolloffMode = AudioRolloffMode.Linear;
            voice.minDistance = 4f; voice.maxDistance = 45f; voice.volume = 0.9f; voice.dopplerLevel = 0f;
            tag = Tag.Add(gameObject, "Taskforce", 2.15f, new Color(1f, 0.6f, 0.25f), 22f);
        }

        void Start() { if (Home == Vector3.zero) Home = transform.position; if (target == Vector3.zero) target = Home; }

        void OnDisable() { if (chasing) { chasing = false; Hunting--; } }

        public void Place(Vector3 p)
        {
            if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out var gh, 4f, ~0, QueryTriggerInteraction.Ignore)) p.y = gh.point.y;
            Home = p; target = p; transform.position = p; transform.rotation = Quaternion.Euler(0, Random.value * 360f, 0);
            grab = 0; SetChase(false);
        }

        public static bool Safe(Vector3 p)
        {
            var v = new Vector2(p.x, p.z);
            return City.Estate.Contains(v) || City.PortYard.Contains(v) || City.PartyStreet.Contains(v) || Interior.Inside(p);
        }

        void SetChase(bool on)
        {
            if (on == chasing) return;
            chasing = on; Hunting += on ? 1 : -1;
            if (on) { voice.Play(); GameUI.Toast("TASKFORCE: \"You! Stop there! Show me your hands!\"  Run, or get in a car.", true); }
        }

        bool CanSee(Vector3 p, Vector3 pl)
        {
            Vector3 eye = p + Vector3.up * 1.6f, chest = pl + Vector3.up * 1.1f;
            return !Physics.Linecast(eye, chest, out var hit, ~0, QueryTriggerInteraction.Ignore) || hit.collider == Player.I.CC;
        }

        void Update()
        {
            if (GameUI.Mode != Mode.None || Player.I == null) return;
            float dt = Time.deltaTime;
            Vector3 p = transform.position, pl = Player.I.Pos;
            float d = Player.Flat(pl - p);
            var car = Player.I.Driving;

            // A car coming fast: jump out of the way.
            if (car != null && d < car.Len * 0.5f + 2.5f && Mathf.Abs(car.Speed) > 4f)
            {
                Vector3 side = Vector3.Cross(Vector3.up, car.transform.forward); if (Vector3.Dot(side, p - car.transform.position) < 0) side = -side;
                transform.position = p + side * 7f * dt; return;
            }

            bool onFoot = car == null;
            if (!chasing && onFoot && !Game.IsCovered && d < Sight && !Safe(pl) && CanSee(p, pl)) SetChase(true);
            if (chasing)
            {
                lost = CanSee(p, pl) ? 0f : lost + dt;
                if (!onFoot || d > Forget || lost > LoseAfter || Safe(pl)) { SetChase(false); GameUI.Toast("You lost the Taskforce."); }
            }
            if (tag != null) { tag.Text = chasing ? "TASKFORCE  !" : "Taskforce"; tag.Col = chasing ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.6f, 0.25f); }

            Vector3 goal; float sp;
            if (chasing)
            {
                goal = pl; sp = ChaseSpeed;
                if (d < 1.1f) { grab += dt; if (grab >= GrabTime) { grab = 0; SetChase(false); Story.Arrested(); return; } }
                else grab = Mathf.Max(0, grab - dt * 2f);
            }
            else
            {
                grab = 0;
                if (wait > 0) { wait -= dt; return; }
                if (Player.Flat(target - p) < 0.3f) { target = Home + new Vector3(Random.Range(-9f, 9f), 0, Random.Range(-9f, 9f)); wait = Random.Range(2f, 6f); return; }
                goal = target; sp = 1.2f;
            }
            Vector3 step = goal - p; step.y = 0;
            if (step.sqrMagnitude < 0.01f) return;
            Vector3 dir = Steer(p, step.normalized);
            Vector3 np = p + dir * Mathf.Min(sp * dt, step.magnitude) + Crowd.Push(body, p) * dt;
            if (!chasing && Safe(np)) { target = Home; return; }
            if (Physics.Raycast(np + Vector3.up * 0.8f, Vector3.down, out var gh, 2f, ~0, QueryTriggerInteraction.Ignore)) np.y = gh.point.y;
            transform.position = np;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * dt);
        }

        /// Walls and parked cars are in the way: feel left and right for a way round.
        static Vector3 Steer(Vector3 p, Vector3 dir)
        {
            Vector3 eye = p + Vector3.up * 0.6f;
            if (!Physics.Raycast(eye, dir, 0.9f, ~0, QueryTriggerInteraction.Ignore)) return dir;
            foreach (var a in new[] { 50f, -50f, 95f, -95f })
            {
                var d = Quaternion.Euler(0, a, 0) * dir;
                if (!Physics.Raycast(eye, d, 0.9f, ~0, QueryTriggerInteraction.Ignore)) return d;
            }
            return dir;
        }
    }

    /// Keeps people from walking through each other: a gentle push away from anyone too close.
    public static class Crowd
    {
        public static Vector3 Push(Human self, Vector3 p, float radius = 0.75f)
        {
            Vector3 push = Vector3.zero;
            foreach (var h in Human.All)
            {
                if (h == self || h.Seated) continue;
                Vector3 d = p - h.transform.position; d.y = 0;
                float m = d.magnitude;
                if (m < radius && m > 1e-4f) push += d / m * (radius - m) * 6f;
            }
            return push;
        }
    }

    /// The driver of a car you took. Runs off, slows to a walk, and is gone once you are far away.
    public class Fleer : MonoBehaviour
    {
        Vector3 dir; float t;

        public static void Go(Human h, Vector3 away)
        {
            var f = h.gameObject.AddComponent<Fleer>(); away.y = 0; f.dir = away.normalized;
        }

        void Update()
        {
            if (GameUI.Mode != Mode.None) return;
            float dt = Time.deltaTime; t += dt;
            if (Physics.Raycast(transform.position + Vector3.up * 0.6f, dir, 0.8f, ~0, QueryTriggerInteraction.Ignore))
                dir = Quaternion.Euler(0, Random.value < 0.5f ? 90f : -90f, 0) * dir;
            transform.position += dir * (t < 5f ? 6.5f : 1.4f) * dt;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * dt);
            if (t > 60f || (t > 8f && Player.I != null && Player.Flat(Player.I.Pos - transform.position) > 70f)) Destroy(gameObject);
        }
    }

    /// Small synthesised sounds: the Taskforce whistle.
    public static class Sfx
    {
        const int Rate = 22050;
        static AudioClip whistle;

        /// Two sharp blasts on a pea whistle.
        public static AudioClip Whistle
        {
            get
            {
                if (whistle != null) return whistle;
                var r = new System.Random(3); int n = (int)(1.1f * Rate); var d = new float[n];
                float ph = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate;
                    bool on = t < 0.32f || (t > 0.45f && t < 1.0f);
                    float env = on ? Mathf.Min(1f, Mathf.Min((t < 0.4f ? t : t - 0.45f) * 60f, 1f)) : 0f;
                    float f = 3100f + 260f * Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 38f * t)); // the pea rattles the pitch
                    ph += 2 * Mathf.PI * f / Rate;
                    d[i] = (Mathf.Sin(ph) * 0.5f + ((float)r.NextDouble() * 2f - 1f) * 0.08f) * env;
                }
                whistle = AudioClip.Create("whistle", n, 1, Rate, false); whistle.SetData(d, 0); return whistle;
            }
        }
    }

    /// A patrolling guard. Sees in a cone, but not through containers or walls.
    public class Guard : MonoBehaviour
    {
        public Vector3[] Path; public float Range = 11f, Speed = 1.8f;
        int i; float alert; Transform cone; Material mat; Tag tag;

        public void Init()
        {
            var go = new GameObject("cone"); go.transform.SetParent(transform, false); go.transform.localPosition = new Vector3(0, 0.08f, 0);
            cone = go.transform;
            var mesh = new Mesh(); const int n = 14; float half = 45f * Mathf.Deg2Rad;
            var v = new Vector3[n + 2]; v[0] = Vector3.zero;
            for (int k = 0; k <= n; k++) { float a = -half + 2 * half * k / n; v[k + 1] = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * Range; }
            var t = new int[n * 3];
            for (int k = 0; k < n; k++) { t[k * 3] = 0; t[k * 3 + 1] = k + 1; t[k * 3 + 2] = k + 2; }
            mesh.vertices = v; mesh.triangles = t; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mat = new Material(Shader.Find("Sprites/Default")); mr.material = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tag = GetComponent<Tag>();
        }

        void Update()
        {
            bool hostile = Story.Hostile;
            cone.gameObject.SetActive(hostile);
            if (tag != null) tag.Text = hostile && alert > 0.05f ? "Guard  !" : "Guard";
            if (GameUI.Mode != Mode.None || Path == null || Path.Length == 0) return;
            float dt = Time.deltaTime;

            Vector3 p = transform.position, dest = Path[i];
            Vector3 step = dest - p; step.y = 0;
            if (step.magnitude < 0.2f) i = (i + 1) % Path.Length;
            else
            {
                transform.position = p + step.normalized * Mathf.Min(Speed * dt, step.magnitude);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(step), 5f * dt);
            }
            float sc = Player.I != null && Player.I.Sneaking ? 0.55f : 1f;
            cone.localScale = new Vector3(sc, 1f, sc);
            if (!hostile || Player.I == null || Player.I.Driving != null) { alert = 0; return; }

            Vector3 to = Player.I.transform.position - p; to.y = 0; float d = to.magnitude;
            bool inCone = (d < Range * sc && Vector3.Angle(transform.forward, to) < 45f) || d < 1.6f;
            bool see = inCone;
            if (inCone)
            {
                Vector3 eye = p + Vector3.up * 1.6f, chest = Player.I.transform.position + Vector3.up * 1.0f;
                if (Physics.Linecast(eye, chest, out var hit, ~0, QueryTriggerInteraction.Ignore) && hit.collider != Player.I.CC) see = false;
            }
            alert = Mathf.Clamp01(alert + (see ? dt / 0.75f : -dt / 1.5f));
            mat.color = Color.Lerp(new Color(1f, 0.9f, 0.2f, 0.18f), new Color(1f, 0.1f, 0.05f, 0.45f), alert);
            if (alert >= 1f) { alert = 0; Story.Caught("Spotted! You slip away and try again."); }
        }
    }

    /// A dose of Suppressant lying on the ground. Comes back after a while.
    public class Pickup : MonoBehaviour
    {
        public static readonly List<Pickup> All = new List<Pickup>();
        float respawn; Transform vial; Renderer[] rs;
        /// On the ground right now (not yet picked up, or grown back).
        public bool Ready => respawn <= 0f;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        void Start() { vial = transform.GetChild(0); rs = GetComponentsInChildren<Renderer>(); }
        void Update()
        {
            if (respawn > 0) { respawn -= Time.deltaTime; if (respawn <= 0) foreach (var r in rs) r.enabled = true; return; }
            vial.localRotation = Quaternion.Euler(0, Time.time * 90f, 0);
            vial.localPosition = new Vector3(0, 0.9f + Mathf.Sin(Time.time * 3f) * 0.12f, 0);
            if (Player.I != null && Game.Doses < 5 && Player.Flat(Player.I.Pos - transform.position) < 1.6f)
            {
                Game.Doses++; respawn = 70f; foreach (var r in rs) r.enabled = false;
                GameUI.Toast("Picked up a dose (" + Game.Doses + ")");
            }
        }
    }

    /// The Lekki estate gate. Slides open once the residents vote you in.
    public class EstateGate : MonoBehaviour
    {
        public Collider Col; Vector3 shut; float t;
        void Start() { shut = transform.localPosition; }
        void Update()
        {
            bool open = Game.Member == Game.Lekki;
            t = Mathf.MoveTowards(t, open ? 1f : 0f, Time.deltaTime * 0.5f);
            transform.localPosition = shut + new Vector3(0, 0, t * 8.5f);
            Col.enabled = t < 0.6f;
        }
    }

    /// Light and weather: afternoon to night across the five chapters.
    public class Sky : MonoBehaviour
    {
        public static Light Sun;
        float night;

        void Update()
        {
            float target = Story.S == null ? 0f : Mathf.Clamp01((Story.Chapter - 1) / 4f);
            night = Mathf.MoveTowards(night, target, Time.deltaTime * 0.08f);
            float elev = Mathf.Lerp(52f, -8f, night);
            Sun.transform.rotation = Quaternion.Euler(Mathf.Max(elev, 6f), -35f + night * 40f, 0);
            Sun.color = Color.Lerp(Mats.Hex("fff1d6"), Mats.Hex("ff9a4a"), Mathf.Clamp01(night * 1.6f));
            Sun.intensity = Mathf.Lerp(1.15f, 0.38f, night);
            Sun.shadowStrength = Mathf.Lerp(0.85f, 0.2f, night);
            RenderSettings.ambientLight = Color.Lerp(Mats.Hex("9aa6b4"), Mats.Hex("4a5878"), night);
            RenderSettings.fogColor = Color.Lerp(Mats.Hex("c9d3dc"), Mats.Hex("2a3248"), night);
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure")) RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(1.2f, 0.25f, night));
            Mats.Pool.color = new Color(1f, 0.8f, 0.5f, Mathf.Lerp(0.02f, 0.7f, night));
        }
    }

    /// The street party. Its music is a 3D sound: it gets louder as you walk in, and it drives the dancers.
    public class Owambe : MonoBehaviour
    {
        public static float Beat;
        AudioSource src; float bpm;

        void Start()
        {
            src = gameObject.AddComponent<AudioSource>();
            src.clip = Radio.ClipFor(0, out bpm); src.loop = true; src.spatialBlend = 1f; src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 10f; src.maxDistance = 85f; src.volume = 0.9f; src.dopplerLevel = 0f; src.Play();
        }

        void Update() { Beat = (src != null && src.isPlaying ? src.time : Time.time) * bpm / 60f; }
    }

    /// Keeps the streets full near you: pedestrians and traffic are recycled from far away to nearby.
    public class Population : MonoBehaviour
    {
        static Population I;
        readonly List<Walker> walkers = new List<Walker>();
        readonly List<Car> traffic = new List<Car>();
        readonly List<Enforcer> patrols = new List<Enforcer>();
        float tick; Transform root;
        public static int WalkerCount = 34, CarCount = 22, PatrolMax = 12;
        System.Random rng = new System.Random(77);

        public static void Begin(Transform world)
        {
            var go = new GameObject("Population"); go.transform.SetParent(world, false);
            I = go.AddComponent<Population>(); I.root = go.transform;
            for (int i = 0; i < WalkerCount; i++)
            {
                var h = Human.Build(I.root, Looks.Citizen(I.rng), Vector3.zero, "Walker", 0f, 0.9f + (float)I.rng.NextDouble() * 0.12f);
                var w = h.gameObject.AddComponent<Walker>(); I.walkers.Add(w); I.Relocate(w, true);
            }
            for (int i = 0; i < CarCount; i++)
            {
                int k = I.rng.Next(20);
                VType t = k < 7 ? VType.Danfo : k < 12 ? VType.Sedan : k < 14 ? VType.Suv : k < 17 ? VType.Keke : k < 19 ? VType.Okada : VType.Truck;
                var c = Car.Build(I.root, t, City.VehicleColour(t, I.rng), Vector3.zero, 0f, Car.Mode.Traffic);
                I.traffic.Add(c); I.Relocate(c, true);
            }
            for (int i = 0; i < PatrolMax; i++)
            {
                var h = Human.Build(I.root, Looks.Taskforce(I.rng), Vector3.zero, "Taskforce", 0f, 0.97f + (float)I.rng.NextDouble() * 0.06f);
                var z = h.gameObject.AddComponent<Enforcer>(); z.Roamer = true; I.patrols.Add(z); I.Relocate(z, true);
            }
        }

        public static void Recycle(Walker w) { if (I != null) I.Relocate(w, false); }

        public static void DriverFlees(Car c)
        {
            if (I == null) return;
            I.traffic.Remove(c);
            var driver = c.Eject();
            if (driver != null) Fleer.Go(driver, -c.transform.right * 2f + c.transform.forward);
            // Replace the stolen car in the traffic pool.
            var t = new[] { VType.Danfo, VType.Sedan, VType.Sedan, VType.Keke, VType.Okada }[I.rng.Next(5)];
            var nc = Car.Build(I.root, t, City.VehicleColour(t, I.rng), Vector3.zero, 0f, Car.Mode.Traffic);
            I.traffic.Add(nc); I.Relocate(nc, false);
        }

        void Update()
        {
            if (Player.I == null || GameUI.Mode == Mode.Intro) return;
            tick -= Time.deltaTime; if (tick > 0) return; tick = 0.5f;
            Vector3 pl = Player.I.Pos;
            foreach (var w in walkers) if (Player.Flat(w.transform.position - pl) > 115f) Relocate(w, false);
            foreach (var c in traffic)
            {
                if (c.State != Car.Mode.Traffic) continue;
                float d = Player.Flat(c.transform.position - pl);
                if (d > 175f || (c.Stuck > 8f && d > 35f)) Relocate(c, false);
            }
            for (int i = 0; i < patrols.Count; i++)
            {
                var z = patrols[i];
                bool want = i < Story.Patrols;
                if (z.gameObject.activeSelf != want) { z.gameObject.SetActive(want); if (want) Relocate(z, false); }
                if (want && !z.Chasing && Player.Flat(z.transform.position - pl) > 125f) Relocate(z, false);
            }
        }

        bool PickSpot(float minD, float maxD, bool forCars, out Grid3 g, out bool onX, out float line, out float along)
        {
            Vector3 pl = Player.I != null ? Player.I.Pos : new Vector3(24, 0, 81);
            for (int tries = 0; tries < 30; tries++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f, dist = minD + (float)rng.NextDouble() * (maxD - minD);
                var pt = new Vector2(pl.x + Mathf.Cos(ang) * dist, pl.z + Mathf.Sin(ang) * dist);
                g = City.GridAt(pt); if (g == null) continue;
                onX = rng.Next(2) == 0;
                var lines = onX ? g.Zs : g.Xs;
                line = lines[0]; float best = float.MaxValue;
                foreach (var l in lines) { float dd = Mathf.Abs(l - (onX ? pt.y : pt.x)); if (dd < best) { best = dd; line = l; } }
                if (forCars && (onX ? g.NoTrafficZ.Contains(line) : g.NoTrafficX.Contains(line))) continue;
                var cross = onX ? g.Xs : g.Zs;
                along = Mathf.Clamp(onX ? pt.x : pt.y, cross[0] + 2f, cross[cross.Length - 1] - 2f);
                var spot = onX ? new Vector2(along, line) : new Vector2(line, along);
                if (City.Estate.Contains(spot) || City.PortYard.Contains(spot) || City.PartyStreet.Contains(spot)) continue;
                return true;
            }
            g = null; onX = false; line = 0; along = 0; return false;
        }

        void Relocate(Walker w, bool initial)
        {
            if (!PickSpot(initial ? 6f : 45f, initial ? 90f : 105f, false, out var g, out var onX, out var line, out var along)) return;
            w.Place(g, onX, line, along, rng.Next(2) == 0 ? 1f : -1f);
        }

        void Relocate(Enforcer z, bool initial)
        {
            if (!PickSpot(initial ? 45f : 60f, initial ? 110f : 115f, false, out var g, out var onX, out var line, out var along)) return;
            float off = (rng.Next(2) == 0 ? 1f : -1f) * (Grid3.HW(line) + 1.6f); // on the sidewalk
            var p = onX ? new Vector3(along, 0.05f, line + off) : new Vector3(line + off, 0.05f, along);
            if (Enforcer.Safe(p)) return;
            z.Place(p);
        }

        void Relocate(Car c, bool initial)
        {
            if (!PickSpot(initial ? 25f : 80f, initial ? 150f : 165f, true, out var g, out var onX, out var line, out var along)) return;
            Vector3 d = (rng.Next(2) == 0 ? 1f : -1f) * (onX ? Vector3.right : Vector3.forward);
            c.PlaceOn(g, d, line, along);
        }
    }

    public class Bootstrap : MonoBehaviour
    {
        public static Transform World;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera"; cam.nearClipPlane = 0.2f; cam.farClipPlane = 700f; cam.fieldOfView = 58f;
            cam.clearFlags = CameraClearFlags.Skybox; cam.backgroundColor = Mats.Hex("c9d3dc");
            cam.gameObject.AddComponent<CameraRig>(); cam.gameObject.AddComponent<AudioListener>();

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.85f;
            Sky.Sun = sun; RenderSettings.sun = sun;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 160f; RenderSettings.fogEndDistance = 620f;
            if (RenderSettings.skybox != null) RenderSettings.skybox = new Material(RenderSettings.skybox);
            QualitySettings.shadowDistance = 90f;
            if (Application.isMobilePlatform)
            {
                // Phones: shorter shadows and view, fewer people and cars.
                QualitySettings.shadowDistance = 40f; sun.shadows = LightShadows.Hard;
                cam.farClipPlane = 420f; RenderSettings.fogStartDistance = 110f; RenderSettings.fogEndDistance = 400f;
                Population.WalkerCount = 20; Population.CarCount = 14; Population.PatrolMax = 9;
            }

            var host = new GameObject("Eko");
            host.AddComponent<Bootstrap>(); host.AddComponent<GameUI>(); host.AddComponent<Radio>(); host.AddComponent<Sky>(); host.AddComponent<WaterMotion>(); host.AddComponent<Ambience>(); host.AddComponent<TouchControls>();
            Application.runInBackground = true; Application.targetFrameRate = 60;
            Restart();
        }

        public static void Restart()
        {
            if (World != null) Destroy(World.gameObject);
            Game.NewGame(); Story.S = null; Story.Pos.Clear();
            Interactable.All.Clear(); Tag.All.Clear(); Car.All.Clear();
            GameUI.ResetUI();
            World = City.Build().transform;
            MapShot.Take();
            var p = Player.Create(World, new Vector3(City.Start.x, 0.2f, City.Start.y), 90f);
            CameraRig.Yaw = 90f;
            Camera.main.transform.position = p.transform.position + new Vector3(-6, 3, 0);
            Population.Begin(World);
        }

        public static void StartCampaign()
        {
            Story.Begin();
            Story.LastSafe = new Vector2(Player.I.transform.position.x, Player.I.transform.position.z);
        }

        void Update()
        {
            if (GameUI.Mode != Mode.None || Story.S == null) return;
            if (Game.Bitten) Game.Inf += Game.InfPerSecond * Time.deltaTime;
            if (Game.Inf >= 100f) Story.Arrested(true);
        }
    }
}
