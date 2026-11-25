using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldTheMine
{
    public enum Team { Friendly, Enemy }
    public enum UnitKind { Player, Soldier, Miner, Tower, Barricade, Truck, Mine, Enemy }

    /// <summary>Shared health/death for every unit and structure. Also a registry of living things.</summary>
    public class Health : MonoBehaviour
    {
        public static readonly List<Health> All = new List<Health>();

        public Team team;
        public UnitKind kind;
        public float maxHealth = 100f;
        [Range(0, 0.9f)] public float armor;
        public float current;
        public bool IsDead { get; private set; }
        public float LastDamageTime { get; private set; } = -100f;

        public event Action<Health> Died;
        public event Action<float> Damaged;

        Renderer[] renderers;
        MaterialPropertyBlock mpb;
        float flashUntil;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            if (current <= 0) current = maxHealth;
            renderers = GetComponentsInChildren<Renderer>(true);
            mpb = new MaterialPropertyBlock();
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void SetMax(float max, bool refill = true)
        {
            maxHealth = max;
            if (refill) current = max;
        }

        /// <summary>Changes max health but keeps the damage already taken.</summary>
        public void SetMaxKeepDamage(float max)
        {
            float damage = maxHealth - current;
            maxHealth = max;
            current = Mathf.Clamp(max - damage, 1f, max);
        }

        /// <summary>World-space height of the top of this thing (for badges/markers).</summary>
        public float TopY
        {
            get
            {
                var c = GetComponent<Collider>();
                return c != null ? c.bounds.max.y : transform.position.y + 2f;
            }
        }

        public float Fraction => maxHealth > 0 ? Mathf.Clamp01(current / maxHealth) : 0;

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0) return;
            amount *= 1f - armor;
            current -= amount;
            LastDamageTime = Time.time;
            Damaged?.Invoke(amount);
            Flash();
            if (current <= 0) Die();
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            current = Mathf.Min(maxHealth, current + amount);
        }

        public void Die()
        {
            if (IsDead) return;
            IsDead = true;
            current = 0;
            Died?.Invoke(this);
            if (GameManager.Instance != null) GameManager.Instance.OnUnitDied(this);
            if (kind != UnitKind.Player && kind != UnitKind.Mine)
            {
                Explosion.SpawnDebris(transform.position, kind == UnitKind.Truck || kind == UnitKind.Tower ? 2.5f : 1f);
                Destroy(gameObject);
            }
        }

        void Flash()
        {
            if (renderers == null) return;
            flashUntil = Time.time + 0.08f;
            mpb.SetColor(BaseColorId, Color.white);
            foreach (var r in renderers) if (r && r.enabled && !(r is LineRenderer)) r.SetPropertyBlock(mpb);
        }

        void LateUpdate()
        {
            if (flashUntil > 0 && Time.time > flashUntil && renderers != null)
            {
                flashUntil = 0;
                foreach (var r in renderers) if (r) r.SetPropertyBlock(null);
            }
        }

        public Vector3 AimPoint => transform.position + Vector3.up * (kind == UnitKind.Mine ? 2f : 1.1f);

        /// <summary>Closest living hostile of the given kinds within range of a point.</summary>
        public static Health FindNearest(Vector3 from, float range, Team ofTeam, Predicate<Health> filter = null)
        {
            Health best = null;
            float bestSqr = range * range;
            foreach (var h in All)
            {
                if (h.IsDead || h.team != ofTeam) continue;
                if (filter != null && !filter(h)) continue;
                float d = (h.transform.position - from).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = h; }
            }
            return best;
        }

        public static int Count(UnitKind kind)
        {
            int n = 0;
            foreach (var h in All) if (!h.IsDead && h.kind == kind) n++;
            return n;
        }
    }
}
