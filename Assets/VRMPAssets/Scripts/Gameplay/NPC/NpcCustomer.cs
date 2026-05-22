using TMPro;
using UnityEngine;

public class NpcCustomer : MonoBehaviour
{
    [Header("NPC")]
    [SerializeField] private int npcId;

    [Header("Visual")]
    [SerializeField] private GameObject requestButtonObject;
    [SerializeField] private GameObject orderTextRoot;
    [SerializeField] private TMP_Text orderText;

    private FoodType requestedFood = FoodType.None;
    private NpcOrderState orderState = NpcOrderState.Idle;

    private bool orderRevealed = false;

    public int NpcId
    {
        get { return npcId; }
    }

    public FoodType RequestedFood
    {
        get { return requestedFood; }
    }

    public NpcOrderState OrderState
    {
        get { return orderState; }
    }

    public bool IsOrderRevealed
    {
        get { return orderRevealed; }
    }

    public void SetOrder(FoodType food)
    {
        requestedFood = food;
        orderState = NpcOrderState.WaitingOrder;
        orderRevealed = false;
        RefreshVisual();
    }

    public void RevealOrder()
    {
        if (orderState != NpcOrderState.WaitingOrder)
            return;

        orderRevealed = true;
        RefreshVisual();
    }

    public void MarkServed()
    {
        orderState = NpcOrderState.Served;
        RefreshVisual();
    }

    public void ClearOrder()
    {
        requestedFood = FoodType.None;
        orderState = NpcOrderState.Idle;
        orderRevealed = false;
        RefreshVisual();
    }

    public bool IsWaitingForOrder()
    {
        return orderState == NpcOrderState.WaitingOrder;
    }

    public bool IsIdle()
    {
        return orderState == NpcOrderState.Idle;
    }

    public bool MatchesFood(FoodType food)
    {
        return requestedFood == food;
    }

    public void ApplyState(FoodType food, NpcOrderState state, bool revealed)
    {
        requestedFood = food;
        orderState = state;
        orderRevealed = revealed;
        RefreshVisual();
    }

    private void Start()
    {
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        bool hasActiveOrder = orderState == NpcOrderState.WaitingOrder;

        if (requestButtonObject != null)
            requestButtonObject.SetActive(hasActiveOrder && !orderRevealed);

        if (orderTextRoot != null)
            orderTextRoot.SetActive(hasActiveOrder && orderRevealed);

        if (orderText != null && hasActiveOrder)
            orderText.text = GetFoodName(requestedFood);
    }

    private string GetFoodName(FoodType food)
    {
        switch (food)
        {
            case FoodType.CookedEgg:
                return "Cooked Egg";

            case FoodType.CookedMeat:
                return "Cooked Meat";

            case FoodType.CookedPasta:
                return "Cooked Pasta";

            case FoodType.CookedFish:
                return "Cooked Fish";

            case FoodType.CookedRice:
                return "Cooked Rice";

            default:
                return "";
        }
    }
}