using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RequestCardSequenceActivator : NetworkBehaviour
{
    [Header("Cards Roots")]
    [SerializeField] private Transform meatRoot;
    [SerializeField] private Transform fishRoot;
    [SerializeField] private Transform eggRoot;
    [SerializeField] private Transform riceRoot;
    [SerializeField] private Transform pastaRoot;
    [SerializeField] private Transform coalRoot;

    private readonly Dictionary<string, List<GameObject>> cardsByType = new();
    private readonly Dictionary<string, int> visibleCountByType = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        InitializeCategory("meat", meatRoot);
        InitializeCategory("fish", fishRoot);
        InitializeCategory("egg", eggRoot);
        InitializeCategory("rice", riceRoot);
        InitializeCategory("pasta", pastaRoot);
        InitializeCategory("coal", coalRoot);
    }

    private void InitializeCategory(string key, Transform root)
    {
        if (root == null)
        {

            cardsByType[key] = new List<GameObject>();
            visibleCountByType[key] = 0;
            return;
        }

        List<GameObject> list = new();

        foreach (Transform child in root)
        {
            list.Add(child.gameObject);
            SetCardVisible(child.gameObject, false);
        }

        cardsByType[key] = list;
        visibleCountByType[key] = 0;


    }

    public void RequestMeat() => RequestShowNextCardServerRpc("meat");
    public void RequestFish() => RequestShowNextCardServerRpc("fish");
    public void RequestEgg() => RequestShowNextCardServerRpc("egg");
    public void RequestRice() => RequestShowNextCardServerRpc("rice");
    public void RequestPasta() => RequestShowNextCardServerRpc("pasta");
    public void RequestCoal() => RequestShowNextCardServerRpc("coal");

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestShowNextCardServerRpc(string category, RpcParams rpcParams = default)
    {
        if (!cardsByType.ContainsKey(category))
        {
            return;
        }

        List<GameObject> cards = cardsByType[category];
        int visibleCount = visibleCountByType[category];

        if (cards.Count == 0)
        {
            return;
        }

        if (visibleCount >= cards.Count)
        {
            return;
        }

        ShowCardClientRpc(category, visibleCount);

        visibleCountByType[category] = visibleCount + 1;
    }

    [Rpc(SendTo.Everyone)]
    private void ShowCardClientRpc(string category, int index, RpcParams rpcParams = default)
    {
        if (!cardsByType.ContainsKey(category))
            return;

        List<GameObject> cards = cardsByType[category];

        if (index < 0 || index >= cards.Count)
            return;

        GameObject card = cards[index];
        if (card == null)
            return;

        SetCardVisible(card, true);


    }

    private void SetCardVisible(GameObject card, bool visible)
    {
        if (card == null)
            return;

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
}