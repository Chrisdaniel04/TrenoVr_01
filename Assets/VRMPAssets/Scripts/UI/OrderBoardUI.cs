using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OrderBoardUI : MonoBehaviour
{
    [SerializeField] private NpcOrderManager orderManager;
    [SerializeField] private TMP_Text orderText1;
    [SerializeField] private TMP_Text orderText2;

    private void Update()
    {
        if (orderManager == null)
            return;

        List<FoodType> revealedOrders = orderManager.GetRevealedOrders();

        if (orderText1 != null)
        {
            if (revealedOrders.Count > 0)
                orderText1.text = GetFoodName(revealedOrders[0]);
            else
                orderText1.text = "";
        }

        if (orderText2 != null)
        {
            if (revealedOrders.Count > 1)
                orderText2.text = GetFoodName(revealedOrders[1]);
            else
                orderText2.text = "";
        }
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