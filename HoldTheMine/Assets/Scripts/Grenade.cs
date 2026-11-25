using UnityEngine;

namespace HoldTheMine
{
    /// <summary>A bomb thrown in an arc by Grenadiers.</summary>
    public class Grenade : MonoBehaviour
    {
        Vector3 from, to;
        float flight, t, damage, radius;
        Team team;

        public static void Throw(Material mat, Vector3 from, Vector3 to, float flightTime, float damage, float radius, Team team)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(go.GetComponent<Collider>());
            go.name = "Grenade";
            go.transform.localScale = Vector3.one * 0.35f;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var g = go.AddComponent<Grenade>();
            g.from = from; g.to = to; g.flight = flightTime; g.damage = damage; g.radius = radius; g.team = team;
            go.transform.position = from;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / flight);
            Vector3 p = Vector3.Lerp(from, to, k);
            p.y += Mathf.Sin(k * Mathf.PI) * (2f + Vector3.Distance(from, to) * 0.25f);
            transform.position = p;
            transform.Rotate(400 * Time.deltaTime, 250 * Time.deltaTime, 0);
            if (k >= 1f)
            {
                Explosion.Explode(to, radius, damage, team);
                Destroy(gameObject);
            }
        }
    }
}
