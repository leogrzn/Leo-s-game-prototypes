using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace HoldTheMine
{
    public enum GameState { Menu, Playing, Paused, Won, Lost }

    /// <summary>Owns the run: economy, food, morale, win/lose and restart.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        static bool skipMenuOnLoad;

        [Header("Config")]
        public GameConfig config;

        [Header("Scene references")]
        public PlayerController player;
        public Health mine;
        public Transform mineEntrance;
        public Transform goldPile;
        public Transform[] orePiles;
        public Transform[] soldierStartPoints;
        public Transform unitsRoot;
        public UIManager ui;
        public WaveManager waves;
        public TruckDispatcher trucks;

        [Header("Prefabs")]
        public GameObject soldierPrefab;
        public GameObject minerPrefab;
        public GameObject towerPrefab;
        public GameObject barricadePrefab;

        [Header("FX materials")]
        public Material tracerMat, fireMat, sparkMat, debrisMat, grenadeMat, upgradeMat;

        public GameState State { get; private set; } = GameState.Menu;
        public int Money { get; private set; }
        public float GoldWaiting { get; set; }
        public float Food { get; private set; }
        public float Morale { get; private set; }
        public int PendingFoodOrders { get; private set; }
        public int EnemiesKilled { get; private set; }

        bool starvingWarned;
        Transform goldPileVisual;

        void Awake()
        {
            Instance = this;
            Tracer.Mat = tracerMat;
            Explosion.FireMat = fireMat;
            Explosion.SparkMat = sparkMat;
            Explosion.DebrisMat = debrisMat;
            Explosion.UpgradeMat = upgradeMat;
            Money = config.startMoney;
            Food = config.startFood;
            Morale = config.startMorale;
            mineUpgrade = mine.GetComponent<Upgradeable>();
            if (goldPile != null && goldPile.childCount > 0) goldPileVisual = goldPile.GetChild(0);
            mine.Damaged += _ =>
            {
                AddMorale(config.moraleMineHit);
                if (Time.time > nextMineWarning)
                {
                    nextMineWarning = Time.time + 12f;
                    ui.Alert("The mine is under attack!", UIManager.Bad, mine.transform);
                }
            };
        }

        float nextMineWarning;
        bool foodLowWarned;
        Upgradeable mineUpgrade;

        public int MineLevel => mineUpgrade != null ? mineUpgrade.Level : 1;
        public int MaxMiners => config.mine.maxMiners[Mathf.Clamp(MineLevel - 1, 0, 2)];
        public float FoodPerMinute => (Health.Count(UnitKind.Soldier) + Health.Count(UnitKind.Miner)) * config.foodPerUnitPerMinute + config.playerFoodPerMinute;

        void Start()
        {
            for (int i = 0; i < config.startMiners; i++) SpawnMiner();
            for (int i = 0; i < config.startSoldiers && i < soldierStartPoints.Length; i++)
                SpawnSoldier(soldierStartPoints[i].position);

            if (skipMenuOnLoad) { skipMenuOnLoad = false; StartGame(); }
            else SetState(GameState.Menu);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------------- Flow ----------------
        public void StartGame()
        {
            SetState(GameState.Playing);
            ui.ShowMessage("Hold the mine! First raid incoming soon.", UIManager.Info);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void Restart()
        {
            skipMenuOnLoad = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().name : "HoldTheMine");
        }

        public void QuitToMenu()
        {
            skipMenuOnLoad = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void SetState(GameState s)
        {
            State = s;
            Time.timeScale = s == GameState.Playing ? 1f : 0f;
            ui.OnStateChanged(s);
        }

        public bool IsPlaying => State == GameState.Playing;

        public void Win()
        {
            if (State != GameState.Playing) return;
            SetState(GameState.Won);
        }

        public void Lose(string reason)
        {
            if (State == GameState.Won || State == GameState.Lost) return;
            ui.SetGameOverReason(reason);
            SetState(GameState.Lost);
        }

        // ---------------- Simulation ----------------
        void Update()
        {
            if (State != GameState.Playing) return;
            float dt = Time.deltaTime;

            int eaters = Health.Count(UnitKind.Soldier) + Health.Count(UnitKind.Miner);
            float eat = (eaters * config.foodPerUnitPerMinute + config.playerFoodPerMinute) / 60f * dt;
            Food = Mathf.Max(0, Food - eat);

            if (Food <= 0)
            {
                AddMorale(-config.moraleStarvingDrain * dt);
                if (!starvingWarned) { starvingWarned = true; ui.Alert("FOOD RAN OUT! Morale is crashing — order food (key 4).", UIManager.Bad); }
            }
            else
            {
                starvingWarned = false;
                float minutesLeft = Food / Mathf.Max(0.01f, FoodPerMinute);
                if (minutesLeft < 1f && PendingFoodOrders == 0 && !foodLowWarned)
                {
                    foodLowWarned = true;
                    ui.Alert("Food is running out (under 1 minute left)! Order food (key 4).", UIManager.Warn);
                }
                if (minutesLeft > 1.5f) foodLowWarned = false;
                Morale = Mathf.MoveTowards(Morale, config.moraleDriftTarget, config.moraleDriftRate * dt);
            }

            if (goldPileVisual != null)
            {
                float s = Mathf.Clamp(GoldWaiting / 60f, 0.05f, 1.6f);
                goldPileVisual.localScale = new Vector3(1.6f * Mathf.Sqrt(s), s, 1.6f * Mathf.Sqrt(s));
                goldPileVisual.localPosition = new Vector3(0, s * 0.5f, 0);
            }
        }

        public void AddMorale(float delta) => Morale = Mathf.Clamp(Morale + delta, 0, 100);

        public float MoraleDigMultiplier => config.MoraleLerp(config.digMultiplierAtZeroMorale, 1f, config.digMultiplierAtFullMorale, Morale);
        public float MineDigMultiplier => config.mine.digMultiplier[Mathf.Clamp(MineLevel - 1, 0, 2)];
        public float DigMultiplier => MoraleDigMultiplier * MineDigMultiplier;
        public float FireRateMultiplier => config.MoraleLerp(config.soldierFireRateAtZeroMorale, 1f, config.soldierFireRateAtFullMorale, Morale);
        public float SpreadMultiplier => config.MoraleLerp(config.soldierSpreadAtZeroMorale, 1f, config.soldierSpreadAtFullMorale, Morale);

        public bool NearMine(Vector3 p) => mine != null && Vector3.Distance(p, mine.transform.position) < config.nearMineRadius;

        public void OnUnitDied(Health h)
        {
            switch (h.kind)
            {
                case UnitKind.Player:
                    Lose("You were killed.");
                    break;
                case UnitKind.Mine:
                    Lose("The mine was destroyed.");
                    break;
                case UnitKind.Soldier:
                case UnitKind.Miner:
                    if (NearMine(h.transform.position)) AddMorale(config.moraleFriendlyDeathNearMine);
                    ui.Alert(h.kind == UnitKind.Miner ? "A miner was killed!" : "A soldier went down!", UIManager.Bad, null, h.transform.position);
                    break;
                case UnitKind.Tower:
                    ui.Alert("A tower was destroyed!", UIManager.Bad, null, h.transform.position);
                    break;
                case UnitKind.Barricade:
                    ui.ShowMessage("A barricade was broken.", UIManager.Warn);
                    break;
                case UnitKind.Enemy:
                    EnemiesKilled++;
                    var e = h.GetComponent<EnemyAI>();
                    if (e != null) Money += config.GetEnemy(e.type).killReward;
                    break;
            }
        }

        public void OnExplosion(Vector3 pos)
        {
            if (NearMine(pos)) AddMorale(config.moraleExplosionNearMine);
        }

        // ---------------- Economy ----------------
        public bool TrySpend(int cost, string what)
        {
            if (Money < cost)
            {
                ui.ShowMessage($"Not enough money for {what} (${cost}).", UIManager.Warn);
                return false;
            }
            Money -= cost;
            return true;
        }

        public void AddMoney(int amount) => Money += amount;

        public void AddFood(float amount) => Food += amount;

        public void OrderFood()
        {
            if (!TrySpend(config.foodOrderCost, "food")) return;
            PendingFoodOrders++;
            ui.ShowMessage($"Food ordered (+{config.foodPerOrder:0}). It comes on the next food truck.", UIManager.Info);
        }

        /// <summary>Called by the food truck when it unloads. Returns the food delivered.</summary>
        public float DeliverFood()
        {
            float amount = PendingFoodOrders * config.foodPerOrder;
            PendingFoodOrders = 0;
            Food += amount;
            AddMorale(config.moraleFoodDelivered);
            return amount;
        }

        public void LoseFoodOrders() => PendingFoodOrders = 0;

        public void HireMiner()
        {
            if (Health.Count(UnitKind.Miner) >= MaxMiners)
            {
                ui.ShowMessage($"The mine only has room for {MaxMiners} miners. Upgrade the mine for more.", UIManager.Warn);
                return;
            }
            if (!TrySpend(config.minerCost, "a miner")) return;
            SpawnMiner();
            ui.ShowMessage("New miner hired.", UIManager.Good);
        }

        public void SpawnMiner()
        {
            Vector3 p = mineEntrance.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-1.5f, 0f));
            if (NavMesh.SamplePosition(p, out var hit, 4f, NavMesh.AllAreas)) p = hit.position;
            Instantiate(minerPrefab, p, Quaternion.identity, unitsRoot);
        }

        public SoldierAI SpawnSoldier(Vector3 p)
        {
            if (NavMesh.SamplePosition(p, out var hit, 4f, NavMesh.AllAreas)) p = hit.position;
            var go = Instantiate(soldierPrefab, p, Quaternion.identity, unitsRoot);
            return go.GetComponent<SoldierAI>();
        }

        public void Rallied() => AddMorale(config.moraleRally);
    }
}
