using UnityEngine;

namespace HoldTheMine
{
    public class Tracer : MonoBehaviour
    {
        public static Material Mat;
        LineRenderer lr;
        float t;
        const float Life = 0.07f;

        public static void Spawn(Vector3 a, Vector3 b, Color c)
        {
            var go = new GameObject("Tracer");
            var tr = go.AddComponent<Tracer>();
            var lr = tr.lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = Mat;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.startWidth = 0.09f;
            lr.endWidth = 0.05f;
            lr.startColor = c;
            lr.endColor = c;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = 1f - t / Life;
            lr.widthMultiplier = Mathf.Max(0, k);
            if (t >= Life) Destroy(gameObject);
        }
    }
}
