using UnityEngine;
using UnityEngine.AI;

namespace HoldTheMine
{
    /// <summary>Walks between the mine and the ore piles, digs, and drops gold on the pile at the mine entrance.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MinerAI : MonoBehaviour
    {
        public GameObject carryVisual;

        enum State { ToPile, Digging, Returning, Hiding }
        State state;
        NavMeshAgent agent;
        Health health;
        Transform pile;
        float timer, carried;
        float walkSpeed = 3.5f, digSpeed = 1f;

        public string StateName => state switch
        {
            State.ToPile => "Walking to ore",
            State.Digging => "Digging",
            State.Returning => carried > 0 ? $"Carrying {carried:0} gold" : "Returning",
            _ => "Taking cover"
        };
        public float DigSpeed => digSpeed;
        public float WalkSpeed => walkSpeed;

        GameManager GM => GameManager.Instance;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            health.Damaged += OnDamaged;
        }

        /// <summary>Called by Upgradeable when spawned or levelled up.</summary>
        public void ApplyLevel(int level)
        {
            var m = GM.config.miner;
            int i = Mathf.Clamp(level - 1, 0, 2);
            health.SetMaxKeepDamage(m.health[i]);
            walkSpeed = m.walkSpeed[i];
            digSpeed = m.digSpeed[i];
            agent.speed = state == State.Hiding ? walkSpeed * 1.5f : walkSpeed;
        }

        void Start()
        {
            if (carryVisual) carryVisual.SetActive(false);
            GoToPile();
        }

        void OnDamaged(float _)
        {
            if (state == State.Hiding) return;
            state = State.Hiding;
            timer = 5f;
            agent.speed = walkSpeed * 1.5f;
            SetDest(GM.mineEntrance.position);
        }

        void GoToPile()
        {
            var piles = GM.orePiles;
            pile = piles[Random.Range(0, piles.Length)];
            state = State.ToPile;
            Vector2 off = Random.insideUnitCircle * 1.5f;
            SetDest(pile.position + new Vector3(off.x, 0, off.y) + (GM.mineEntrance.position - pile.position).normalized * 1.8f);
        }

        void SetDest(Vector3 p)
        {
            if (agent.isOnNavMesh && NavMesh.SamplePosition(p, out var hit, 3f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
        }

        bool Arrived => agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f;

        void Update()
        {
            if (GM == null || !GM.IsPlaying || health.IsDead) return;
            switch (state)
            {
                case State.ToPile:
                    if (Arrived) { state = State.Digging; timer = GM.config.digTime / Mathf.Max(0.1f, digSpeed); }
                    break;
                case State.Digging:
                    timer -= Time.deltaTime;
                    // little digging bob
                    transform.Rotate(0, Mathf.Sin(Time.time * 12f) * 40f * Time.deltaTime, 0);
                    if (timer <= 0)
                    {
                        carried = GM.config.goldPerTrip * GM.DigMultiplier;
                        if (carryVisual) carryVisual.SetActive(true);
                        state = State.Returning;
                        SetDest(GM.goldPile.position + (GM.goldPile.position - GM.mine.transform.position).normalized * 1.2f);
                    }
                    break;
                case State.Returning:
                    if (Arrived)
                    {
                        GM.GoldWaiting += carried;
                        carried = 0;
                        if (carryVisual) carryVisual.SetActive(false);
                        GoToPile();
                    }
                    break;
                case State.Hiding:
                    timer -= Time.deltaTime;
                    if (timer <= 0 && Time.time - health.LastDamageTime > 4f)
                    {
                        agent.speed = walkSpeed;
                        if (carried > 0) { state = State.Returning; SetDest(GM.goldPile.position); }
                        else GoToPile();
                    }
                    break;
            }
        }
    }
}
