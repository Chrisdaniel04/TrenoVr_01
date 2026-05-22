using Unity.Netcode;
using UnityEngine;

public class NpcServePoint : MonoBehaviour
{
    [SerializeField] private NpcCustomer npc;
    [SerializeField] private NpcOrderManager orderManager;

    private void OnTriggerEnter(Collider other)
    {
        if (npc == null || orderManager == null)
            return;

        FoodType deliveredFood = GetFoodTypeFromTag(other.tag);

        if (deliveredFood == FoodType.None)
            return;

        if (!npc.MatchesFood(deliveredFood))
            return;

        orderManager.TryServeNpcFromClient(npc, deliveredFood);

        RemoveDeliveredObject(other.gameObject);
    }

    private void RemoveDeliveredObject(GameObject obj)
    {
        NetworkObject networkObject = obj.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            if (networkObject.IsSpawned)
                networkObject.Despawn(true);
            else
                Destroy(obj);
        }
        else
        {
            Destroy(obj);
        }
    }

    private FoodType GetFoodTypeFromTag(string tagName)
    {
        switch (tagName)
        {
            case "CookedEgg":
                return FoodType.CookedEgg;
            case "CookedMeat":
                return FoodType.CookedMeat;
            case "CookedPasta":
                return FoodType.CookedPasta;
            case "CookedFish":
                return FoodType.CookedFish;
            case "CookedRice":
                return FoodType.CookedRice;
            default:
                return FoodType.None;
        }
    }
}