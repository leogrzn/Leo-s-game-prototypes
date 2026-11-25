using System;
using UnityEngine;

namespace HoldTheMine
{
    public enum EnemyType { Rifleman, Raider, Ambusher, Grenadier, Heavy }

    [Serializable]
    public class EnemyStats
    {
        public EnemyType type;
        public float health = 60f;
        [Tooltip("0 = no armour, 0.5 = takes half damage")] [Range(0f, 0.9f)] public float armor = 0f;
        public float moveSpeed = 3.5f;
        public float damage = 8f;
        [Tooltip("Seconds between shots / swings / throws")] public float attackInterval = 1f;
        public float attackRange = 15f;
        [Tooltip("Inaccuracy in degrees")] public float spread = 6f;
        [Tooltip("How far it notices targets other than its main one")] public float aggroRange = 16f;
        public int killReward = 10;

        [Header("Wave composition")]
        [Tooltip("First wave in which this type appears")] public int firstWave = 1;
        public float baseCount = 3f;
        [Tooltip("Extra units per wave after firstWave")] public float countPerWave = 1f;
    }

    [Serializable]
    public class MineLevels
    {
        public int[] upgradeCost = { 450, 1000 };
        public float[] health = { 1200f, 1800f, 2600f };
        [Tooltip("Multiplies all gold dug")] public float[] digMultiplier = { 1f, 1.35f, 1.8f };
        [Tooltip("Max number of miners you can hire")] public int[] maxMiners = { 6, 9, 12 };
    }

    [Serializable]
    public class SoldierLevels
    {
        public int[] upgradeCost = { 120, 250 };
        public float[] health = { 100f, 150f, 215f };
        public float[] damage = { 12f, 17f, 24f };
        public float[] fireInterval = { 0.6f, 0.5f, 0.4f };
        [Tooltip("Inaccuracy in degrees (lower = more accurate)")] public float[] spread = { 5f, 3.5f, 2f };
        public float[] range = { 20f, 22f, 25f };
    }

    [Serializable]
    public class TowerLevels
    {
        public int[] upgradeCost = { 200, 420 };
        public float[] health = { 320f, 500f, 760f };
        public float[] damage = { 18f, 25f, 34f };
        public float[] range = { 28f, 33f, 39f };
        public float[] fireInterval = { 0.45f, 0.4f, 0.33f };
        public float spread = 3f;
    }

    [Serializable]
    public class MinerLevels
    {
        public int[] upgradeCost = { 60, 130 };
        [Tooltip("Divides dig time")] public float[] digSpeed = { 1f, 1.4f, 1.9f };
        public float[] walkSpeed = { 3.5f, 4.3f, 5.2f };
        public float[] health = { 60f, 75f, 95f };
    }

    [Serializable]
    public class BarricadeLevels
    {
        public int[] upgradeCost = { 40, 100 };
        public float[] health = { 260f, 550f, 950f };
        [Tooltip("Width multiplier")] public float[] width = { 1f, 1.25f, 1.5f };
    }

    [CreateAssetMenu(menuName = "Hold the Mine/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Start")]
        public int startMoney = 250;
        public float startFood = 45f;
        [Range(0, 100)] public float startMorale = 65f;
        public int startMiners = 4;
        public int startSoldiers = 2;

        [Header("Prices")]
        public int barricadeCost = 30;
        public int soldierCost = 100;
        public int towerCost = 220;
        public int minerCost = 80;
        public int foodOrderCost = 50;
        public float foodPerOrder = 40f;

        [Header("Gold")]
        [Tooltip("Gold units a miner carries back per trip at morale 50")] public float goldPerTrip = 5f;
        [Tooltip("Seconds a miner digs at an ore pile at morale 50")] public float digTime = 3f;
        [Tooltip("$ earned per unit of gold")] public float goldValue = 2f;
        [Tooltip("Dig multiplier at morale 0")] public float digMultiplierAtZeroMorale = 0.25f;
        [Tooltip("Dig multiplier at morale 100")] public float digMultiplierAtFullMorale = 2.2f;
        [Tooltip("If true, money is paid when the truck loads; if false, when it leaves the map safely (a destroyed truck loses its gold).")]
        public bool payGoldOnPickup = false;

