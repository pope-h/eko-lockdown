using UnityEngine;

// Eko Lockdown: the map is a real overhead picture of the city, taken once by an orthographic
// camera after the city is built, in clear daylight with no fog. GameUI draws markers over it.
namespace Eko
{
    public static class MapShot
    {
        public static RenderTexture Tex;

        public static void Take()
        {
            var B = City.MapBounds;
            if (Tex == null)
            {
                int w = TouchControls.Lite ? 1024 : 2048; // phones: a quarter of the memory
                Tex = new RenderTexture(w, Mathf.RoundToInt(w * B.height / B.width), 24) { antiAliasing = TouchControls.Lite ? 1 : 2, useMipMap = !TouchControls.Lite, autoGenerateMips = !TouchControls.Lite, filterMode = FilterMode.Bilinear, name = "MapShot" };
            }
            var go = new GameObject("MapCam");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false; cam.orthographic = true; cam.orthographicSize = B.height / 2f; cam.aspect = B.width / B.height;
            cam.transform.position = new Vector3(B.center.x, 300f, B.center.y); cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cam.nearClipPlane = 1f; cam.farClipPlane = 400f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Mats.Hex("1f5466");
            cam.targetTexture = Tex;

            // Daylight with no haze, just for the photo.
            bool fog = RenderSettings.fog; var amb = RenderSettings.ambientLight;
            RenderSettings.fog = false; RenderSettings.ambientLight = Mats.Hex("b4bcc6");
            cam.Render();
            RenderSettings.fog = fog; RenderSettings.ambientLight = amb;
            cam.targetTexture = null; Object.Destroy(go);
        }
    }
}
