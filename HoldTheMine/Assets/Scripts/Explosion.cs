using System.Collections.Generic;
using UnityEngine;

namespace HoldTheMine
{
    /// <summary>Simple blocky FX: expanding fireball, sparks, debris cubes. Also area damage.</summary>
    public class Explosion : MonoBehaviour
    {
        public static Material FireMat, SparkMat, DebrisMat, UpgradeMat;

        enum Kind { Fireball, Debris, Rise }
        Kind kind;
        float life, t, size;
        Vector3 velocity;

        static readonly Collider[] Overlap = new Collider[64];

        public static void Explode(Vector3 pos, float radius, float damage, Team sourceTeam)
        {
            SpawnFireball(pos, radius);
            int n = Physics.OverlapSphereNonAlloc(pos, radius, Overlap, ~0, QueryTriggerInteraction.Ignore);
            var done = new HashSet<Health>();
            for (int i = 0; i < n; i++)
            {
                var h = Overlap[i].GetComponentInParent<Health>();
                if (h == null || h.IsDead || h.team == sourceTeam || !done.Add(h)) continue;
                float d = Vector3.Distance(Combat.ClosestPoint(h, pos), pos);
                float falloff = Mathf.Clamp01(1f - d / (radius * 1.2f));
                h.TakeDamage(damage * Mathf.Lerp(0.35f, 1f, falloff));
            }
            if (GameManager.Instance != null) GameManager.Instance.OnExplosion(pos);
        }

        public static void SpawnFireball(Vector3 pos, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(go.GetComponent<Collider>());
            go.name = "Fireball";
            go.transform.position = pos + Vector3.up * 0.5f;
            go.transform.rotation = Random.rotation;
            go.GetComponent<Renderer>().sharedMaterial = FireMat;
            var e = go.AddComponent<Explosion>();
            e.kind = Kind.Fireball; e.life = 0.45f; e.size = size * 1.4f;
            go.transform.localScale = Vector3.one * 0.2f;
            SpawnDebris(pos, size * 0.6f);
        }

        public static void SpawnSpark(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(go.GetComponent<Collider>());
            go.name = "Spark";
            go.transform.position = pos;
            go.GetComponent<Renderer>().sharedMaterial = SparkMat;
            var e = go.AddComponent<Explosion>();
            e.kind = Kind.Fireball; e.life = 0.12f; e.size = 0.35f;
            go.transform.localScale = Vector3.one * 0.1f;
        }

        public static void SpawnDebris(Vector3 pos, float amount)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(5 * amount), 3, 14);
            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                DestroyImmediate(go.GetComponent<Collider>());
                go.name = "Debris";
                go.transform.position = pos + Vector3.up * 0.8f;
                go.transform.rotation = Random.rotation;
                go.transform.localScale = Vector3.one * Random.Range(0.15f, 0.35f) * Mathf.Sqrt(amount);
                go.GetComponent<Renderer>().sharedMaterial = DebrisMat;
                var e = go.AddComponent<Explosion>();
                e.kind = Kind.Debris; e.life = Random.Range(0.6f, 1.1f);
                var r = Random.insideUnitCircle * 4f;
                e.velocity = new Vector3(r.x, Random.Range(4f, 8f), r.y);
            }
        }

        /// <summary>Golden cubes spiralling upwards plus a flash ring: shown when something is upgraded.</summary>
        public static void SpawnUpgradeFx(Vector3 pos, float size)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(10 * size), 10, 40);
            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                DestroyImmediate(go.GetComponent<Collider>());
                go.name = "UpgradeFx";
                float a = i * Mathf.PI * 2f / count * 3f;
                float r = size * 0.8f;
                go.transform.position = pos + new Vector3(Mathf.Cos(a) * r, 0.2f + i * 0.04f, Mathf.Sin(a) * r);
                go.transform.localScale = Vector3.one * Random.Range(0.15f, 0.3f) * Mathf.Sqrt(size);
                go.GetComponent<Renderer>().sharedMaterial = UpgradeMat != null ? UpgradeMat : SparkMat;
                var e = go.AddComponent<Explosion>();
                e.kind = Kind.Rise; e.life = Random.Range(0.9f, 1.4f);
                e.velocity = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)) * 2.5f + Vector3.up * Random.Range(3f, 5.5f);
                e.size = r;
            }
            SpawnFireballColored(pos + Vector3.up * size * 0.4f, size, UpgradeMat);
        }

        static void SpawnFireballColored(Vector3 pos, float size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(go.GetComponent<Collider>());
            go.name = "UpgradeFlash";
            go.transform.position = pos;
            go.GetComponent<Renderer>().sharedMaterial = mat != null ? mat : SparkMat;
            var e = go.AddComponent<Explosion>();
            e.kind = Kind.Fireball; e.life = 0.5f; e.size = size * 1.2f;
            go.transform.localScale = Vector3.one * 0.2f;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = t / life;
            if (kind == Kind.Rise)
            {
                transform.position += velocity * Time.deltaTime;
                transform.Rotate(200 * Time.deltaTime, 300 * Time.deltaTime, 0);
                transform.localScale *= 1f - Time.deltaTime * 0.6f;
                if (t >= life) Destroy(gameObject);
                return;
            }
            if (kind == Kind.Fireball)
            {
                transform.localScale = Vector3.one * size * Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) + Vector3.one * 0.05f;
                transform.Rotate(90 * Time.deltaTime, 140 * Time.deltaTime, 0);
            }
            else
            {
                velocity += Physics.gravity * Time.deltaTime;
                transform.position += velocity * Time.deltaTime;
                if (transform.position.y < 0.1f) { velocity = Vector3.zero; transform.position = new Vector3(transform.position.x, 0.1f, transform.position.z); }
                transform.Rotate(300 * Time.deltaTime, 0, 200 * Time.deltaTime);
            }
            if (t >= life) Destroy(gameObject);
        }
    }
}