        [Header("Trucks")]
        public float goldTruckFirstDelay = 30f;
        public float goldTruckInterval = 60f;
        public float foodTruckFirstDelay = 45f;
        public float foodTruckInterval = 75f;
        public float truckSpeed = 7f;
        public float truckHealth = 180f;
        public float truckLoadTime = 4f;

        [Header("Food")]
        [Tooltip("Food eaten per soldier/miner per minute")] public float foodPerUnitPerMinute = 1f;
        [Tooltip("Food the player character eats per minute")] public float playerFoodPerMinute = 0.5f;

        [Header("Morale")]
        public float moraleDriftTarget = 55f;
        [Tooltip("Morale points per second towards the drift target while fed")] public float moraleDriftRate = 0.25f;
        [Tooltip("Morale lost per second while food is zero")] public float moraleStarvingDrain = 2.5f;
        public float moraleFriendlyDeathNearMine = -6f;
        public float moraleExplosionNearMine = -3f;
        public float moraleMineHit = -0.3f;
        public float moraleTruckDestroyed = -8f;
        public float moraleFoodDelivered = 12f;
        public float moraleWaveSurvived = 15f;
        public float moraleRally = 8f;
        [Tooltip("Deaths/explosions closer than this to the mine hurt morale")] public float nearMineRadius = 22f;
        public float soldierFireRateAtZeroMorale = 0.6f;
        public float soldierFireRateAtFullMorale = 1.4f;
        [Tooltip("Spread multiplier at morale 0 (1 = config spread)")] public float soldierSpreadAtZeroMorale = 2f;
        public float soldierSpreadAtFullMorale = 0.5f;

        [Header("Player")]
        public float playerHealth = 160f;
        public float playerSpeed = 7f;
        public float playerDamage = 26f;
        public float playerFireInterval = 0.14f;
        public int playerMagazine = 24;
        public float playerReloadTime = 1.5f;
        public float playerRange = 32f;
        public float playerSpread = 1.5f;
        [Tooltip("Health regenerated per second when not hit for 5 s")] public float playerRegen = 2f;
        public float buildRange = 14f;
        public float rallyRadius = 16f;
        public float rallyDuration = 8f;
        public float rallyFireRateMultiplier = 1.7f;
        public float rallyCooldown = 30f;
        public float orderRadius = 16f;

        [Header("Friendly units")]
        public float soldierSpeed = 4.5f;
        [Tooltip("Autonomous soldiers notice enemies this far away and walk towards them")] public float soldierDetectRange = 26f;
        [Tooltip("Autonomous soldiers don't chase further than this from where they were placed/released")] public float soldierLeashRadius = 16f;

        [Header("Upgrades (index 0 = level I, 1 = level II, 2 = level III; upgradeCost[0] = cost of I→II, [1] = II→III)")]
        public MineLevels mine = new MineLevels();
        public SoldierLevels soldier = new SoldierLevels();
        public TowerLevels tower = new TowerLevels();
        public MinerLevels miner = new MinerLevels();
        public BarricadeLevels barricade = new BarricadeLevels();

        [Header("Waves")]
        public int totalWaves = 10;
        public float firstWaveDelay = 60f;
        public float waveInterval = 120f;
        [Tooltip("Seconds between individual spawns within a wave")] public float spawnInterval = 0.7f;
        [Tooltip("Enemy health multiplier added per wave (0.07 = +7% per wave)")] public float healthGrowthPerWave = 0.07f;
        [Tooltip("From this wave on, enemies attack from both sides")] public int bothSidesFromWave = 3;
        [Tooltip("From this wave on, some riflemen also come up the road")] public int roadAttacksFromWave = 5;

        [Header("Grenades")]
        public float grenadeRadius = 4.5f;
        public float grenadeFlightTime = 1.1f;

        [Header("Forest")]
        [Tooltip("Enemies in the forest are invisible beyond this distance from the player/soldiers")] public float forestRevealDistance = 11f;
        [Tooltip("Seconds an enemy stays visible after firing")] public float forestRevealAfterShot = 1.5f;

        [Header("Enemies")]
        public EnemyStats[] enemies = DefaultEnemies();

