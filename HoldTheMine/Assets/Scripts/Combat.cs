using System.Collections.Generic;
using UnityEngine;

namespace HoldTheMine
{
    /// <summary>Hitscan shooting. Bullets are stopped by rocks, trees, buildings and anything of the other team.</summary>
    public static class Combat
    {
        static readonly RaycastHit[] Hits = new RaycastHit[32];
        static readonly HitDistanceComparer Comparer = new HitDistanceComparer();

        class HitDistanceComparer : IComparer<RaycastHit>
        {
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }

        public static Health Fire(Vector3 origin, Vector3 dir, float range, float damage, Team shooterTeam, float spreadDeg, Color tracerColor)
        {
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            dir.Normalize();
            if (spreadDeg > 0) dir = Quaternion.AngleAxis(Random.Range(-spreadDeg, spreadDeg), Vector3.up) * dir;

            int n = Physics.RaycastNonAlloc(origin, dir, Hits, range, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(Hits, 0, n, Comparer);
            Vector3 end = origin + dir * range;
            Health result = null;
            for (int i = 0; i < n; i++)
            {
                var hit = Hits[i];
                var h = hit.collider.GetComponentInParent<Health>();
                if (h != null && (h.team == shooterTeam || h.IsDead)) continue;
                end = hit.point;
                if (h != null)
                {
                    h.TakeDamage(damage);
                    result = h;
                }
                Explosion.SpawnSpark(hit.point);
                break;
            }
            Tracer.Spawn(origin, end, tracerColor);
            return result;
        }

        /// <summary>True if nothing solid (rock, tree, building, enemy barricade) is between two points.</summary>
        public static bool HasLineOfSight(Vector3 from, Vector3 to, Team viewerTeam, Health target)
        {
            Vector3 d = to - from;
            float dist = d.magnitude;
            if (dist < 0.01f) return true;
            int n = Physics.RaycastNonAlloc(from, d / dist, Hits, dist, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = Hits[i].collider.GetComponentInParent<Health>();
                if (h == target) continue;
                if (h != null && h.team == viewerTeam) continue;
                if (h != null && h.kind != UnitKind.Barricade && h.kind != UnitKind.Mine && h.kind != UnitKind.Tower) continue; // units don't block LOS checks
                return false;
            }
            return true;
        }

        /// <summary>Closest point on a target's colliders, so big things (the mine) can be attacked from their edge.</summary>
        public static Vector3 ClosestPoint(Health target, Vector3 from)
        {
            var col = target.GetComponent<Collider>();
            if (col == null) col = target.GetComponentInChildren<Collider>();
            if (col == null) return target.transform.position;
            Vector3 p = col.ClosestPoint(from);
            p.y = target.transform.position.y;
            return p;
        }

        public static float DistanceTo(Health target, Vector3 from)
        {
            Vector3 p = ClosestPoint(target, from);
            p.y = from.y = 0;
            return Vector3.Distance(p, from);
        }
    }
}
