using UnityEngine;

namespace HoldTheMine
{
    /// <summary>Holds the single selected friendly unit/building, draws its ring and its move-order marker.</summary>
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager Instance { get; private set; }

        public Material lineMat;
        public Color ringColor = new Color(0.35f, 1f, 0.45f);
        public Color destColor = new Color(1f, 0.85f, 0.25f);

        public Health Selected { get; private set; }
        public SoldierAI SelectedSoldier { get; private set; }   // null for towers
        public Upgradeable SelectedUpgradeable { get; private set; }

        LineRenderer ring, destRing, destPost;
        const int Segments = 40;

        GameManager GM => GameManager.Instance;

        void Awake()
        {
            Instance = this;
            ring = MakeLine("SelectionRing", ringColor, Segments, true, 0.14f);
            destRing = MakeLine("OrderMarker", destColor, Segments, true, 0.12f);
            destPost = MakeLine("OrderMarkerPost", destColor, 2, false, 0.1f);
            Hide();
        }

        LineRenderer MakeLine(string name, Color c, int count, bool loop, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = lineMat;
            lr.positionCount = count;
            lr.loop = loop;
            lr.useWorldSpace = true;
            lr.widthMultiplier = width;
            lr.startColor = lr.endColor = c;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        public static bool IsSelectable(Health h) =>
            h != null && !h.IsDead && h.team == Team.Friendly &&
            (h.kind == UnitKind.Soldier || h.kind == UnitKind.Tower || h.kind == UnitKind.Miner || h.kind == UnitKind.Barricade || h.kind == UnitKind.Mine);

        public void Select(Health h)
        {
            if (!IsSelectable(h)) return;
            Selected = h;
            var s = h.GetComponent<SoldierAI>();
            SelectedSoldier = s != null && !s.isTower ? s : null;
            SelectedUpgradeable = h.GetComponent<Upgradeable>();
        }

        public void Deselect()
        {
            Selected = null;
            SelectedSoldier = null;
            SelectedUpgradeable = null;
            Hide();
        }

        public void OrderMove(Vector3 p)
        {
            if (SelectedSoldier == null) return;
            SelectedSoldier.OrderMove(p);
            GM.ui.ShowMessage("Soldier moving — he will hold that position.", UIManager.Info);
        }

        public void ReleaseSelected()
        {
            if (SelectedSoldier == null) { GM.ui.ShowMessage("Select a soldier to release him.", UIManager.Warn); return; }
            if (SelectedSoldier.Mode == SoldierMode.Autonomous) { GM.ui.ShowMessage("This soldier is already autonomous.", UIManager.Info); return; }
            SelectedSoldier.Release();
            GM.ui.ShowMessage("Soldier released — he will hunt enemies on his own.", UIManager.Info);
        }

        public void UpgradeSelected()
        {
            if (SelectedUpgradeable == null) { GM.ui.ShowMessage("Click a unit or building to select it first.", UIManager.Warn); return; }
            SelectedUpgradeable.TryUpgrade();
        }

        void Hide()
        {
            ring.enabled = false;
            destRing.enabled = false;
            destPost.enabled = false;
        }

        void LateUpdate()
        {
            if (Selected != null && Selected.IsDead) Deselect();
            if (Selected == null) { Hide(); return; }

            float radius = RadiusOf(Selected) * (1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.04f);
            Circle(ring, Selected.transform.position, radius);
            ring.enabled = true;

            bool showDest = SelectedSoldier != null && SelectedSoldier.Mode != SoldierMode.Autonomous;
            destRing.enabled = destPost.enabled = showDest;
            if (showDest)
            {
                Vector3 p = SelectedSoldier.HoldPoint;
                Circle(destRing, p, 0.6f + Mathf.PingPong(Time.unscaledTime, 0.3f));
                destPost.SetPosition(0, p + Vector3.up * 0.05f);
                destPost.SetPosition(1, p + Vector3.up * 2.2f);
            }
        }

        static float RadiusOf(Health h)
        {
            switch (h.kind)
            {
                case UnitKind.Mine: return 8f;
                case UnitKind.Tower: return 2.3f;
                case UnitKind.Barricade: return 1.9f * h.transform.localScale.x;
                default: return 0.85f;
            }
        }

        static void Circle(LineRenderer lr, Vector3 c, float r)
        {
            for (int i = 0; i < Segments; i++)
            {
                float a = i * Mathf.PI * 2f / Segments;
                lr.SetPosition(i, new Vector3(c.x + Mathf.Cos(a) * r, 0.08f, c.z + Mathf.Sin(a) * r));
            }
        }
    }
}
