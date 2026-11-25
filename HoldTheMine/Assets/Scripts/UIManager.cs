using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HoldTheMine
{
    /// <summary>
    /// HUD, build bar, selection panel, alerts with off-screen arrows, minimap, level badges, shop and menus.
    /// BuildUI() is run once by the Editor builder; the result is saved in the scene.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static readonly Color Info = new Color(0.85f, 0.9f, 1f);
        public static readonly Color Good = new Color(0.5f, 1f, 0.5f);
        public static readonly Color Warn = new Color(1f, 0.85f, 0.3f);
        public static readonly Color Bad = new Color(1f, 0.4f, 0.35f);
        static readonly Color Gold = new Color(1f, 0.82f, 0.2f);
        static readonly Color PanelColor = new Color(0.07f, 0.07f, 0.08f, 0.82f);
        static readonly Color ButtonColor = new Color(0.16f, 0.16f, 0.18f, 0.95f);
        static readonly Color ButtonSelected = new Color(0.22f, 0.5f, 0.25f, 0.98f);
        const float SelWidth = 470f;
        static readonly Color BarBad = new Color(0.72f, 0.2f, 0.16f), BarWarn = new Color(0.72f, 0.55f, 0.1f), BarGood = new Color(0.25f, 0.6f, 0.25f), BarGold = new Color(0.72f, 0.52f, 0.08f);
        const float BarWidth = 8 * 100 + 7 * 6 + 16;

        [System.Serializable]
        public class Icons
        {
            public Sprite coin, gold, food, heart, ammo, skull, clock, flag, pickaxe, barricade, soldier, tower, miner, star, target, crate, up, arrow, alert, truck;
        }

        [SerializeField] Font font;
        [SerializeField] Icons icons = new Icons();
        [SerializeField] Sprite minimapSprite;

        // HUD
        [SerializeField] GameObject hudRoot;
        [SerializeField] Text moneyText, goldText, foodText, trucksText, unitsText, waveText, countdownText, sidesText, moraleText, mineText, healthText, ammoText;
        [SerializeField] RectTransform moraleFill, mineFill, healthFill, reloadFill;
        [SerializeField] Image moraleFillImg, mineFillImg, healthFillImg;
        [SerializeField] RectTransform messageRoot, badgeRoot, arrowRoot, minimapContent;

        // Build bar: barricade, soldier, tower, food, miner, rally, order, shop
        [SerializeField] Button[] barButtons;
        [SerializeField] CanvasGroup[] barGroups;
        [SerializeField] Text[] barPrices;

        // Selection panel
        [SerializeField] GameObject selPanel;
        [SerializeField] Image selIcon;
        [SerializeField] Text selName, selLevel, selMode, selStats, selHpText, selNext, selHint, upgradeLabel;
        [SerializeField] RectTransform selHpFill;
        [SerializeField] Button upgradeButton, releaseButton, deselectButton;
        [SerializeField] CanvasGroup upgradeGroup;

        // Panels
        [SerializeField] GameObject menuPanel, pausePanel, victoryPanel, gameOverPanel, shopPanel;
        [SerializeField] Text gameOverReason, victoryStats, gameOverStats, shopInfo;
        [SerializeField] Button playButton, quitButton, resumeButton, pauseRestartButton, pauseMenuButton, victoryRestartButton, victoryMenuButton, overRestartButton, overMenuButton, shopFoodButton, shopMinerButton, shopMineButton, shopWaveButton, shopCloseButton;

        class Message { public CanvasGroup group; public float until; }
        class Threat { public Transform follow; public Vector3 pos; public Color color; public float until; }

        readonly List<Message> messages = new List<Message>();
        readonly List<Threat> threats = new List<Threat>();
        readonly List<Image> arrowPool = new List<Image>();
        readonly List<Image> dotPool = new List<Image>();
        readonly List<RectTransform> badgePool = new List<RectTransform>();
        int dotCount;
        Camera cam;

        public bool ShopOpen => shopPanel != null && shopPanel.activeSelf;
        GameManager GM => GameManager.Instance;
        SelectionManager Sel => SelectionManager.Instance;

        // =====================================================================
        // Runtime
        // =====================================================================
        void Awake()
        {
            playButton.onClick.AddListener(() => GM.StartGame());
            quitButton.onClick.AddListener(() => GM.Quit());
            resumeButton.onClick.AddListener(() => GM.TogglePause());
            pauseRestartButton.onClick.AddListener(() => GM.Restart());
            pauseMenuButton.onClick.AddListener(() => GM.QuitToMenu());
            victoryRestartButton.onClick.AddListener(() => GM.Restart());
            victoryMenuButton.onClick.AddListener(() => GM.QuitToMenu());
            overRestartButton.onClick.AddListener(() => GM.Restart());
            overMenuButton.onClick.AddListener(() => GM.QuitToMenu());
            shopFoodButton.onClick.AddListener(() => GM.OrderFood());
            shopMinerButton.onClick.AddListener(() => GM.HireMiner());
            shopMineButton.onClick.AddListener(() => { Sel.Select(GM.mine); shopPanel.SetActive(false); });
            shopWaveButton.onClick.AddListener(() => GM.waves.LaunchNextWave());
            shopCloseButton.onClick.AddListener(ToggleShop);

            barButtons[0].onClick.AddListener(() => GM.player.SelectBuild(GM.player.Building == BuildType.Barricade ? BuildType.None : BuildType.Barricade));
            barButtons[1].onClick.AddListener(() => GM.player.SelectBuild(GM.player.Building == BuildType.Soldier ? BuildType.None : BuildType.Soldier));
            barButtons[2].onClick.AddListener(() => GM.player.SelectBuild(GM.player.Building == BuildType.Tower ? BuildType.None : BuildType.Tower));
            barButtons[3].onClick.AddListener(() => GM.OrderFood());
            barButtons[4].onClick.AddListener(() => GM.HireMiner());
            barButtons[5].onClick.AddListener(() => ShowMessage("Press Q near your soldiers to rally them.", Info));
            barButtons[6].onClick.AddListener(() => ShowMessage("Press F with the mouse on a spot to send nearby soldiers there.", Info));
            barButtons[7].onClick.AddListener(ToggleShop);

            upgradeButton.onClick.AddListener(() => Sel.UpgradeSelected());
            releaseButton.onClick.AddListener(() => Sel.ReleaseSelected());
            deselectButton.onClick.AddListener(() => Sel.Deselect());
            shopPanel.SetActive(false);
            selPanel.SetActive(false);
        }

        void Start()
        {
            cam = Camera.main;
            if (GM != null) ApplyPrices(GM.config);
        }

        public void ToggleShop()
        {
            if (GM != null && !GM.IsPlaying) return;
            shopPanel.SetActive(!shopPanel.activeSelf);
        }

        public void OnStateChanged(GameState s)
        {
            menuPanel.SetActive(s == GameState.Menu);
            pausePanel.SetActive(s == GameState.Paused);
            victoryPanel.SetActive(s == GameState.Won);
            gameOverPanel.SetActive(s == GameState.Lost);
            hudRoot.SetActive(s != GameState.Menu);
            if (s != GameState.Playing) shopPanel.SetActive(false);
            if (s == GameState.Won || s == GameState.Lost)
            {
                string stats = $"Waves reached: {GM.waves.CurrentWave}/{GM.config.totalWaves}\nRaiders killed: {GM.EnemiesKilled}\nMine level: {Upgradeable.Roman(GM.MineLevel)}\nMoney: ${GM.Money}";
                victoryStats.text = "The raiders gave up. The gold keeps flowing.\n\n" + stats;
                gameOverStats.text = stats;
            }
        }

        public void SetGameOverReason(string r) => gameOverReason.text = r;

        public void ShowMessage(string msg, Color c) => PushMessage(msg, c, false);

        /// <summary>Important event: message with an alert icon, plus an arrow to the place if it is off-screen.</summary>
        public void Alert(string msg, Color c, Transform follow = null, Vector3? pos = null)
        {
            PushMessage(msg, c, true);
            if (follow != null || pos.HasValue) AddThreat(pos ?? follow.position, follow, c, 7f);
        }

        public void AddThreat(Vector3 pos, Transform follow, Color c, float duration)
        {
            foreach (var t in threats)
            {
                if ((follow != null && t.follow == follow) || (follow == null && t.follow == null && (t.pos - pos).sqrMagnitude < 25f))
                {
                    t.until = Time.unscaledTime + duration; t.color = c; return;
                }
            }
            threats.Add(new Threat { follow = follow, pos = pos, color = c, until = Time.unscaledTime + duration });
            if (threats.Count > 8) threats.RemoveAt(0);
        }

        void PushMessage(string msg, Color c, bool alert)
        {
            if (messageRoot == null) return;
            var row = new GameObject("Msg", typeof(RectTransform));
            row.transform.SetParent(messageRoot, false);
            row.AddComponent<Image>().color = new Color(0, 0, 0, alert ? 0.7f : 0.5f);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(12, 14, 3, 3); h.spacing = 8; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            if (alert)
            {
                var icon = NewImage((RectTransform)row.transform, "Icon", icons.alert, Color.white);
                var le = icon.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 26; le.preferredHeight = 26;
            }
            var t = MakeText((RectTransform)row.transform, "Text", msg, alert ? 25 : 22, TextAnchor.MiddleCenter, c);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var g = row.AddComponent<CanvasGroup>();
            g.blocksRaycasts = false; g.interactable = false;
            row.transform.SetAsFirstSibling();
            messages.Add(new Message { group = g, until = Time.unscaledTime + (alert ? 6f : 4f) });
            while (messages.Count > 5) { if (messages[0].group) Destroy(messages[0].group.gameObject); messages.RemoveAt(0); }
        }

        void Update()
        {
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                var m = messages[i];
                if (m.group == null) { messages.RemoveAt(i); continue; }
                float left = m.until - Time.unscaledTime;
                if (left <= 0) { Destroy(m.group.gameObject); messages.RemoveAt(i); continue; }
                m.group.alpha = Mathf.Clamp01(left / 0.8f);
            }
            if (GM == null || GM.State == GameState.Menu) return;
            if (cam == null) cam = Camera.main;
            RefreshHud();
            RefreshBuildBar();
            RefreshSelection();
            RefreshMinimap();
            RefreshBadges();
            RefreshArrows();
        }

        // ---------------- HUD ----------------
        void RefreshHud()
        {
            var c = GM.config;
            var w = GM.waves;
            waveText.text = $"WAVE {w.CurrentWave} / {c.totalWaves}";
            if (!w.AllWavesLaunched)
            {
                int secs = Mathf.CeilToInt(w.Countdown);
                countdownText.text = $"Next wave in {secs / 60}:{secs % 60:00}";
                countdownText.color = w.Countdown < 15 ? Bad : Color.white;
                sidesText.text = "from " + w.NextWaveSides;
            }
            else
            {
                countdownText.text = $"Final wave — {w.Alive} raiders left";
                countdownText.color = Warn;
                sidesText.text = "Hold on!";
            }

            moneyText.text = $"${GM.Money}";
            goldText.text = $"{GM.GoldWaiting:0} gold waiting  <color=#c8b070>(${GM.GoldWaiting * c.goldValue:0})</color>";
            float perMin = GM.FoodPerMinute;
            string orders = GM.PendingFoodOrders > 0 ? $"  <color=#8fe28f>+{GM.PendingFoodOrders * c.foodPerOrder:0} ordered</color>" : "";
            foodText.text = $"{GM.Food:0} food  <color=#a0a0a0>(-{perMin:0.0}/min)</color>{orders}";
            foodText.color = GM.Food <= 0 ? Bad : GM.Food / Mathf.Max(0.1f, perMin) < 1.5f ? Warn : Color.white;
            var tr = GM.trucks;
            trucksText.text = $"Gold truck {(tr.GoldTruckActive ? "on the road" : $"in {tr.GoldTruckIn:0}s")}   ·   Food truck {(tr.FoodTruckActive ? "on the road" : $"in {tr.FoodTruckIn:0}s")}";
            unitsText.text = $"Miners {Health.Count(UnitKind.Miner)}/{GM.MaxMiners}   Soldiers {Health.Count(UnitKind.Soldier)}   Towers {Health.Count(UnitKind.Tower)}   Raiders {EnemyAI.All.Count}";

            float m = GM.Morale / 100f;
            SetBar(moraleFill, m);
            moraleFillImg.color = m < 0.5f ? Color.Lerp(BarBad, BarWarn, m * 2f) : Color.Lerp(BarWarn, BarGood, (m - 0.5f) * 2f);
            string mood = GM.Morale >= 75 ? "High" : GM.Morale >= 45 ? "Steady" : GM.Morale >= 20 ? "Low" : "Breaking";
            moraleText.text = $"Morale {GM.Morale:0}  {mood}   <color=#c0c0c0>dig x{GM.MoraleDigMultiplier:0.0} · fire x{GM.FireRateMultiplier:0.0}</color>";
            SetBar(mineFill, GM.mine.Fraction);
            mineFillImg.color = GM.mine.Fraction < 0.3f ? BarBad : BarGold;
            mineText.text = $"Mine {Upgradeable.Roman(GM.MineLevel)}   {GM.mine.current:0} / {GM.mine.maxHealth:0}";

            var p = GM.player;
            var ph = p.GetComponent<Health>();
            SetBar(healthFill, ph.Fraction);
            healthFillImg.color = ph.Fraction < 0.3f ? BarBad : new Color(0.22f, 0.45f, 0.85f);
            healthText.text = $"{ph.current:0} / {ph.maxHealth:0}";
            ammoText.text = p.Reloading ? "Reloading…" : $"{p.Ammo} / {c.playerMagazine}   <color=#a0a0a0>[R] reload</color>";
            ammoText.color = p.Reloading || p.Ammo < 6 ? Warn : Color.white;
            SetBar(reloadFill, p.Reloading ? p.ReloadProgress : (float)p.Ammo / c.playerMagazine);
        }

        void RefreshBuildBar()
        {
            var c = GM.config;
            var p = GM.player;
            int[] costs = { c.barricadeCost, c.soldierCost, c.towerCost, c.foodOrderCost, c.minerCost };
            for (int i = 0; i < 5; i++)
            {
                bool ok = GM.Money >= costs[i];
                if (i == 4 && Health.Count(UnitKind.Miner) >= GM.MaxMiners) ok = false;
                barGroups[i].alpha = ok ? 1f : 0.4f;
                barPrices[i].color = ok ? new Color(0.6f, 1f, 0.6f) : Bad;
            }
            for (int i = 0; i < 3; i++)
                barButtons[i].image.color = (int)p.Building == i + 1 ? ButtonSelected : ButtonColor;
            float cd = p.RallyCooldownLeft;
            barPrices[5].text = cd > 0 ? $"{cd:0}s" : "ready";
            barPrices[5].color = cd > 0 ? Warn : new Color(0.6f, 1f, 0.6f);
            barGroups[5].alpha = cd > 0 ? 0.5f : 1f;
            barButtons[7].image.color = ShopOpen ? ButtonSelected : ButtonColor;

            if (ShopOpen)
            {
                float perMin = GM.FoodPerMinute;
                float minutes = perMin > 0 ? GM.Food / perMin : 99;
                shopInfo.text = $"Food: {GM.Food:0}  —  eating {perMin:0.0}/min  (~{minutes:0.0} min left)\n" +
                                $"Orders waiting for the food truck: {GM.PendingFoodOrders}\n" +
                                $"Miners: {Health.Count(UnitKind.Miner)} / {GM.MaxMiners} (mine level {Upgradeable.Roman(GM.MineLevel)})\n" +
                                $"Dig speed x{GM.DigMultiplier:0.00}  (morale x{GM.MoraleDigMultiplier:0.00} · mine x{GM.MineDigMultiplier:0.00})";
            }
        }

        // ---------------- Selection panel ----------------
        void RefreshSelection()
        {
            var h = Sel != null ? Sel.Selected : null;
            bool show = h != null && GM.IsPlaying;
            if (selPanel.activeSelf != show) selPanel.SetActive(show);
            if (!show) return;
            // Sit beside the build bar if there is room, otherwise above it.
            float canvasW = ((RectTransform)transform).rect.width;
            bool room = canvasW / 2f - BarWidth / 2f >= SelWidth + 30f;
            ((RectTransform)selPanel.transform).anchoredPosition = new Vector2(-18, room ? 14 : 128);

            var up = Sel.SelectedUpgradeable;
            var c = GM.config;
            int lv = up != null ? up.Level : 1;
            selIcon.sprite = IconFor(h.kind);
            selName.text = up != null ? up.DisplayName : h.kind.ToString();
            selLevel.text = $"LEVEL {Upgradeable.Roman(lv)}";
            SetBar(selHpFill, h.Fraction);
            selHpFill.GetComponent<Image>().color = h.Fraction < 0.3f ? BarBad : BarGood;
            selHpText.text = $"{h.current:0} / {h.maxHealth:0}";

            var soldier = h.GetComponent<SoldierAI>();
            releaseButton.gameObject.SetActive(Sel.SelectedSoldier != null);
            switch (h.kind)
            {
                case UnitKind.Soldier:
                    selMode.text = soldier.Mode switch
                    {
                        SoldierMode.Autonomous => "<color=#9fe89f>Autonomous</color> — hunts enemies near his post",
                        SoldierMode.Moving => "<color=#ffd34d>Moving</color> to ordered position",
                        _ => "<color=#ffd34d>Holding position</color> — fights from there"
                    };
                    selStats.text = $"Damage {soldier.Damage:0}     Fire rate {1f / soldier.FireInterval:0.0}/s\nAccuracy ±{soldier.Spread:0.0}°     Range {soldier.Range:0} m";
                    selHint.text = "Right-click ground: move & hold · Right-click him / Esc: deselect";
                    break;
                case UnitKind.Tower:
                    selMode.text = soldier.HasTarget ? "<color=#ff8a70>Engaging</color>" : "Watching";
                    selStats.text = $"Damage {soldier.Damage:0}     Fire rate {1f / soldier.FireInterval:0.0}/s\nRange {soldier.Range:0} m";
                    selHint.text = "[U] upgrade   ·   Esc: deselect";
                    break;
                case UnitKind.Miner:
                    var miner = h.GetComponent<MinerAI>();
                    selMode.text = miner.StateName;
                    selStats.text = $"Dig speed x{miner.DigSpeed:0.0#}     Walk speed {miner.WalkSpeed:0.0}\nGold per trip ~{c.goldPerTrip * GM.DigMultiplier:0.0}";
                    selHint.text = "[U] upgrade   ·   Esc: deselect";
                    break;
                case UnitKind.Barricade:
                    selMode.text = "Blocks raiders and bullets";
                    selStats.text = $"Width x{h.transform.localScale.x:0.0#}     Material: {(lv == 1 ? "sandbags" : lv == 2 ? "concrete" : "steel")}";
                    selHint.text = "[U] upgrade   ·   Esc: deselect";
                    break;
                case UnitKind.Mine:
                    selMode.text = $"{GM.GoldWaiting:0} gold waiting for the truck";
                    selStats.text = $"Dig bonus x{GM.MineDigMultiplier:0.0#}     Miners {Health.Count(UnitKind.Miner)} / {GM.MaxMiners}\nMorale dig x{GM.MoraleDigMultiplier:0.0#}";
                    selHint.text = "[U] upgrade   ·   [5] hire miner   ·   Esc: deselect";
                    break;
            }

            if (up == null || up.IsMax)
            {
                upgradeLabel.text = "MAX LEVEL";
                upgradeGroup.alpha = 0.5f;
                upgradeButton.interactable = false;
                selNext.text = "<color=#ffd34d>Fully upgraded.</color>";
            }
            else
            {
                bool afford = up.CanAfford;
                upgradeLabel.text = $"UPGRADE → {Upgradeable.Roman(lv + 1)}  <size=15>[U]</size>\n<color={(afford ? "#8fe28f" : "#ff7a6a")}>${up.NextCost}</color>";
                upgradeGroup.alpha = afford ? 1f : 0.45f;
                upgradeButton.interactable = afford;
                selNext.text = $"<color=#ffd34d>Next level:</color> {up.NextDescription}";
            }
        }

        Sprite IconFor(UnitKind k) => k switch
        {
            UnitKind.Soldier => icons.soldier,
            UnitKind.Tower => icons.tower,
            UnitKind.Miner => icons.miner,
            UnitKind.Barricade => icons.barricade,
            UnitKind.Mine => icons.pickaxe,
            UnitKind.Truck => icons.truck,
            _ => icons.soldier
        };

        // ---------------- Minimap ----------------
        const float MapWorldSize = 140f;

        void RefreshMinimap()
        {
            if (minimapContent == null) return;
            if (cam != null) minimapContent.localEulerAngles = new Vector3(0, 0, cam.transform.eulerAngles.y);
            dotCount = 0;
            foreach (var e in EnemyAI.All)
                if (!e.Concealed) Dot(e.transform.position, new Color(1f, 0.25f, 0.2f), 5f);
            foreach (var h in Health.All)
            {
                if (h.IsDead) continue;
                switch (h.kind)
                {
                    case UnitKind.Soldier: Dot(h.transform.position, new Color(0.35f, 0.95f, 0.35f), 5f); break;
                    case UnitKind.Tower: Dot(h.transform.position, new Color(0.2f, 0.75f, 0.25f), 8f); break;
                    case UnitKind.Miner: Dot(h.transform.position, new Color(1f, 0.85f, 0.2f), 4f); break;
                    case UnitKind.Barricade: Dot(h.transform.position, new Color(0.75f, 0.6f, 0.4f), 5f); break;
                    case UnitKind.Truck: Dot(h.transform.position, Color.white, 8f); break;
                    case UnitKind.Mine: Dot(h.transform.position + Vector3.forward, new Color(1f, 0.75f, 0.1f), 12f); break;
                }
            }
            if (Sel != null && Sel.Selected != null) Dot(Sel.Selected.transform.position, Color.white, 9f, true);
            Dot(GM.player.transform.position, new Color(0.3f, 0.6f, 1f), 8f, true);
            for (int i = dotCount; i < dotPool.Count; i++) if (dotPool[i].enabled) dotPool[i].enabled = false;
        }

        void Dot(Vector3 world, Color c, float size, bool outline = false)
        {
            if (dotCount >= dotPool.Count)
            {
                var img = NewImage(minimapContent, "Dot", null, c);
                img.raycastTarget = false;
                var o = img.gameObject.AddComponent<Outline>();
                o.effectColor = Color.black; o.effectDistance = new Vector2(1, -1);
                dotPool.Add(img);
            }
            var d = dotPool[dotCount++];
            d.enabled = true;
            d.color = c;
            var rt = d.rectTransform;
            float k = minimapContent.rect.width / MapWorldSize;
            rt.anchoredPosition = new Vector2(Mathf.Clamp(world.x, -70f, 70f) * k, Mathf.Clamp(world.z, -70f, 70f) * k);
            rt.sizeDelta = new Vector2(size, size);
            rt.localEulerAngles = Vector3.zero;
            d.GetComponent<Outline>().effectColor = outline ? Color.white : new Color(0, 0, 0, 0.8f);
        }

        // ---------------- Level badges ----------------
        void RefreshBadges()
        {
            int n = 0;
            if (cam != null)
            {
                foreach (var u in Upgradeable.All)
                {
                    if (u.Level < 2 || u.Health == null || u.Health.IsDead) continue;
                    Vector3 top = u.transform.position; top.y = u.Health.TopY + 0.5f;
                    if (!ToCanvas(top, out Vector2 local, out bool onScreen) || !onScreen) continue;
                    if (n >= badgePool.Count) badgePool.Add(MakeBadge());
                    var b = badgePool[n++];
                    b.gameObject.SetActive(true);
                    b.anchoredPosition = local;
                    var t = b.GetComponentInChildren<Text>();
                    t.text = Upgradeable.Roman(u.Level);
                    b.GetComponent<Image>().color = u.Level == 3 ? new Color(0.55f, 0.15f, 0.1f, 0.92f) : new Color(0.12f, 0.2f, 0.45f, 0.92f);
                }
            }
            for (int i = n; i < badgePool.Count; i++) if (badgePool[i].gameObject.activeSelf) badgePool[i].gameObject.SetActive(false);
        }

        RectTransform MakeBadge()
        {
            var img = NewImage(badgeRoot, "Badge", null, Color.black);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(40, 24);
            img.gameObject.AddComponent<Outline>().effectColor = Gold;
            var t = MakeText(rt, "Lv", "II", 18, TextAnchor.MiddleCenter, Gold);
            Place(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return rt;
        }

        // ---------------- Off-screen threat arrows ----------------
        void RefreshArrows()
        {
            int n = 0;
            var canvasRect = (RectTransform)transform;
            Vector2 half = canvasRect.rect.size * 0.5f;
            const float margin = 48f;
            for (int i = threats.Count - 1; i >= 0; i--)
            {
                var t = threats[i];
                if (Time.unscaledTime > t.until) { threats.RemoveAt(i); continue; }
                Vector3 world = t.follow != null ? t.follow.position : t.pos;
                if (cam == null) break;
                Vector3 sp = cam.WorldToScreenPoint(world + Vector3.up);
                bool behind = sp.z < 0;
                if (behind) sp = -sp;
                bool onScreen = !behind && sp.x > 0 && sp.x < Screen.width && sp.y > 0 && sp.y < Screen.height;
                if (onScreen) continue;

                Vector2 dir = new Vector2(sp.x - Screen.width * 0.5f, sp.y - Screen.height * 0.5f);
                if (dir.sqrMagnitude < 1f) dir = Vector2.up;
                Vector2 limit = half - new Vector2(margin, margin + 70f);
                float scale = Mathf.Min(limit.x / Mathf.Max(0.001f, Mathf.Abs(dir.x)), limit.y / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
                Vector2 pos = dir * scale;

                if (n >= arrowPool.Count)
                {
                    var img = NewImage(arrowRoot, "Arrow", icons.arrow, Color.white);
                    img.raycastTarget = false;
                    img.rectTransform.sizeDelta = new Vector2(48, 48);
                    img.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
                    arrowPool.Add(img);
                }
                var a = arrowPool[n++];
                a.enabled = true;
                a.color = t.color;
                a.rectTransform.anchoredPosition = pos;
                a.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
                a.rectTransform.localScale = Vector3.one * (1f + Mathf.PingPong(Time.unscaledTime * 2f, 0.25f));
            }
            for (int i = n; i < arrowPool.Count; i++) arrowPool[i].enabled = false;
        }

        bool ToCanvas(Vector3 world, out Vector2 local, out bool onScreen)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            onScreen = sp.z > 0 && sp.x > -20 && sp.x < Screen.width + 20 && sp.y > -20 && sp.y < Screen.height + 20;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, sp, null, out local);
        }

        static void SetBar(RectTransform fill, float f) => fill.anchorMax = new Vector2(Mathf.Clamp01(f), 1);

        public void ApplyPrices(GameConfig c)
        {
            barPrices[0].text = $"${c.barricadeCost}";
            barPrices[1].text = $"${c.soldierCost}";
            barPrices[2].text = $"${c.towerCost}";
            barPrices[3].text = $"${c.foodOrderCost}";
            barPrices[4].text = $"${c.minerCost}";
            shopFoodButton.GetComponentInChildren<Text>().text = $"Order food (+{c.foodPerOrder:0})  —  ${c.foodOrderCost}   [4]";
            shopMinerButton.GetComponentInChildren<Text>().text = $"Hire miner  —  ${c.minerCost}   [5]";
        }

        // =====================================================================
        // UI construction (run from the Editor builder; saved in the scene)
        // =====================================================================
        public void BuildUI(Font f, Icons ic, Sprite minimap)
        {
            font = f;
            icons = ic;
            minimapSprite = minimap;
            for (int i = transform.childCount - 1; i >= 0; i--) DestroyImmediate(transform.GetChild(i).gameObject);
            var root = (RectTransform)transform;

            // ---------- HUD ----------
            hudRoot = MakeRect(root, "HUD", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var hud = (RectTransform)hudRoot.transform;
            Place(hud, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // World overlays (behind panels)
            badgeRoot = MakeRect(hud, "LevelBadges", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Place(badgeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            arrowRoot = MakeRect(hud, "ThreatArrows", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Place(arrowRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Resources (top-left)
            var res = Panel(hud, "Resources", new Vector2(0, 1), new Vector2(18, -18), new Vector2(470, 214));
            moneyText = IconRow(res, ic.coin, 0, 32, new Color(0.6f, 1f, 0.6f));
            goldText = IconRow(res, ic.gold, 1, 24, Gold);
            foodText = IconRow(res, ic.food, 2, 24, Color.white);
            trucksText = IconRow(res, ic.truck, 3, 17, Info);
            unitsText = IconRow(res, ic.soldier, 4, 17, Info);

            // Wave (top-centre)
            var wave = Panel(hud, "Wave", new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(440, 132));
            var skull = NewImage(wave, "Skull", ic.skull, Color.white);
            Place(skull.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -66), new Vector2(66, -18));
            waveText = MakeText(wave, "Wave", "WAVE 0/10", 36, TextAnchor.MiddleLeft, Warn);
            Place(waveText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(78, -66), new Vector2(-10, -14));
            var clock = NewImage(wave, "Clock", ic.clock, Color.white);
            Place(clock.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(26, 34), new Vector2(54, 62));
            countdownText = MakeText(wave, "Countdown", "Next wave in 2:00", 24, TextAnchor.MiddleLeft, Color.white);
            Place(countdownText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(64, 30), new Vector2(-10, 66));
            sidesText = MakeText(wave, "Sides", "", 16, TextAnchor.MiddleLeft, new Color(0.8f, 0.8f, 0.8f));
            Place(sidesText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(64, 6), new Vector2(-10, 30));

            // Messages (under wave panel)
            messageRoot = MakeRect(hud, "Messages", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(1100, 220));
            messageRoot.pivot = new Vector2(0.5f, 1);
            messageRoot.anchoredPosition = new Vector2(0, -160);
            var mv = messageRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            mv.spacing = 4; mv.childControlHeight = true; mv.childControlWidth = true; mv.childForceExpandHeight = false; mv.childForceExpandWidth = false;
            mv.childAlignment = TextAnchor.UpperCenter;

            // Minimap + morale/mine (top-right)
            var mapPanel = Panel(hud, "Minimap", new Vector2(1, 1), new Vector2(-18, -18), new Vector2(300, 300));
            var mapLabel = MakeText(mapPanel, "Label", "MAP", 14, TextAnchor.UpperLeft, new Color(1, 1, 1, 0.6f));
            Place(mapLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -26), new Vector2(-10, -6));
            minimapContent = MakeRect(mapPanel, "MapContent", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(206, 206));
            var mapImg = minimapContent.gameObject.AddComponent<Image>();
            mapImg.sprite = minimap; mapImg.raycastTarget = false;
            minimapContent.localEulerAngles = new Vector3(0, 0, 45);
            var legend = MakeText(mapPanel, "Legend", "<color=#5aa0ff>■</color> you  <color=#5af05a>■</color> troops  <color=#ffd633>■</color> miners  <color=#ff4433>■</color> raiders", 13, TextAnchor.LowerCenter, Color.white);
            Place(legend.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(4, 4), new Vector2(-4, 24));

            var bars = Panel(hud, "MoraleMine", new Vector2(1, 1), new Vector2(-18, -326), new Vector2(460, 112));
            moraleText = IconBar(bars, ic.flag, 0, out moraleFill, out moraleFillImg, Good);
            mineText = IconBar(bars, ic.pickaxe, 1, out mineFill, out mineFillImg, Gold);

            // Player (bottom-left)
            var pl = Panel(hud, "Player", new Vector2(0, 0), new Vector2(18, 18), new Vector2(360, 112));
            healthText = IconBar(pl, ic.heart, 0, out healthFill, out healthFillImg, new Color(0.35f, 0.65f, 1f));
            ammoText = IconBar(pl, ic.ammo, 1, out reloadFill, out var reloadImg, Warn);
            reloadImg.color = new Color(0.62f, 0.48f, 0.15f);

            // Build bar (bottom-centre)
            var bar = MakeRect(hud, "BuildBar", new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(BarWidth, 104));
            bar.pivot = new Vector2(0.5f, 0);
            bar.anchoredPosition = new Vector2(0, 14);
            var barBg = bar.gameObject.AddComponent<Image>(); barBg.color = PanelColor;
            var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(8, 8, 8, 8); h.spacing = 6;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            var items = new (string key, string name, Sprite icon)[]
            {
                ("1", "Barricade", ic.barricade), ("2", "Soldier", ic.soldier), ("3", "Tower", ic.tower), ("4", "Food", ic.food),
                ("5", "Miner", ic.miner), ("Q", "Rally", ic.star), ("F", "Order", ic.target), ("Tab", "Shop", ic.crate)
            };
            barButtons = new Button[items.Length];
            barGroups = new CanvasGroup[items.Length];
            barPrices = new Text[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                var b = MakeButton(bar, "Bar_" + items[i].name, "", 1, out var lbl);
                DestroyImmediate(lbl.gameObject);
                var brt = (RectTransform)b.transform;
                barButtons[i] = b;
                barGroups[i] = b.gameObject.AddComponent<CanvasGroup>();
                var icon = NewImage(brt, "Icon", items[i].icon, Color.white);
                Place(icon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-19, -46), new Vector2(19, -8));
                var name = MakeText(brt, "Name", items[i].name, 17, TextAnchor.MiddleCenter, Color.white);
                Place(name.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(2, 22), new Vector2(-2, 44));
                barPrices[i] = MakeText(brt, "Price", i == 6 ? "nearby" : i == 7 ? "panel" : "", 16, TextAnchor.MiddleCenter, new Color(0.6f, 1f, 0.6f));
                Place(barPrices[i].rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(2, 2), new Vector2(-2, 22));
                var keyBg = NewImage(brt, "Key", null, new Color(1f, 0.82f, 0.2f, 0.95f));
                Place(keyBg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(3, -23), new Vector2(items[i].key.Length > 1 ? 39 : 23, -3));
                keyBg.raycastTarget = false;
                var key = MakeText(keyBg.rectTransform, "K", items[i].key, 14, TextAnchor.MiddleCenter, Color.black);
                Place(key.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                key.GetComponent<Outline>().enabled = false;
            }

            // Selection panel (bottom-right; lifted above the build bar on narrow screens)
            var sp = MakeRect(hud, "SelectionPanel", new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(SelWidth, 318));
            sp.pivot = new Vector2(1, 0);
            sp.anchoredPosition = new Vector2(-18, 14);
            var spBg = sp.gameObject.AddComponent<Image>();
            spBg.color = new Color(0.075f, 0.075f, 0.085f, 1f);
            var border = NewImage(sp, "Border", null, new Color(0.35f, 1f, 0.45f, 0.9f));
            Place(border.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -4), Vector2.zero);
            selPanel = sp.gameObject;
            var iconBg = NewImage(sp, "IconBg", null, new Color(0.18f, 0.18f, 0.2f));
            Place(iconBg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -86), new Vector2(86, -12));
            selIcon = NewImage(iconBg.rectTransform, "Icon", ic.soldier, Color.white);
            Place(selIcon.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));
            selName = MakeText(sp, "Name", "Soldier", 28, TextAnchor.MiddleLeft, Color.white);
            Place(selName.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(98, -48), new Vector2(-120, -12));
            selLevel = MakeText(sp, "Level", "LEVEL I", 20, TextAnchor.MiddleRight, Gold);
            Place(selLevel.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-130, -46), new Vector2(-12, -14));
            selMode = MakeText(sp, "Mode", "", 17, TextAnchor.MiddleLeft, new Color(0.85f, 0.85f, 0.85f));
            Place(selMode.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(98, -86), new Vector2(-12, -50));
            var hpBg = NewImage(sp, "HpBg", null, new Color(0.22f, 0.22f, 0.22f));
            Place(hpBg.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -120), new Vector2(-12, -96));
            selHpFill = NewImage(hpBg.rectTransform, "Fill", null, new Color(0.25f, 0.62f, 0.3f)).rectTransform;
            Place(selHpFill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            selHpText = MakeText(hpBg.rectTransform, "Hp", "", 17, TextAnchor.MiddleCenter, Color.white);
            Place(selHpText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            selStats = MakeText(sp, "Stats", "", 18, TextAnchor.UpperLeft, Color.white);
            Place(selStats.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -174), new Vector2(-12, -126));
            selNext = MakeText(sp, "Next", "", 16, TextAnchor.UpperLeft, Color.white);
            Place(selNext.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -224), new Vector2(-12, -178));
            upgradeButton = MakeButton(sp, "Upgrade", "", 20, out upgradeLabel);
            Place((RectTransform)upgradeButton.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 12), new Vector2(232, 62));
            upgradeGroup = upgradeButton.gameObject.AddComponent<CanvasGroup>();
            upgradeButton.image.color = new Color(0.2f, 0.42f, 0.2f, 1f);
            var upIcon = NewImage((RectTransform)upgradeButton.transform, "UpIcon", ic.up, Color.white);
            Place(upIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, -14), new Vector2(36, 14));
            Place(upgradeLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(36, 2), new Vector2(-4, -2));
            releaseButton = MakeButton(sp, "Release", "RELEASE [X]", 16, out _);
            Place((RectTransform)releaseButton.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(240, 12), new Vector2(358, 62));
            deselectButton = MakeButton(sp, "Deselect", "CLOSE [Esc]", 16, out _);
            Place((RectTransform)deselectButton.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-114, 12), new Vector2(-12, 62));
            selHint = MakeText(sp, "Hint", "", 14, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.7f));
            Place(selHint.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(14, 68), new Vector2(-12, 90));

            // Shop
            shopPanel = MakeModal(root, "ShopPanel", "SUPPLY & MANAGEMENT", new Vector2(760, 560), out var shop).gameObject;
            shopInfo = MakeText(shop, "Info", "", 22, TextAnchor.UpperLeft, Color.white);
            Place(shopInfo.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -210), new Vector2(-30, -80));
            shopFoodButton = MakeButton(shop, "BuyFood", "Order food", 24, out _);
            Place((RectTransform)shopFoodButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-320, -290), new Vector2(-10, -225));
            shopMinerButton = MakeButton(shop, "HireMiner", "Hire miner", 24, out _);
            Place((RectTransform)shopMinerButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(10, -290), new Vector2(320, -225));
            shopMineButton = MakeButton(shop, "SelectMine", "Upgrade the gold mine  [M]", 24, out _);
            Place((RectTransform)shopMineButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-320, -370), new Vector2(320, -305));
            shopWaveButton = MakeButton(shop, "CallWave", "Call next wave now", 22, out _);
            Place((RectTransform)shopWaveButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-320, -450), new Vector2(-10, -385));
            shopCloseButton = MakeButton(shop, "Close", "Close  [Tab]", 22, out _);
            Place((RectTransform)shopCloseButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(10, -450), new Vector2(320, -385));

            // ---------- Menus ----------
            menuPanel = MakeFullscreen(root, "MainMenu", new Color(0.08f, 0.07f, 0.05f, 0.94f)).gameObject;
            var menu = (RectTransform)menuPanel.transform;
            var title = MakeText(menu, "Title", "HOLD THE MINE", 104, TextAnchor.MiddleCenter, Gold);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -240), new Vector2(0, -90));
            title.GetComponent<Outline>().effectDistance = new Vector2(4, -4);
            var sub = MakeText(menu, "Subtitle", "The camp is behind you. Now the gold mine is yours to defend.\nSurvive 10 raider waves. Keep the miners fed, the trucks moving and morale high.", 26, TextAnchor.MiddleCenter, Info);
            Place(sub.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -340), new Vector2(0, -240));
            playButton = MakeButton(menu, "Play", "PLAY", 40, out _);
            Place((RectTransform)playButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180, 20), new Vector2(180, 100));
            playButton.image.color = new Color(0.2f, 0.42f, 0.2f, 0.98f);
            quitButton = MakeButton(menu, "Quit", "QUIT", 28, out _);
            Place((RectTransform)quitButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120, -55), new Vector2(120, 5));
            var controls = MakeText(menu, "Controls",
                "<color=#ffd34d>MOVE & FIGHT</color>   WASD move  ·  Mouse aim  ·  Left click shoot  ·  R reload  ·  Q rally nearby troops\n" +
                "<color=#ffd34d>BUILD</color>   1 Barricade  ·  2 Soldier  ·  3 Tower  (left click place, right click cancel, scroll rotates)\n" +
                "<color=#ffd34d>COMMAND</color>   Left click a unit/building to select it  ·  Right click ground: move & hold  ·  X release  ·  U upgrade  ·  F send nearby soldiers\n" +
                "<color=#ffd34d>MANAGE</color>   4 Order food  ·  5 Hire miner  ·  M select the mine  ·  Tab shop  ·  Esc deselect / pause\n\n" +
                "Desert (west): open ground, rocks block bullets.   Forest (east): raiders stay hidden until close or firing.",
                21, TextAnchor.UpperCenter, new Color(0.88f, 0.88f, 0.84f));
            Place(controls.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 50), new Vector2(-40, 300));

            pausePanel = MakeModal(root, "PausePanel", "PAUSED", new Vector2(520, 470), out var pause).gameObject;
            resumeButton = MakeButton(pause, "Resume", "Resume  [Esc]", 28, out _);
            Place((RectTransform)resumeButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-190, -170), new Vector2(190, -100));
            pauseRestartButton = MakeButton(pause, "Restart", "Restart", 28, out _);
            Place((RectTransform)pauseRestartButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-190, -260), new Vector2(190, -190));
            pauseMenuButton = MakeButton(pause, "Menu", "Main menu", 28, out _);
            Place((RectTransform)pauseMenuButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-190, -350), new Vector2(190, -280));

            victoryPanel = MakeModal(root, "VictoryPanel", "VICTORY — THE MINE HOLDS!", new Vector2(760, 520), out var vic).gameObject;
            victoryStats = MakeText(vic, "Stats", "", 26, TextAnchor.UpperCenter, Color.white);
            Place(victoryStats.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -360), new Vector2(-30, -90));
            victoryRestartButton = MakeButton(vic, "Restart", "Play again", 28, out _);
            Place((RectTransform)victoryRestartButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-300, 30), new Vector2(-20, 100));
            victoryMenuButton = MakeButton(vic, "Menu", "Main menu", 28, out _);
            Place((RectTransform)victoryMenuButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(20, 30), new Vector2(300, 100));

            gameOverPanel = MakeModal(root, "GameOverPanel", "GAME OVER", new Vector2(760, 520), out var over).gameObject;
            gameOverReason = MakeText(over, "Reason", "", 32, TextAnchor.MiddleCenter, Bad);
            Place(gameOverReason.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -150), new Vector2(-30, -90));
            gameOverStats = MakeText(over, "Stats", "", 26, TextAnchor.UpperCenter, Color.white);
            Place(gameOverStats.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -360), new Vector2(-30, -170));
            overRestartButton = MakeButton(over, "Restart", "Restart", 28, out _);
            Place((RectTransform)overRestartButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-300, 30), new Vector2(-20, 100));
            overMenuButton = MakeButton(over, "Menu", "Main menu", 28, out _);
            Place((RectTransform)overMenuButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(20, 30), new Vector2(300, 100));

            pausePanel.SetActive(false);
            victoryPanel.SetActive(false);
            gameOverPanel.SetActive(false);
            shopPanel.SetActive(false);
            selPanel.SetActive(false);
        }

        // ---------- helpers ----------
        static RectTransform MakeRect(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
        }

        static Image NewImage(RectTransform parent, string name, Sprite sprite, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = c;
            img.raycastTarget = false;
            if (sprite != null) img.preserveAspect = true;
            return img;
        }

        /// <summary>A dark panel anchored (and pivoted) at a corner/edge. Blocks clicks so they don't shoot.</summary>
        static RectTransform Panel(RectTransform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = MakeRect(parent, name, anchor, anchor, pos, size);
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = PanelColor;
            img.raycastTarget = true;
            return rt;
        }

        Text IconRow(RectTransform panel, Sprite icon, int row, int size, Color c)
        {
            float rowH = row == 0 ? 50 : 40;
            float top = row == 0 ? 10 : 10 + 50 + (row - 1) * 38;
            float ic = row >= 3 ? 22 : 32;
            var img = NewImage(panel, "Icon" + row, icon, Color.white);
            Place(img.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -top - (rowH + ic) / 2f), new Vector2(14 + ic, -top - (rowH - ic) / 2f));
            var t = MakeText(panel, "Text" + row, "", size, TextAnchor.MiddleLeft, c);
            Place(t.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(58, -top - rowH), new Vector2(-10, -top));
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        Text IconBar(RectTransform panel, Sprite icon, int row, out RectTransform fill, out Image fillImg, Color color)
        {
            float top = 10 + row * 50;
            var img = NewImage(panel, "Icon" + row, icon, Color.white);
            Place(img.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -top - 40), new Vector2(48, -top - 4));
            var bg = NewImage(panel, "BarBg" + row, null, new Color(0.2f, 0.2f, 0.22f, 0.95f));
            Place(bg.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(58, -top - 42), new Vector2(-12, -top - 2));
            fillImg = NewImage(bg.rectTransform, "Fill", null, color);
            fill = fillImg.rectTransform;
            Place(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var t = MakeText(bg.rectTransform, "Text", "", 19, TextAnchor.MiddleLeft, Color.white);
            var o = t.GetComponent<Outline>(); o.effectColor = new Color(0, 0, 0, 0.95f); o.effectDistance = new Vector2(1.5f, -1.5f);
            Place(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-6, 0));
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        RectTransform MakeFullscreen(RectTransform parent, string name, Color c)
        {
            var rt = MakeRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Place(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            return rt;
        }

        RectTransform MakeModal(RectTransform parent, string name, string title, Vector2 size, out RectTransform box)
        {
            var dim = MakeFullscreen(parent, name, new Color(0, 0, 0, 0.55f));
            box = MakeRect(dim, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            box.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.11f, 0.09f, 0.97f);
            box.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 0.8f, 0.3f, 0.6f);
            var t = MakeText(box, "Title", title, 40, TextAnchor.MiddleCenter, Gold);
            Place(t.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -75), new Vector2(-10, -15));
            return dim;
        }

        Text MakeText(RectTransform parent, string name, string text, int size, TextAnchor anchor, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = c;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var o = go.AddComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, 0.65f);
            o.effectDistance = new Vector2(1.2f, -1.2f);
            return t;
        }

        Button MakeButton(RectTransform parent, string name, string label, int size, out Text text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = ButtonColor;
            var b = go.AddComponent<Button>();
            var colors = b.colors;
            colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            colors.disabledColor = new Color(0.8f, 0.8f, 0.8f);
            b.colors = colors;
            go.AddComponent<Outline>().effectColor = new Color(1f, 0.8f, 0.3f, 0.35f);
            text = MakeText((RectTransform)go.transform, "Label", label, size, TextAnchor.MiddleCenter, Color.white);
            Place(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(4, 2), new Vector2(-4, -2));
            return b;
        }
    }
}
