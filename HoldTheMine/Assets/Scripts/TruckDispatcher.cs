using UnityEngine;

namespace HoldTheMine
{
    /// <summary>Sends the gold and food trucks down the road on their timers.</summary>
    public class TruckDispatcher : MonoBehaviour
    {
        public Transform[] roadPoints; // edge → depot
        public GameObject goldTruckPrefab;
        public GameObject foodTruckPrefab;

        public float GoldTruckIn { get; private set; }
        public float FoodTruckIn { get; private set; }
        public bool GoldTruckActive => goldTruck != null;
        public bool FoodTruckActive => foodTruck != null;

        Truck goldTruck, foodTruck;
        GameManager GM => GameManager.Instance;

        void Start()
        {
            GoldTruckIn = GM.config.goldTruckFirstDelay;
            FoodTruckIn = GM.config.foodTruckFirstDelay;
        }

        void Update()
        {
            if (GM == null || !GM.IsPlaying) return;
            if (goldTruck == null)
            {
                GoldTruckIn -= Time.deltaTime;
                if (GoldTruckIn <= 0) { goldTruck = Send(goldTruckPrefab); GM.ui.ShowMessage("Gold truck on the road.", UIManager.Info); }
            }
            if (foodTruck == null)
            {
                FoodTruckIn -= Time.deltaTime;
                if (FoodTruckIn <= 0)
                {
                    foodTruck = Send(foodTruckPrefab);
                    GM.ui.ShowMessage(GM.PendingFoodOrders > 0 ? "Food truck on the road." : "Food truck coming, but nothing is ordered (key 4)!", GM.PendingFoodOrders > 0 ? UIManager.Info : UIManager.Warn);
                }
            }
        }

        Truck Send(GameObject prefab)
        {
            var go = Instantiate(prefab, roadPoints[0].position, Quaternion.LookRotation(roadPoints[1].position - roadPoints[0].position), transform);
            var t = go.GetComponent<Truck>();
            t.Init(roadPoints);
            var h = go.GetComponent<Health>();
            float nextWarn = 0f;
            h.Damaged += _ =>
            {
                if (Time.time < nextWarn) return;
                nextWarn = Time.time + 15f;
                if (t != null) GM.ui.Alert($"The {(t.kind == TruckKind.Gold ? "gold" : "food")} truck is under attack!", UIManager.Bad, t.transform);
            };
            return t;
        }

        public void OnTruckGone(Truck t)
        {
            if (t == goldTruck) { goldTruck = null; GoldTruckIn = GM.config.goldTruckInterval; }
            if (t == foodTruck) { foodTruck = null; FoodTruckIn = GM.config.foodTruckInterval; }
        }
    }
}
