using UnityEngine;

public class NpcOrderButton : MonoBehaviour
{
    [SerializeField] private NpcCustomer npc;
    [SerializeField] private NpcOrderManager orderManager;

    public void PressButton()
    {
        if (npc == null || orderManager == null)
            return;

        orderManager.RevealNpcOrderFromClient(npc);
    }
}