using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HoldTheMine
{
    public enum BuildType { None, Barricade, Soldier, Tower }

    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Transform muzzle;
        public Material ghostOkMat, ghostBadMat;
        public Camera cam;

        public int Ammo { get; private set; }
        public bool Reloading => reloadUntil > Time.time;
        public float ReloadProgress => Reloading ? 1f - (reloadUntil - Time.time) / Cfg.playerReloadTime : 1f;
        public BuildType Building { get; private set; }
        public float RallyCooldownLeft => Mathf.Max(0, rallyReadyAt - Time.time);
        public Vector3 AimPoint { get; private set; }

        CharacterController cc;
        Health health;
        float nextShot, reloadUntil, rallyReadyAt, buildYaw;
        bool wasReloading;
        GameObject ghost;
        Renderer ghostRenderer;
        BuildType ghostType;
        float vy;
        bool holdFire;
        static readonly RaycastHit[] PickHits = new RaycastHit[16];

        /// <summary>The selectable friendly under the mouse, or null if the mouse is on the ground / an enemy.</summary>
        Health PickFriendly(Mouse mouse)
        {
            Ray r = cam.ScreenPointToRay(mouse.position.ReadValue());
            int n = Physics.RaycastNonAlloc(r, PickHits, 300f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(PickHits, 0, n, Comparer);
            for (int i = 0; i < n; i++)
            {
                var h = PickHits[i].collider.GetComponentInParent<Health>();
                if (h != null && h.kind == UnitKind.Player) continue;
                if (SelectionManager.IsSelectable(h)) return h;
                if (h != null) return null; // an enemy or truck: shoot
                // Ground/rock/tree: be forgiving with small units standing right there.
                Vector3 p = PickHits[i].point;
                Health best = null; float bestD = 0.75f;
                foreach (var u in Health.All)
                {
                    if ((u.kind != UnitKind.Soldier && u.kind != UnitKind.Miner) || u.IsDead) continue;
                    float d = Vector3.Distance(new Vector3(u.transform.position.x, 0, u.transform.position.z), new Vector3(p.x, 0, p.z));
                    if (d < bestD) { bestD = d; best = u; }
                }
                return best;
            }
            return null;
        }

        static readonly System.Collections.Generic.Comparer<RaycastHit> Comparer =
            System.Collections.Generic.Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        GameConfig Cfg => GameManager.Instance.config;
        GameManager GM => GameManager.Instance;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            if (cam == null) cam = Camera.main;
        }

        void Start()
        {
            health.SetMax(Cfg.playerHealth);
            Ammo = Cfg.playerMagazine;
        }

        void Update()
        {
            if (GM == null) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            var sel = SelectionManager.Instance;
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (GM.IsPlaying && Building != BuildType.None) SetBuild(BuildType.None);
                else if (GM.IsPlaying && sel.Selected != null) sel.Deselect();
                else if (GM.ui.ShopOpen && GM.IsPlaying) GM.ui.ToggleShop();
                else GM.TogglePause();
            }
            if (!GM.IsPlaying || health.IsDead) { if (ghost) ghost.SetActive(false); return; }

            UpdateAim(mouse);
            Move(kb);

            // Reload
            if (wasReloading && !Reloading) { Ammo = Cfg.playerMagazine; }
            wasReloading = Reloading;
            if (kb.rKey.wasPressedThisFrame && !Reloading && Ammo < Cfg.playerMagazine) StartReload();

            // Build selection
            if (kb.digit1Key.wasPressedThisFrame) SetBuild(Building == BuildType.Barricade ? BuildType.None : BuildType.Barricade);
            if (kb.digit2Key.wasPressedThisFrame) SetBuild(Building == BuildType.Soldier ? BuildType.None : BuildType.Soldier);
            if (kb.digit3Key.wasPressedThisFrame) SetBuild(Building == BuildType.Tower ? BuildType.None : BuildType.Tower);
            if (kb.digit4Key.wasPressedThisFrame) GM.OrderFood();
            if (kb.digit5Key.wasPressedThisFrame) GM.HireMiner();
            if (kb.tabKey.wasPressedThisFrame) GM.ui.ToggleShop();
            if (kb.qKey.wasPressedThisFrame) Rally();
            if (kb.fKey.wasPressedThisFrame) OrderSoldiers();
            if (kb.xKey.wasPressedThisFrame) sel.ReleaseSelected();
            if (kb.uKey.wasPressedThisFrame) sel.UpgradeSelected();
            if (kb.mKey.wasPressedThisFrame) sel.Select(GM.mine);
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f) buildYaw += Mathf.Sign(scroll) * 45f;

            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // Right click: cancel build / order the selected soldier / deselect.
            if (mouse.rightButton.wasPressedThisFrame && !overUI)
            {
                if (Building != BuildType.None) SetBuild(BuildType.None);
                else if (sel.Selected != null)
                {
                    var picked = PickFriendly(mouse);
                    if (picked == sel.Selected) sel.Deselect();
                    else if (sel.SelectedSoldier != null) sel.OrderMove(AimPoint);
                }
            }

            // Left click on a friendly unit/building selects it instead of shooting.
            if (Building == BuildType.None && mouse.leftButton.wasPressedThisFrame && !overUI)
            {
                var picked = PickFriendly(mouse);
                if (picked != null) { sel.Select(picked); holdFire = true; }
            }
            if (!mouse.leftButton.isPressed) holdFire = false;

            if (Building != BuildType.None)
            {
                bool valid = UpdateGhost(out Vector3 pos, out Quaternion rot);
                if (mouse.leftButton.wasPressedThisFrame && !overUI) TryPlace(pos, rot, valid);
            }
            else if (ghost) ghost.SetActive(false);

            if (Building == BuildType.None && mouse.leftButton.isPressed && !overUI && !holdFire) TryShoot();
            if (Ammo <= 0 && !Reloading) StartReload();

            // Regeneration when out of combat
            if (Time.time - health.LastDamageTime > 5f) health.Heal(Cfg.playerRegen * Time.deltaTime);
        }

        void UpdateAim(Mouse mouse)
        {
            Ray r = cam.ScreenPointToRay(mouse.position.ReadValue());
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(r, out float enter))
            {
                AimPoint = r.GetPoint(enter);
                Vector3 look = AimPoint - transform.position;
                look.y = 0;
                if (look.sqrMagnitude > 0.05f) transform.rotation = Quaternion.LookRotation(look);
            }
        }

        void Move(Keyboard kb)
        {
            Vector2 input = Vector2.zero;
            if (kb.wKey.isPressed) input.y += 1;
            if (kb.sKey.isPressed) input.y -= 1;
            if (kb.dKey.isPressed) input.x += 1;
            if (kb.aKey.isPressed) input.x -= 1;
            Vector3 fwd = cam.transform.forward; fwd.y = 0; fwd.Normalize();
            Vector3 right = cam.transform.right; right.y = 0; right.Normalize();
            Vector3 move = (fwd * input.y + right * input.x);
            if (move.sqrMagnitude > 1) move.Normalize();
            vy = cc.isGrounded ? -1f : vy + Physics.gravity.y * Time.deltaTime;
            cc.Move((move * Cfg.playerSpeed + Vector3.up * vy) * Time.deltaTime);
            // Stay inside the map
            Vector3 p = transform.position;
            float lim = 62f;
            if (Mathf.Abs(p.x) > lim || Mathf.Abs(p.z) > lim)
            {
                cc.enabled = false;
                transform.position = new Vector3(Mathf.Clamp(p.x, -lim, lim), p.y, Mathf.Clamp(p.z, -lim, lim));
                cc.enabled = true;
            }
        }

        void StartReload()
        {
            reloadUntil = Time.time + Cfg.playerReloadTime;
            wasReloading = true;
        }

        void TryShoot()
        {
            if (Reloading || Ammo <= 0 || Time.time < nextShot) return;
            nextShot = Time.time + Cfg.playerFireInterval;
            Ammo--;
            Vector3 dir = new Vector3(AimPoint.x, muzzle.position.y, AimPoint.z) - muzzle.position;
            Combat.Fire(muzzle.position, dir, Cfg.playerRange, Cfg.playerDamage, Team.Friendly, Cfg.playerSpread, new Color(1f, 0.95f, 0.4f));
        }

        void Rally()
        {
            if (RallyCooldownLeft > 0)
            {
                GM.ui.ShowMessage($"Rally ready in {RallyCooldownLeft:0}s", UIManager.Warn);
                return;
            }
            int n = 0;
            foreach (var s in SoldierAI.All)
            {
                if (Vector3.Distance(s.transform.position, transform.position) <= Cfg.rallyRadius)
                {
                    s.Rally(Cfg.rallyDuration);
                    n++;
                }
            }
            rallyReadyAt = Time.time + Cfg.rallyCooldown;
            GM.Rallied();
            Explosion.SpawnSpark(transform.position + Vector3.up * 2.5f);
            GM.ui.ShowMessage(n > 0 ? $"RALLY! {n} defenders fire faster. Morale up." : "RALLY! Morale up (no defenders nearby).", UIManager.Good);
        }

        void OrderSoldiers()
        {
            int n = 0;
            foreach (var s in SoldierAI.All)
            {
                if (s.isTower) continue;
                if (Vector3.Distance(s.transform.position, transform.position) <= Cfg.orderRadius)
                {
                    Vector2 off = Random.insideUnitCircle * 1.8f;
                    s.MoveTo(AimPoint + new Vector3(off.x, 0, off.y));
                    n++;
                }
            }
            GM.ui.ShowMessage(n > 0 ? $"{n} soldiers moving to position." : "No soldiers near you to order.", n > 0 ? UIManager.Info : UIManager.Warn);
        }

        // ---------------- Building ----------------
        void SetBuild(BuildType t)
        {
            Building = t;
            if (t == BuildType.None && ghost) ghost.SetActive(false);
        }

        public void SelectBuild(BuildType t) => SetBuild(t);

        static Vector3 SizeOf(BuildType t) => t switch
        {
            BuildType.Barricade => new Vector3(3.2f, 1.5f, 0.8f),
            BuildType.Soldier => new Vector3(0.9f, 2f, 0.9f),
            BuildType.Tower => new Vector3(2.6f, 4.5f, 2.6f),
            _ => Vector3.one
        };

        int CostOf(BuildType t) => t switch
        {
            BuildType.Barricade => Cfg.barricadeCost,
            BuildType.Soldier => Cfg.soldierCost,
            BuildType.Tower => Cfg.towerCost,
            _ => 0
        };

        bool UpdateGhost(out Vector3 pos, out Quaternion rot)
        {
            if (ghost == null)
            {
                ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ghost.name = "BuildGhost";
                DestroyImmediate(ghost.GetComponent<Collider>());
                ghostRenderer = ghost.GetComponent<Renderer>();
                ghostRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            ghost.SetActive(true);
            Vector3 size = SizeOf(Building);
            ghost.transform.localScale = size;

            Vector3 target = AimPoint;
            Vector3 fromPlayer = target - transform.position; fromPlayer.y = 0;
            if (fromPlayer.magnitude > Cfg.buildRange) target = transform.position + fromPlayer.normalized * Cfg.buildRange;
            target.y = 0;

            Vector3 away = target - GM.mine.transform.position; away.y = 0;
            rot = Quaternion.Euler(0, buildYaw, 0) * (away.sqrMagnitude > 0.1f ? Quaternion.LookRotation(away) : Quaternion.identity);
            pos = target;

            bool valid = NavMesh.SamplePosition(target, out var hit, 0.6f, NavMesh.AllAreas);
            if (valid)
            {
                pos = hit.position; pos.y = 0;
                var overlaps = Physics.OverlapBox(pos + Vector3.up * (size.y * 0.5f + 0.15f), size * 0.45f, rot, ~0, QueryTriggerInteraction.Ignore);
                foreach (var c in overlaps)
                {
                    if (c.GetComponentInParent<PlayerController>() != null) continue;
                    valid = false; break;
                }
            }
            if (GM.Money < CostOf(Building)) valid = false;
            ghost.transform.SetPositionAndRotation(pos + Vector3.up * size.y * 0.5f, rot);
            ghostRenderer.sharedMaterial = valid ? ghostOkMat : ghostBadMat;
            return valid;
        }

        void TryPlace(Vector3 pos, Quaternion rot, bool valid)
        {
            int cost = CostOf(Building);
            if (!valid)
            {
                GM.ui.ShowMessage(GM.Money < cost ? $"Not enough money (${cost})." : "Can't build there.", UIManager.Warn);
                return;
            }
            string label = Building.ToString().ToLower();
            if (!GM.TrySpend(cost, label)) return;
            switch (Building)
            {
                case BuildType.Barricade:
                    Instantiate(GM.barricadePrefab, pos, rot, GM.unitsRoot);
                    break;
                case BuildType.Soldier:
                    GM.SpawnSoldier(pos);
                    break;
                case BuildType.Tower:
                    Instantiate(GM.towerPrefab, pos, rot, GM.unitsRoot);
                    break;
            }
            GM.ui.ShowMessage($"{Building} placed (-${cost}).", UIManager.Good);
        }
    }
}
