using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace HoldTheMine
{
    /// <summary>
    /// Raider AI. Rifleman: advance and shoot from mid range. Raider: rush the miners. Ambusher: hunt trucks on the road.
    /// Grenadier: lob bombs at barricades, towers and groups. Heavy: slow armoured gunner. All stop at barricades and attack them.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        public static readonly List<EnemyAI> All = new List<EnemyAI>();
        public static float ForestStartX = 14f;

        public EnemyType type;
        public Transform muzzle;

        public bool Concealed { get; private set; }
        public Vector3 AmbushPoint { get; set; }

        NavMeshAgent agent;
        Health health;
        EnemyStats stats;
        Health target;
        float nextRetarget, nextAttack, nextRepath, lastShot = -100f, nextConcealCheck;
        Renderer[] renderers;
        Vector3 lastDest = Vector3.positiveInfinity;
        bool attacking;

        GameManager GM => GameManager.Instance;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            renderers = GetComponentsInChildren<Renderer>();
            health.Damaged += _ => lastShot = Mathf.Max(lastShot, Time.time - GM.config.forestRevealAfterShot * 0.5f);
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void Init(EnemyStats s, float healthMultiplier)
        {
            stats = s;
            health.SetMax(s.health * healthMultiplier);
            health.armor = s.armor;
            agent.speed = s.moveSpeed;
            agent.acceleration = 20f;
            agent.angularSpeed = 540f;
            nextAttack = Time.time + Random.Range(0.5f, s.attackInterval + 0.5f);
            if (muzzle == null) muzzle = transform;
        }

        void Update()
        {
            if (GM == null || !GM.IsPlaying || health.IsDead || stats == null) return;
            if (!agent.isOnNavMesh) return;

            if (Time.time >= nextConcealCheck) { nextConcealCheck = Time.time + 0.2f; UpdateConcealment(); }

            if (Time.time >= nextRetarget || target == null || target.IsDead)
            {
                nextRetarget = Time.time + 0.4f;
                target = ChooseTarget();
            }

            if (target == null)
            {
                // Ambushers wait by the road when there is nothing to shoot.
                MoveTowards(AmbushPoint, 1f);
                attacking = false;
                return;
            }

            Vector3 aimFrom = muzzle.position;
            float dist = Combat.DistanceTo(target, transform.position);
            bool inRange = dist <= stats.attackRange;
            bool los = type == EnemyType.Grenadier || type == EnemyType.Raider || Combat.HasLineOfSight(aimFrom, target.AimPoint, Team.Enemy, target);

            if (inRange && los)
            {
                attacking = true;
                agent.isStopped = true;
                agent.updateRotation = false;
                Vector3 look = Combat.ClosestPoint(target, transform.position) - transform.position; look.y = 0;
                if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 10f * Time.deltaTime);
                if (Time.time >= nextAttack) Attack();
            }
            else
            {
                attacking = false;
                agent.isStopped = false;
                agent.updateRotation = true;
                MoveTowards(Combat.ClosestPoint(target, transform.position), 0.5f);
            }
        }

        void MoveTowards(Vector3 p, float minChange)
        {
            agent.isStopped = false;
            if (Time.time < nextRepath && (p - lastDest).sqrMagnitude < minChange * minChange) return;
            nextRepath = Time.time + 0.5f;
            lastDest = p;
            if (NavMesh.SamplePosition(p, out var hit, 4f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
        }

        void Attack()
        {
            nextAttack = Time.time + stats.attackInterval * Random.Range(0.85f, 1.15f);
            lastShot = Time.time;
            switch (type)
            {
                case EnemyType.Raider:
                    target.TakeDamage(stats.damage);
                    Explosion.SpawnSpark(target.AimPoint);
                    break;
                case EnemyType.Grenadier:
                    Vector3 to = Combat.ClosestPoint(target, transform.position) + new Vector3(Random.Range(-1.2f, 1.2f), 0, Random.Range(-1.2f, 1.2f));
                    Grenade.Throw(GM.grenadeMat, muzzle.position + Vector3.up * 0.5f, to, GM.config.grenadeFlightTime, stats.damage, GM.config.grenadeRadius, Team.Enemy);
                    break;
                default:
                    Vector3 dir = target.AimPoint - muzzle.position;
                    if (target.kind == UnitKind.Mine) dir = Combat.ClosestPoint(target, transform.position) + Vector3.up * 1.5f - muzzle.position;
                    Combat.Fire(muzzle.position, dir, stats.attackRange + 4f, stats.damage, Team.Enemy, stats.spread,
                        type == EnemyType.Heavy ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.45f, 0.35f));
                    break;
            }
        }

        // ---------------- Targeting ----------------
        Health ChooseTarget()
        {
            Vector3 pos = transform.position;

            // Barricades in the way stop everyone: attack the barricade.
            var barricade = Health.FindNearest(pos, 2.8f, Team.Friendly, h => h.kind == UnitKind.Barricade);
            if (barricade != null) return barricade;

            switch (type)
            {
                case EnemyType.Raider:
                {
                    var miner = Health.FindNearest(pos, 200f, Team.Friendly, h => h.kind == UnitKind.Miner);
                    if (miner != null) return miner;
                    break;
                }
                case EnemyType.Ambusher:
                {
                    var truck = Health.FindNearest(pos, 200f, Team.Friendly, h => h.kind == UnitKind.Truck);
                    if (truck != null) return truck;
                    var near = Health.FindNearest(pos, stats.aggroRange, Team.Friendly, IsUnitOrStructure);
                    return near; // null → wait at the ambush point
                }
                case EnemyType.Grenadier:
                {
                    var structure = Health.FindNearest(pos, stats.aggroRange * 1.3f, Team.Friendly, h => h.kind == UnitKind.Barricade || h.kind == UnitKind.Tower);
                    if (structure != null) return structure;
                    var group = BiggestGroup(pos, stats.aggroRange);
                    if (group != null) return group;
                    break;
                }
            }

            var nearest = Health.FindNearest(pos, stats.aggroRange, Team.Friendly, IsUnitOrStructure);
            if (nearest != null) return nearest;
            return GM.mine != null && !GM.mine.IsDead ? GM.mine : null;
        }

        static bool IsUnitOrStructure(Health h) =>
            h.kind == UnitKind.Player || h.kind == UnitKind.Soldier || h.kind == UnitKind.Miner || h.kind == UnitKind.Tower || h.kind == UnitKind.Truck;

        static Health BiggestGroup(Vector3 pos, float range)
        {
            Health best = null;
            int bestCount = 1;
            foreach (var h in Health.All)
            {
                if (h.IsDead || h.team != Team.Friendly || !IsUnitOrStructure(h)) continue;
                if (Vector3.Distance(h.transform.position, pos) > range) continue;
                int count = 0;
                foreach (var o in Health.All)
                    if (!o.IsDead && o.team == Team.Friendly && IsUnitOrStructure(o) && Vector3.Distance(o.transform.position, h.transform.position) < 4.5f) count++;
                if (count > bestCount || best == null) { bestCount = count; best = h; }
            }
            return best;
        }

        // ---------------- Forest concealment ----------------
        void UpdateConcealment()
        {
            bool hidden = false;
            if (transform.position.x > ForestStartX && Time.time - lastShot > GM.config.forestRevealAfterShot)
            {
                hidden = true;
                float r = GM.config.forestRevealDistance;
                if (GM.player != null && Vector3.Distance(GM.player.transform.position, transform.position) < r) hidden = false;
                else
                {
                    foreach (var s in SoldierAI.All)
                        if (Vector3.Distance(s.transform.position, transform.position) < r * (s.isTower ? 1.4f : 1f)) { hidden = false; break; }
                }
            }
            if (hidden != Concealed)
            {
                Concealed = hidden;
                foreach (var rd in renderers) if (rd) rd.enabled = !hidden;
            }
        }

        public bool IsAttacking => attacking;
    }
}
