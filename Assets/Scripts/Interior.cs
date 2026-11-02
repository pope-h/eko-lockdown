using System.Collections.Generic;
using UnityEngine;

// Eko Lockdown: rooms you can walk into. Each interior is built away from the city and joined to
// its street door by a pair of doors: press E on one and you come out of the other.
namespace Eko
{
    public class Interior
    {
        public static readonly List<Interior> All = new List<Interior>();
        public string Name; public Rect Area; public Vector3 Exit;

        public static Interior At(Vector3 p)
        {
            var v = new Vector2(p.x, p.z);
            foreach (var i in All) if (i.Area.Contains(v)) return i;
            return null;
        }

        public static bool Inside(Vector3 p) => At(p) != null;
    }

    /// One side of a doorway. Using it puts you at the other side, facing into the room or the street.
    public class Door : MonoBehaviour
    {
        public Vector3 To; public float Yaw; public string Arrive;

        public void Use()
        {
            Player.I.Place(To, Yaw);
            CameraRig.Yaw = Yaw; CameraRig.Snap = true;
            GameUI.Flash(Color.black);
            if (Arrive != null) GameUI.Card(Arrive);
        }
    }
}
