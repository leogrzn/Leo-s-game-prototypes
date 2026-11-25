using UnityEngine;

namespace HoldTheMine
{
    public enum TruckKind { Gold, Food }

    /// <summary>Drives along the road waypoints to the depot, loads/unloads, then drives back off the map.</summary>
    public class Truck : MonoBehaviour
    {
        public TruckKind kind;
        public GameObject cargoVisual;

        Transform[] path;     // path[0] = map edge, path[last] = depot
        int index;
        bool leaving, loading;
        float loadTimer;
        float cargoGold;
        Health health;

        GameManager GM => GameManager.Instance;
        public bool Loaded => cargoGold > 0;

        public void Init(Transform[] road)
        {
            path = road;
            health = GetComponent<Health>();
            health.SetMax(GM.config.truckHealth);
            health.Died += OnDestroyed;
            transform.position = path[0].position;
            index = 1;
            if (cargoVisual) cargoVisual.SetActive(kind == TruckKind.Food && GM.PendingFoodOrders > 0);
        }

        void Update()
        {
            if (GM == null || !GM.IsPlaying || health.IsDead) return;
            if (loading)
            {
                loadTimer -= Time.deltaTime;
                if (loadTimer <= 0) FinishLoading();
                return;
            }
            Vector3 target = path[index].position;
            Vector3 to = target - transform.position; to.y = 0;
            float step = GM.config.truckSpeed * Time.deltaTime;
            if (to.magnitude <= step)
            {
                transform.position = target;
                if (!leaving)
                {
                    if (index == path.Length - 1) { loading = true; loadTimer = GM.config.truckLoadTime; StartLoading(); }
                    else index++;
                }
                else
                {
                    if (index == 0) { LeaveMap(); return; }
                    index--;
                }
            }
            else
            {
                transform.position += to.normalized * step;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 6f * Time.deltaTime);
            }
        }

        void StartLoading()
        {
            GM.ui.ShowMessage(kind == TruckKind.Gold ? "Gold truck arrived at the mine — loading." : "Food truck arrived — unloading.", UIManager.Info);
        }

        void FinishLoading()
        {
            loading = false;
            leaving = true;
            index = path.Length - 2;
            if (kind == TruckKind.Gold)
            {
                cargoGold = GM.GoldWaiting;
                GM.GoldWaiting = 0;
                int value = Mathf.RoundToInt(cargoGold * GM.config.goldValue);
                if (cargoVisual) cargoVisual.SetActive(cargoGold > 0);
                if (GM.config.payGoldOnPickup)
                {
                    GM.AddMoney(value);
                    GM.ui.ShowMessage($"Gold truck picked up {cargoGold:0} gold: +${value}", UIManager.Good);
                    cargoGold = 0.001f;
                }
                else
                {
                    GM.ui.ShowMessage(cargoGold > 0 ? $"Gold truck loaded {cargoGold:0} gold (${value}) — protect it until it leaves!" : "Gold truck left empty. Dig more gold!", cargoGold > 0 ? UIManager.Info : UIManager.Warn);
                }
            }
            else
            {
                float food = GM.DeliverFood();
                if (cargoVisual) cargoVisual.SetActive(false);
                GM.ui.ShowMessage(food > 0 ? $"Food delivered: +{food:0} food. Morale up!" : "Food truck had nothing to deliver — order food (key 4).", food > 0 ? UIManager.Good : UIManager.Warn);
            }
        }

        void LeaveMap()
        {
            if (kind == TruckKind.Gold && !GM.config.payGoldOnPickup && cargoGold > 0)
            {
                int value = Mathf.RoundToInt(cargoGold * GM.config.goldValue);
                GM.AddMoney(value);
                GM.ui.ShowMessage($"Gold truck got through: +${value}", UIManager.Good);
            }
            GM.trucks.OnTruckGone(this);
            Destroy(gameObject);
        }

        void OnDestroyed(Health _)
        {
            Explosion.SpawnFireball(transform.position, 3.5f);
            GM.AddMorale(GM.config.moraleTruckDestroyed);
            if (kind == TruckKind.Gold)
            {
                if (!leaving) GM.ui.ShowMessage("Gold truck destroyed before pickup!", UIManager.Bad);
                else GM.ui.ShowMessage($"Gold truck destroyed — {cargoGold:0} gold lost!", UIManager.Bad);
            }
            else
            {
                if (!leaving) { GM.LoseFoodOrders(); GM.ui.ShowMessage("Food truck destroyed — no food delivery!", UIManager.Bad); }
                else GM.ui.ShowMessage("Food truck destroyed on its way out.", UIManager.Bad);
            }
            GM.trucks.OnTruckGone(this);
        }
    }
}
