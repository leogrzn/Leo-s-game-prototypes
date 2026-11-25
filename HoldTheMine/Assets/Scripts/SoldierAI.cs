using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace HoldTheMine
{
    public enum SoldierMode { Autonomous, Moving, Holding }

    /// <summary>
    /// Friendly soldier or tower.
    /// Autonomous soldiers find, approach and engage enemies near their post. Ordered soldiers walk to a point, then hold it and fight from there.
    /// Towers always hold.
    /// </summary>
    public class SoldierAI : MonoBehaviour
    {
        public static readonly List<SoldierAI> All = new List<SoldierAI>();

        public bool isTower;
        public Transform muzzle;
        public Transform turretPivot;

        public SoldierMode Mode { get; private set; } = SoldierMode.Autonomous;
        public Vector3 HoldPoint { get; private set; }
        public float Damage => damage;
        public float FireInterval => interval;
        public float Range => range;
        public float Spread => spread;
        public bool HasTarget => target != null;

        Health health;
        NavMeshAgent agent;
        LevelVisuals visuals;
        Health target;
        Vector3 home;
        float nextShot, nextRetarget, nextRepath, rallyUntil;
        float damage = 10, interval = 1, range = 20, spread = 5;
        GameObject rallyMarker;

        GameManager GM => GameManager.Instance;

        void Awake()
        {
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            visuals = GetComponent<LevelVisuals>();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Start()
        {
            home = transform.position;
            if (isTower) Mode = SoldierMode.Holding;
            if (agent) agent.speed = GM.config.soldierSpeed;
            nextShot = Time.time + Random.Range(0f, interval);
            if (muzzle == null) muzzle = transform;
        }

        /// <summary>Called by Upgradeable when the unit spawns or levels up.</summary>
        public void ApplyLevel(int level)
        {
            var c = GM.config;
            int i = Mathf.Clamp(level - 1, 0, 2);
            if (isTower)
            {
                health.SetMaxKeepDamage(c.tower.health[i]);
                damage = c.tower.damage[i]; interval = c.tower.fireInterval[i]; range = c.tower.range[i]; spread = c.tower.spread;
            }
            else
            {
                health.SetMaxKeepDamage(c.soldier.health[i]);
                damage = c.soldier.damage[i]; interval = c.soldier.fireInterval[i]; range = c.soldier.range[i]; spread = c.soldier.spread[i];
            }
            if (visuals)
            {
                var m = visuals.Muzzle(level); if (m) muzzle = m;
                var p = visuals.Pivot(level); if (p) turretPivot = p;
            }
            if (rallyMarker) rallyMarker.transform.localPosition = Vector3.up * (health.TopY - transform.position.y + 0.6f);
        }

        // ---------------- Orders ----------------
        public void OrderMove(Vector3 p)
        {
            if (isTower || agent == null || !agent.isOnNavMesh) return;
            if (!NavMesh.SamplePosition(p, out var hit, 3f, NavMesh.AllAreas)) return;
            HoldPoint = hit.position;
            Mode = SoldierMode.Moving;
            agent.isStopped = false;
            agent.SetDestination(HoldPoint);
        }

        /// <summary>Back to autonomous mode; the current position becomes his post.</summary>
        public void Release()
        {
            if (isTower) return;
            Mode = SoldierMode.Autonomous;
            home = transform.position;
        }

        public void MoveTo(Vector3 p) => OrderMove(p);

        public void Rally(float duration)
        {
            rallyUntil = Time.time + duration;
            if (rallyMarker == null)
            {
                rallyMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                DestroyImmediate(rallyMarker.GetComponent<Collider>());
                rallyMarker.GetComponent<Renderer>().sharedMaterial = Explosion.SparkMat;
                rallyMarker.transform.SetParent(transform, false);
                rallyMarker.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            }
            rallyMarker.transform.localPosition = Vector3.up * (health.TopY - transform.position.y + 0.6f);
            rallyMarker.SetActive(true);
        }

        public bool Rallied => Time.time < rallyUntil;

        // ---------------- Loop ----------------
        void Update()
        {
            if (GM == null || !GM.IsPlaying || health.IsDead) return;
            if (rallyMarker && rallyMarker.activeSelf)
            {
                if (!Rallied) rallyMarker.SetActive(false);
                else rallyMarker.transform.Rotate(0, 180 * Time.deltaTime, 0);
            }

            if (Time.time >= nextRetarget || target == null || target.IsDead)
            {
                nextRetarget = Time.time + 0.3f;
                target = Mode == SoldierMode.Autonomous && !isTower ? FindTargetAutonomous() : FindTargetInRange();
            }

            bool canShoot = false;
            float dist = 0;
            if (target != null)
            {
                dist = Vector3.Distance(target.transform.position, transform.position);
                canShoot = dist <= range && Combat.HasLineOfSight(muzzle.position, target.AimPoint, Team.Friendly, target);
            }

            if (agent != null && agent.isOnNavMesh) Move(canShoot, dist);

            if (canShoot)
            {
                Vector3 look = target.transform.position - transform.position; look.y = 0;
                Transform rotor = turretPivot != null ? turretPivot : transform;
                if (look.sqrMagnitude > 0.01f)
                    rotor.rotation = Quaternion.Slerp(rotor.rotation, Quaternion.LookRotation(look), 12f * Time.deltaTime);
                if (Time.time >= nextShot)
                {
                    float rate = GM.FireRateMultiplier * (Rallied ? GM.config.rallyFireRateMultiplier : 1f);
                    nextShot = Time.time + interval / Mathf.Max(0.1f, rate);
                    Vector3 dir = target.AimPoint - muzzle.position;
                    Combat.Fire(muzzle.position, dir, range + 2f, damage, Team.Friendly, spread * GM.SpreadMultiplier,
                        isTower ? new Color(1f, 0.7f, 0.3f) : new Color(0.6f, 1f, 0.5f));
                }
            }
            if (agent != null) agent.updateRotation = !canShoot;
        }

        void Move(bool canShoot, float dist)
        {
            switch (Mode)
            {
                case SoldierMode.Moving:
                    Vector3 off = transform.position - HoldPoint; off.y = 0;
                    if (!agent.pathPending && off.magnitude <= agent.stoppingDistance + 0.6f)
                    {
                        Mode = SoldierMode.Holding;
                        agent.ResetPath();
                    }
                    break;
                case SoldierMode.Holding:
                    // Get back on the spot if pushed off it.
                    if (!agent.hasPath && !agent.pathPending && (transform.position - HoldPoint).sqrMagnitude > 1.5f * 1.5f) agent.SetDestination(HoldPoint);
                    break;
                case SoldierMode.Autonomous:
                    if (Time.time < nextRepath) break;
                    nextRepath = Time.time + 0.5f;
                    float leash = GM.config.soldierLeashRadius;
                    if (target != null && !canShoot && Vector3.Distance(target.transform.position, home) < leash + range)
                    {
                        // Approach: go towards the enemy, but stay on the leash.
                        Vector3 to = target.transform.position;
                        Vector3 fromHome = to - home;
                        if (fromHome.magnitude > leash) to = home + fromHome.normalized * leash;
                        agent.isStopped = false;
                        agent.SetDestination(to);
                    }
                    else if (target != null && canShoot)
                    {
                        if (agent.hasPath) agent.ResetPath();
                    }
                    else if ((transform.position - home).sqrMagnitude > 2f * 2f)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(home);
                    }
                    break;
            }
        }

        bool Visible(Health h)
        {
            var e = h.GetComponent<EnemyAI>();
            return e == null || !e.Concealed;
        }

        Health FindTargetInRange()
        {
            Health best = null;
            float bestD = range;
            Vector3 eye = muzzle.position;
            foreach (var h in Health.All)
            {
                if (h.IsDead || h.team != Team.Enemy) continue;
                float d = Vector3.Distance(h.transform.position, transform.position);
                if (d >= bestD || !Visible(h)) continue;
                if (!Combat.HasLineOfSight(eye, h.AimPoint, Team.Friendly, h)) continue;
                best = h; bestD = d;
            }
            return best;
        }

        Health FindTargetAutonomous()
        {
            // Prefer something we can shoot right now, otherwise the nearest visible enemy near our post.
            var inRange = FindTargetInRange();
            if (inRange != null) return inRange;
            Health best = null;
            float bestD = GM.config.soldierDetectRange;
            float leash = GM.config.soldierLeashRadius + range;
            foreach (var h in Health.All)
            {
                if (h.IsDead || h.team != Team.Enemy || !Visible(h)) continue;
                float d = Vector3.Distance(h.transform.position, transform.position);
                if (d >= bestD) continue;
                if (Vector3.Distance(h.transform.position, home) > leash) continue;
                best = h; bestD = d;
            }
            return best;
        }
    }
}