        /// <summary>Cost to go from currentLevel to currentLevel+1, or -1 at max level.</summary>
        public int UpgradeCost(UnitKind kind, int currentLevel)
        {
            int[] costs = kind switch
            {
                UnitKind.Mine => mine.upgradeCost,
                UnitKind.Soldier => soldier.upgradeCost,
                UnitKind.Tower => tower.upgradeCost,
                UnitKind.Miner => miner.upgradeCost,
                UnitKind.Barricade => barricade.upgradeCost,
                _ => null
            };
            int i = currentLevel - 1;
            if (costs == null || i < 0 || i >= costs.Length) return -1;
            return costs[i];
        }

        /// <summary>Short "what the next level improves" text.</summary>
        public string DescribeNext(UnitKind kind, int currentLevel)
        {
            int a = currentLevel - 1, b = currentLevel;
            if (b > 2) return "Maximum level reached.";
            switch (kind)
            {
                case UnitKind.Mine:
                    return $"Health {mine.health[a]:0}→{mine.health[b]:0}   Dig speed x{mine.digMultiplier[a]:0.0#}→x{mine.digMultiplier[b]:0.0#}   Miner slots {mine.maxMiners[a]}→{mine.maxMiners[b]}";
                case UnitKind.Soldier:
                    return $"Health {soldier.health[a]:0}→{soldier.health[b]:0}   Damage {soldier.damage[a]:0}→{soldier.damage[b]:0}   Accuracy +{(1f - soldier.spread[b] / soldier.spread[a]) * 100f:0}%   Better rifle";
                case UnitKind.Tower:
                    return $"Range {tower.range[a]:0}→{tower.range[b]:0}   Damage {tower.damage[a]:0}→{tower.damage[b]:0}   Health {tower.health[a]:0}→{tower.health[b]:0}";
                case UnitKind.Miner:
                    return $"Dig speed x{miner.digSpeed[a]:0.0#}→x{miner.digSpeed[b]:0.0#}   Walk speed {miner.walkSpeed[a]:0.0}→{miner.walkSpeed[b]:0.0}   Health {miner.health[a]:0}→{miner.health[b]:0}";
                case UnitKind.Barricade:
                    return $"Health {barricade.health[a]:0}→{barricade.health[b]:0}   Width x{barricade.width[a]:0.0#}→x{barricade.width[b]:0.0#}";
            }
            return "";
        }

        public EnemyStats GetEnemy(EnemyType t)
        {
            foreach (var e in enemies) if (e.type == t) return e;
            return enemies[0];
        }

        public float MoraleLerp(float atZero, float atFifty, float atHundred, float morale)
        {
            morale = Mathf.Clamp(morale, 0, 100);
            return morale < 50 ? Mathf.Lerp(atZero, atFifty, morale / 50f) : Mathf.Lerp(atFifty, atHundred, (morale - 50f) / 50f);
        }

        public static EnemyStats[] DefaultEnemies() => new[]
        {
            new EnemyStats { type = EnemyType.Rifleman,  health = 60,  armor = 0,    moveSpeed = 3.6f, damage = 7,  attackInterval = 1.1f, attackRange = 16, spread = 6, aggroRange = 18, killReward = 8,  firstWave = 1, baseCount = 4, countPerWave = 1.5f },
            new EnemyStats { type = EnemyType.Raider,    health = 45,  armor = 0,    moveSpeed = 6.5f, damage = 12, attackInterval = 0.8f, attackRange = 1.8f, spread = 0, aggroRange = 12, killReward = 8,  firstWave = 2, baseCount = 2, countPerWave = 1f },
            new EnemyStats { type = EnemyType.Ambusher,  health = 55,  armor = 0,    moveSpeed = 4.6f, damage = 10, attackInterval = 0.9f, attackRange = 15, spread = 5, aggroRange = 14, killReward = 10, firstWave = 2, baseCount = 1, countPerWave = 0.6f },
            new EnemyStats { type = EnemyType.Grenadier, health = 70,  armor = 0,    moveSpeed = 3.1f, damage = 45, attackInterval = 3.5f, attackRange = 17, spread = 0, aggroRange = 20, killReward = 12, firstWave = 4, baseCount = 1, countPerWave = 0.6f },
            new EnemyStats { type = EnemyType.Heavy,     health = 380, armor = 0.4f, moveSpeed = 2.2f, damage = 13, attackInterval = 0.45f, attackRange = 14, spread = 7, aggroRange = 16, killReward = 30, firstWave = 6, baseCount = 1, countPerWave = 0.5f },
        };
    }
}
