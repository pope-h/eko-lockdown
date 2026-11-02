using System.Collections;
using System.IO;
using UnityEngine;

// Dev tool: launch with  -ekoshots <folder>  and the game visits key places, saves a
// screenshot of each, and quits. Does nothing in a normal launch.
namespace Eko
{
    public class AutoShot : MonoBehaviour
    {
        string dir;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-ekoshots") { new GameObject("AutoShot").AddComponent<AutoShot>().dir = args[i + 1]; return; }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            yield return new WaitForSeconds(2f);
            yield return Shot("00_title");

            GameUI.Mode = Mode.None; Bootstrap.StartCampaign();
            yield return new WaitForSeconds(1f);
            yield return Shot("01_dialogue");
            GameUI.SkipDialogue();
            yield return new WaitForSeconds(3.5f);
            yield return Shot("02_start_mushin");

            yield return Visit(new Vector2(40f, 77f), 270f, 14f, 6.5f, "03_market");
            yield return Visit(new Vector2(262f, 214f), 0f, 12f, 9f, "04_owambe");
            yield return Visit(new Vector2(0f, -4f), 45f, 18f, 14f, "05_main_road");
            yield return Visit(new Vector2(-308f, -150f), 300f, 14f, 8f, "06_ajegunle");
            Story.I = 9;
            yield return Visit(new Vector2(225f, -228f), 180f, 30f, 16f, "07_port_yard");
            yield return Visit(new Vector2(470f, 150f), 90f, 12f, 12f, "08_bridge");
            Story.I = 14; Game.Member = Game.Lekki;
            yield return Visit(new Vector2(770f, 78f), 70f, 28f, 18f, "09_estate");
            yield return Visit(new Vector2(0f, 75f), 30f, 70f, 120f, "10_aerial");

            // The infected and the traffic up close: a corner with a horde, then a street with cars passing.
            Story.I = 2; Game.Member = -1; Game.Inf = 70f;
            Player.I.Teleport(new Vector2(147f, 149f)); CameraRig.Yaw = 20f; CameraRig.Pitch = 6f; CameraRig.Dist = 5f;
            yield return new WaitForSeconds(0.9f);
            yield return Shot("10b_taskforce");
            Game.Inf = 10f; GameUI.Mode = Mode.None; Story.Retry();
            // Inside: the bar and the hospital.
            Player.I.Place(new Vector3(3005f, 0.05f, -5f), 330f); CameraRig.Yaw = 330f; CameraRig.Pitch = 18f; CameraRig.Dist = 3.5f; CameraRig.Snap = true;
            yield return new WaitForSeconds(1.5f); yield return Shot("10d_bar");
            Player.I.Place(new Vector3(3000f, 0.05f, 194f), 330f); CameraRig.Yaw = 330f; CameraRig.Snap = true;
            yield return new WaitForSeconds(1.5f); yield return Shot("10e_hospital");
            yield return Visit(new Vector2(-30f, 76f), 0f, 10f, 7f, "10f_bar_front");
            yield return Visit(new Vector2(447f, 118f), 60f, 16f, 9f, "10g_waterfront");
            yield return Visit(new Vector2(780f, -150f), 180f, 14f, 12f, "10h_beach");
            Car close = null; float cd = 999f;
            foreach (var c in Car.All) if (c.State == Car.Mode.Traffic && c.Driver != null) { float d = Player.Flat(c.transform.position - Player.I.transform.position); if (d < cd) { cd = d; close = c; } }
            if (close != null)
            {
                var side = close.transform.position - close.transform.right * 5f;
                Player.I.Teleport(new Vector2(side.x, side.z));
                CameraRig.Yaw = close.transform.eulerAngles.y + 90f; CameraRig.Pitch = 8f; CameraRig.Dist = 4f;
                yield return new WaitForSeconds(0.1f);
                yield return Shot("10c_traffic_driver");
            }

            // Drive a car for a moment
            Story.I = 1;
            Player.I.Teleport(new Vector2(4.2f, 96f));
            yield return new WaitForSeconds(0.5f);
            var p = Player.I; Car near = null; float best = 99f;
            foreach (var c in Car.All) { float d = Player.Flat(c.transform.position - p.transform.position); if (d < best && c.State != Car.Mode.Traffic) { best = d; near = c; } }
            if (near != null)
            {
                p.SendMessage("EnterCar", near, SendMessageOptions.DontRequireReceiver);
                CameraRig.Yaw = near.transform.eulerAngles.y; CameraRig.Pitch = 14f; CameraRig.Dist = 8f;
                yield return new WaitForSeconds(2f);
                yield return Shot("11_driving");
                int on = 0; foreach (var r in p.GetComponentsInChildren<Renderer>()) if (r.enabled && r.gameObject.activeInHierarchy) on++;
                Debug.Log("EKOSHOT driver parent=" + (p.transform.parent != null ? p.transform.parent.name : "none") + " local=" + p.transform.localPosition + " world=" + p.transform.position + " car=" + near.transform.position + " renderersOn=" + on + " seated=" + p.Body.Seated);
                CameraRig.Yaw = near.transform.eulerAngles.y + 70f; CameraRig.Pitch = 10f; CameraRig.Dist = 3f;
                yield return new WaitForSeconds(0.3f);
                yield return Shot("11b_driving_side");
            }
            // The phone layout, as it looks with the touch controls on.
            TouchControls.On = true;
            yield return new WaitForSeconds(0.5f);
            yield return Shot("11c_touch_driving");
            Player.I.ExitCar();
            Player.I.Teleport(new Vector2(14f, 84f));
            yield return new WaitForSeconds(1f);
            yield return Shot("11d_touch_foot");
            TouchControls.On = false;
            GameUI.Mode = Mode.Map;
            yield return new WaitForSeconds(0.5f);
            yield return Shot("12_map");
            GameUI.Mode = Mode.None;
            Application.Quit();
        }

        IEnumerator Visit(Vector2 p, float yaw, float pitch, float dist, string name)
        {
            Player.I.Teleport(p);
            CameraRig.Yaw = yaw; CameraRig.Pitch = pitch; CameraRig.Dist = dist;
            yield return new WaitForSeconds(3f);
            yield return Shot(name);
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            yield return new WaitForSeconds(0.5f);
        }
    }
}
