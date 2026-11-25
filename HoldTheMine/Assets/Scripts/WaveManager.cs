using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace HoldTheMine
{
    /// <summary>Counts down to each wave, builds its composition from GameConfig, spawns it and detects when it is beaten.</summary>
    public class WaveManager : MonoBehaviour
    {
        public Transform[] desertSpawns;
        public Transform[] forestSpawns;
        public Transform[] roadSpawns;
        public Transform[] ambushPoints;
        public Transform enemiesRoot;
        public GameObject[] enemyPrefabs = new GameObject[5]; // indexed by EnemyType

        public int CurrentWave { get; private set; }
        public float Countdown { get; private set; }
        public bool AllWavesLaunched => CurrentWave >= Cfg.totalWaves;
        public int Alive => aliveThisRun.Count;

        readonly HashSet<Health> aliveThisRun = new HashSet<Health>();
        readonly Dictionary<int, int> remainingPerWave = new Dictionary<int, int>();
        int spawning;
        List<SpawnOrder> nextOrders;

        GameConfig Cfg => GameManager.Instance.config;
        GameManager GM => GameManager.Instance;

        void Start()
        {
            Countdown = Cfg.firstWaveDelay;
            nextOrders = BuildWave(1);
        }

        /// <summary>Where the next wave comes from, e.g. "the desert and the road".</summary>
        public string NextWaveSides => nextOrders == null ? "" : DescribeSides(nextOrders);

        string DescribeSides(List<SpawnOrder> orders)
        {
            bool d = false, f = false, r = false;
            foreach (var o in orders)
            {
                if (System.Array.IndexOf(desertSpawns, o.at) >= 0) d = true;
                else if (System.Array.IndexOf(forestSpawns, o.at) >= 0) f = true;
                else r = true;
            }
            var parts = new List<string>();
            if (d) parts.Add("desert");
            if (f) parts.Add("forest");
            if (r) parts.Add("road");
            return string.Join(" + ", parts);
        }

        void PointAtSides(List<SpawnOrder> orders, string text, Color c)
        {
            bool d = false, f = false, r = false;
            foreach (var o in orders)
            {
                if (System.Array.IndexOf(desertSpawns, o.at) >= 0) d = true;
                else if (System.Array.IndexOf(forestSpawns, o.at) >= 0) f = true;
                else r = true;
            }
            GM.ui.ShowMessage(text, c);
            if (d) GM.ui.AddThreat(Average(desertSpawns), null, c, 8f);
            if (f) GM.ui.AddThreat(Average(forestSpawns), null, c, 8f);
            if (r) GM.ui.AddThreat(Average(roadSpawns), null, c, 8f);
        }

        static Vector3 Average(Transform[] ts)
        {
            Vector3 s = Vector3.zero;
            foreach (var t in ts) s += t.position;
            return s / Mathf.Max(1, ts.Length);
        }

        void Update()
        {
            if (GM == null || !GM.IsPlaying) return;

            if (!AllWavesLaunched)
            {
                Countdown -= Time.deltaTime;
                if (Countdown <= 10f && Countdown + Time.deltaTime > 10f)
                    PointAtSides(nextOrders, $"Wave {CurrentWave + 1} incoming in 10 seconds from {NextWaveSides}!", UIManager.Warn);
                if (Countdown <= 0f) LaunchNextWave();
            }
            else if (spawning == 0 && aliveThisRun.Count == 0)
            {
                GM.Win();
            }
        }

        /// <summary>Starts the next wave immediately (also used by the "call wave early" button).</summary>
        public void LaunchNextWave()
        {
            if (AllWavesLaunched) return;
            CurrentWave++;
            Countdown = Cfg.waveInterval;
            var list = nextOrders ?? BuildWave(CurrentWave);
            nextOrders = BuildWave(CurrentWave + 1);
            remainingPerWave[CurrentWave] = list.Count;
            PointAtSides(list, $"WAVE {CurrentWave}/{Cfg.totalWaves}: {list.Count} raiders from {DescribeSides(list)}!", UIManager.Bad);
            StartCoroutine(SpawnRoutine(CurrentWave, list));
        }

        struct SpawnOrder { public EnemyType type; public Transform at; }

        List<SpawnOrder> BuildWave(int w)
        {
            var orders = new List<SpawnOrder>();
            bool both = w >= Cfg.bothSidesFromWave;
            bool desertOnly = !both && w % 2 == 1;
            foreach (var s in Cfg.enemies)
            {
                if (w < s.firstWave) continue;
                int count = Mathf.FloorToInt(s.baseCount + s.countPerWave * (w - s.firstWave));
                for (int i = 0; i < count; i++)
                {
                    Transform at;
                    if (s.type == EnemyType.Ambusher) at = Pick(roadSpawns);
                    else if (s.type == EnemyType.Rifleman && w >= Cfg.roadAttacksFromWave && i % 4 == 3) at = Pick(roadSpawns);
                    else if (both) at = Random.value < 0.5f ? Pick(desertSpawns) : Pick(forestSpawns);
                    else at = desertOnly ? Pick(desertSpawns) : Pick(forestSpawns);
                    orders.Add(new SpawnOrder { type = s.type, at = at });
                }
            }
            // Shuffle so types are mixed
            for (int i = orders.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (orders[i], orders[j]) = (orders[j], orders[i]);
            }
            return orders;
        }

        static Transform Pick(Transform[] a) => a[Random.Range(0, a.Length)];

        IEnumerator SpawnRoutine(int wave, List<SpawnOrder> orders)
        {
            spawning++;
            float hpMult = 1f + Cfg.healthGrowthPerWave * (wave - 1);
            foreach (var o in orders)
            {
                Spawn(o.type, o.at.position, hpMult, wave);
                yield return new WaitForSeconds(Cfg.spawnInterval);
            }
            spawning--;
        }

        void Spawn(EnemyType type, Vector3 pos, float hpMult, int wave)
        {
            Vector2 off = Random.insideUnitCircle * 4f;
            pos += new Vector3(off.x, 0, off.y);
            if (!NavMesh.SamplePosition(pos, out var hit, 8f, NavMesh.AllAreas)) return;
            var go = Instantiate(enemyPrefabs[(int)type], hit.position, Quaternion.identity, enemiesRoot);
            var ai = go.GetComponent<EnemyAI>();
            ai.AmbushPoint = ClosestAmbushPoint(hit.position);
            ai.Init(Cfg.GetEnemy(type), hpMult);
            var h = go.GetComponent<Health>();
            aliveThisRun.Add(h);
            h.Died += dead =>
            {
                aliveThisRun.Remove(dead);
                if (remainingPerWave.TryGetValue(wave, out int left))
                {
                    left--;
                    remainingPerWave[wave] = left;
                    if (left <= 0) OnWaveCleared(wave);
                }
            };
        }

        Vector3 ClosestAmbushPoint(Vector3 p)
        {
            Transform best = ambushPoints[0];
            foreach (var a in ambushPoints)
                if ((a.position - p).sqrMagnitude < (best.position - p).sqrMagnitude) best = a;
            return best.position;
        }

        void OnWaveCleared(int wave)
        {
            if (GM == null || GM.State == GameState.Lost) return;
            GM.AddMorale(Cfg.moraleWaveSurvived);
            GM.ui.ShowMessage($"Wave {wave} beaten! Morale up.", UIManager.Good);
        }
    }
}
