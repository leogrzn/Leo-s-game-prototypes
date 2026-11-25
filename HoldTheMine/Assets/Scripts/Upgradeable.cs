using System.Collections.Generic;
using UnityEngine;

namespace HoldTheMine
{
    /// <summary>Level I–III for the mine, soldiers, towers, miners and barricades. Stats come from GameConfig level tables.</summary>
    [RequireComponent(typeof(Health))]
    public class Upgradeable : MonoBehaviour
    {
        public static readonly List<Upgradeable> All = new List<Upgradeable>();
        public const int MaxLevel = 3;

        public int Level { get; private set; } = 1;
        public Health Health { get; private set; }
        public UnitKind Kind => Health.kind;
        public bool IsMax => Level >= MaxLevel;
        public int NextCost => GM.config.UpgradeCost(Kind, Level);
        public bool CanAfford => !IsMax && NextCost >= 0 && GM.Money >= NextCost;
        public string NextDescription => GM.config.DescribeNext(Kind, Level);

        LevelVisuals visuals;
        GameManager GM => GameManager.Instance;

        void Awake()
        {
            Health = GetComponent<Health>();
            visuals = GetComponent<LevelVisuals>();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Start() => Apply();

        public bool TryUpgrade()
        {
            if (Health.IsDead) return false;
            if (IsMax)
            {
                GM.ui.ShowMessage($"{DisplayName} is already at maximum level.", UIManager.Warn);
                return false;
            }
            int cost = NextCost;
            if (!GM.TrySpend(cost, $"the {DisplayName.ToLower()} upgrade")) return false;
            Level++;
            Apply();
            float size = Kind == UnitKind.Mine ? 6f : Kind == UnitKind.Tower ? 2.5f : Kind == UnitKind.Barricade ? 2f : 1.2f;
            Explosion.SpawnUpgradeFx(transform.position, size);
            GM.ui.ShowMessage($"{DisplayName} upgraded to level {Roman(Level)}! (-${cost})", UIManager.Good);
            return true;
        }

        void Apply()
        {
            if (visuals) visuals.SetLevel(Level);
            var c = GM.config;
            int i = Level - 1;
            switch (Kind)
            {
                case UnitKind.Soldier:
                case UnitKind.Tower:
                    GetComponent<SoldierAI>()?.ApplyLevel(Level);
                    break;
                case UnitKind.Miner:
                    GetComponent<MinerAI>()?.ApplyLevel(Level);
                    break;
                case UnitKind.Barricade:
                    Health.SetMaxKeepDamage(c.barricade.health[i]);
                    transform.localScale = new Vector3(c.barricade.width[i], 1f, 1f);
                    break;
                case UnitKind.Mine:
                    Health.SetMaxKeepDamage(c.mine.health[i]);
                    break;
            }
        }

        public string DisplayName => Kind switch
        {
            UnitKind.Mine => "Gold Mine",
            UnitKind.Soldier => "Soldier",
            UnitKind.Tower => "Guard Tower",
            UnitKind.Miner => "Miner",
            UnitKind.Barricade => "Barricade",
            _ => Kind.ToString()
        };

        public static string Roman(int level) => level switch { 1 => "I", 2 => "II", _ => "III" };
    }
}
