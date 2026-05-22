using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NpcOrderManager : NetworkBehaviour
{
    #region Inspector

    [Header("NPC")]
    [SerializeField] private List<NpcCustomer> npcs = new List<NpcCustomer>();

    [Header("Card Spawn")]
    [SerializeField] private NpcCardSpawner cardSpawner;

    [Header("Settings")]
    [SerializeField] private int maxActiveOrders = 2;
    [SerializeField] private float nextOrderDelay = 1f;

    #endregion

    #region Private Fields

    private readonly List<FoodType> availableFoods = new List<FoodType>
    {
        FoodType.CookedEgg,
        FoodType.CookedMeat,
        FoodType.CookedPasta,
        FoodType.CookedFish,
        FoodType.CookedRice
    };

    #endregion

    #region Unity / Network Lifecycle

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InitializeOrders();
    }

    #endregion

    #region Initialization

    private void InitializeOrders()
    {
        FillOrdersServerSide();
        SyncAllNpcStatesToEveryoneRpc(BuildFoodArray(), BuildStateArray(), BuildRevealArray());
    }

    private void FillOrdersServerSide()
    {
        while (CountActiveOrders() < maxActiveOrders)
        {
            NpcCustomer freeNpc = GetRandomIdleNpc();
            if (freeNpc == null)
                return;

            FoodType randomFood = GetRandomFood();
            freeNpc.SetOrder(randomFood);
        }
    }

    #endregion

    #region Order Flow

    public void RevealNpcOrderFromClient(NpcCustomer npc)
    {
        if (npc == null)
            return;

        RevealNpcOrderRpc(npc.NpcId);
    }

    [Rpc(SendTo.Server)]
    private void RevealNpcOrderRpc(int npcId, RpcParams rpcParams = default)
    {
        NpcCustomer npc = GetNpcById(npcId);
        if (npc == null)
            return;

        if (!npc.IsWaitingForOrder())
            return;

        npc.RevealOrder();
        SyncAllNpcStatesToEveryoneRpc(BuildFoodArray(), BuildStateArray(), BuildRevealArray());
    }

    public void TryServeNpcFromClient(NpcCustomer npc, FoodType deliveredFood)
    {
        if (npc == null)
            return;

        TryServeNpcRpc(npc.NpcId, (int)deliveredFood);
    }

    [Rpc(SendTo.Server)]
    private void TryServeNpcRpc(int npcId, int deliveredFoodValue, RpcParams rpcParams = default)
    {
        NpcCustomer npc = GetNpcById(npcId);
        if (npc == null)
            return;

        FoodType deliveredFood = (FoodType)deliveredFoodValue;

        if (!npc.IsWaitingForOrder())
            return;

        if (!npc.MatchesFood(deliveredFood))
            return;

        StartCoroutine(ServeNpcRoutine(npc));
    }

    private IEnumerator ServeNpcRoutine(NpcCustomer npc)
    {
        npc.MarkServed();
        SyncAllNpcStatesToEveryoneRpc(BuildFoodArray(), BuildStateArray(), BuildRevealArray());

        if (cardSpawner != null)
        {
            cardSpawner.RequestSpawnCard();
        }

        yield return new WaitForSeconds(0.5f);

        npc.ClearOrder();
        SyncAllNpcStatesToEveryoneRpc(BuildFoodArray(), BuildStateArray(), BuildRevealArray());

        yield return new WaitForSeconds(nextOrderDelay);

        FillOrdersServerSide();
        SyncAllNpcStatesToEveryoneRpc(BuildFoodArray(), BuildStateArray(), BuildRevealArray());
    }

    #endregion

    #region Helpers

    private int CountActiveOrders()
    {
        int count = 0;

        foreach (var npc in npcs)
        {
            if (npc != null && npc.IsWaitingForOrder())
                count++;
        }

        return count;
    }

    private NpcCustomer GetRandomIdleNpc()
    {
        List<NpcCustomer> idleNpcs = new List<NpcCustomer>();

        foreach (var npc in npcs)
        {
            if (npc != null && npc.IsIdle())
                idleNpcs.Add(npc);
        }

        if (idleNpcs.Count == 0)
            return null;

        int index = Random.Range(0, idleNpcs.Count);
        return idleNpcs[index];
    }

    private FoodType GetRandomFood()
    {
        int index = Random.Range(0, availableFoods.Count);
        return availableFoods[index];
    }

    private NpcCustomer GetNpcById(int npcId)
    {
        foreach (var npc in npcs)
        {
            if (npc != null && npc.NpcId == npcId)
                return npc;
        }

        return null;
    }

    #endregion

    #region Sync

    private int[] BuildFoodArray()
    {
        int[] foods = new int[npcs.Count];

        for (int i = 0; i < npcs.Count; i++)
        {
            foods[i] = npcs[i] != null ? (int)npcs[i].RequestedFood : (int)FoodType.None;
        }

        return foods;
    }

    private int[] BuildStateArray()
    {
        int[] states = new int[npcs.Count];

        for (int i = 0; i < npcs.Count; i++)
        {
            states[i] = npcs[i] != null ? (int)npcs[i].OrderState : (int)NpcOrderState.Idle;
        }

        return states;
    }

    private bool[] BuildRevealArray()
    {
        bool[] reveals = new bool[npcs.Count];

        for (int i = 0; i < npcs.Count; i++)
        {
            reveals[i] = npcs[i] != null && npcs[i].IsOrderRevealed;
        }

        return reveals;
    }

    [Rpc(SendTo.Everyone)]
    private void SyncAllNpcStatesToEveryoneRpc(int[] foods, int[] states, bool[] reveals, RpcParams rpcParams = default)
    {
        int count = Mathf.Min(npcs.Count, Mathf.Min(foods.Length, Mathf.Min(states.Length, reveals.Length)));

        for (int i = 0; i < count; i++)
        {
            if (npcs[i] == null)
                continue;

            npcs[i].ApplyState((FoodType)foods[i], (NpcOrderState)states[i], reveals[i]);
        }
    }

    #endregion

    #region Public API

    public List<FoodType> GetRevealedOrders()
    {
        List<FoodType> revealedOrders = new List<FoodType>();

        foreach (var npc in npcs)
        {
            if (npc != null && npc.IsWaitingForOrder() && npc.IsOrderRevealed)
            {
                revealedOrders.Add(npc.RequestedFood);
            }
        }

        return revealedOrders;
    }

    #endregion
}