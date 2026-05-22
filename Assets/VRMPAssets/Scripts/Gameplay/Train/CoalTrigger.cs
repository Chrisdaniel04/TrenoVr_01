using UnityEngine;
using Unity.Netcode;

public class CoalTrigger : NetworkBehaviour
{
    [SerializeField] private string coalTag = "Coal";
    [SerializeField] private float fuelToAddPerCoal = 250f;
    [SerializeField] private TrainGameController trainGameController;

    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag(coalTag))
        {
            return;
        }

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            return;
        }

        if (trainGameController != null)
        {
            trainGameController.AddFuel(fuelToAddPerCoal);
        }

        netObj.Despawn(true);
    }
}