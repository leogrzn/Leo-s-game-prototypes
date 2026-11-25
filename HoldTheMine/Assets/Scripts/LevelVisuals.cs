using UnityEngine;
using UnityEngine.AI;

namespace HoldTheMine
{
    /// <summary>Switches the blocky model parts, colours and collider size of a unit/building for levels I–III.</summary>
    public class LevelVisuals : MonoBehaviour
    {
        [Header("Parts")]
        public GameObject[] onlyL1 = new GameObject[0];
        public GameObject[] onlyL2 = new GameObject[0];
        public GameObject[] onlyL3 = new GameObject[0];
        public GameObject[] fromL2 = new GameObject[0];
        public GameObject[] fromL3 = new GameObject[0];

        [Header("Recolouring (one material per level)")]
        public Renderer[] tintA = new Renderer[0];
        public Material[] tintAMats = new Material[3];
        public Renderer[] tintB = new Renderer[0];
        public Material[] tintBMats = new Material[3];

        [Header("Per-level gun / turret (optional)")]
        public Transform[] muzzles = new Transform[3];
        public Transform[] pivots = new Transform[3];

        [Header("Per-level box collider (optional)")]
        public Vector3[] boxCenters = new Vector3[0];
        public Vector3[] boxSizes = new Vector3[0];

        public void SetLevel(int level)
        {
            Set(onlyL1, level == 1);
            Set(onlyL2, level == 2);
            Set(onlyL3, level == 3);
            Set(fromL2, level >= 2);
            Set(fromL3, level >= 3);
            int i = Mathf.Clamp(level - 1, 0, 2);
            Tint(tintA, tintAMats, i);
            Tint(tintB, tintBMats, i);
            if (boxSizes.Length == 3 && boxCenters.Length == 3)
            {
                var bc = GetComponent<BoxCollider>();
                if (bc) { bc.center = boxCenters[i]; bc.size = boxSizes[i]; }
                var ob = GetComponent<NavMeshObstacle>();
                if (ob) { ob.center = boxCenters[i]; ob.size = boxSizes[i]; }
            }
        }

        public Transform Muzzle(int level) => muzzles != null && muzzles.Length >= level ? muzzles[level - 1] : null;
        public Transform Pivot(int level) => pivots != null && pivots.Length >= level ? pivots[level - 1] : null;

        static void Set(GameObject[] parts, bool on)
        {
            if (parts == null) return;
            foreach (var p in parts) if (p) p.SetActive(on);
        }

        static void Tint(Renderer[] rs, Material[] mats, int i)
        {
            if (rs == null || mats == null || mats.Length <= i || mats[i] == null) return;
            foreach (var r in rs) if (r) r.sharedMaterial = mats[i];
        }
    }
}
