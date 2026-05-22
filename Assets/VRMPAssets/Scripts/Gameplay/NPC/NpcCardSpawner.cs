using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NpcCardSpawner : NetworkBehaviour
{
    [SerializeField] private Transform cardsRoot;

    private readonly List<GameObject> cards = new();
    private bool initialized = false;

    private readonly NetworkVariable<int> visibleCardsCount = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        InitializeCards();
        visibleCardsCount.OnValueChanged += OnVisibleCardsCountChanged;
        ApplyVisibilityState(visibleCardsCount.Value);
    }

    public override void OnNetworkDespawn()
    {
        visibleCardsCount.OnValueChanged -= OnVisibleCardsCountChanged;
        base.OnNetworkDespawn();
    }

    private void InitializeCards()
    {
        if (initialized)
            return;

        if (cardsRoot == null)
            return;

        cards.Clear();

        foreach (Transform child in cardsRoot)
        {
            cards.Add(child.gameObject);
        }

        initialized = true;
    }

    public void RequestSpawnCard()
    {
        if (!IsSpawned)
        {
            return;
        }

        SpawnCardServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SpawnCardServerRpc(RpcParams rpcParams = default)
    {
        if (!initialized)
            InitializeCards();

        if (cards.Count == 0)
            return;

        int currentVisible = visibleCardsCount.Value;

        if (currentVisible >= cards.Count)
            return;

        visibleCardsCount.Value = currentVisible + 1;
        ApplyVisibilityState(visibleCardsCount.Value);
    }

    public void RequestRemoveAll()
    {
        if (!IsSpawned)
        {

            return;
        }

        RemoveAllServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RemoveAllServerRpc(RpcParams rpcParams = default)
    {
        visibleCardsCount.Value = 0;
        ApplyVisibilityState(visibleCardsCount.Value);
    }

    private void OnVisibleCardsCountChanged(int previousValue, int newValue)
    {
        if (!initialized)
            InitializeCards();

        ApplyVisibilityState(newValue);
    }

    private void SetCardVisible(GameObject card, bool visible)
    {
        foreach (Renderer r in card.GetComponentsInChildren<Renderer>(true))
            r.enabled = visible;

        foreach (Collider c in card.GetComponentsInChildren<Collider>(true))
            c.enabled = visible;

        foreach (Canvas canvas in card.GetComponentsInChildren<Canvas>(true))
            canvas.enabled = visible;

        foreach (XRGrabInteractable grab in card.GetComponentsInChildren<XRGrabInteractable>(true))
            grab.enabled = visible;

        foreach (Rigidbody rb in card.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.detectCollisions = visible;
            rb.isKinematic = !visible;

            if (visible)
                rb.WakeUp();
            else
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void ApplyVisibilityState(int visibleCount)
    {
        if (!initialized)
            InitializeCards();

        for (int i = 0; i < cards.Count; i++)
        {
            GameObject card = cards[i];
            if (card == null)
                continue;

            bool visible = i < visibleCount;
            SetCardVisible(card, visible);
        }
    }
}