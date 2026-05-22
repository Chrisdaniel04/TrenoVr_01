using UnityEngine;

public class TrainPassengerTriggerRelay : MonoBehaviour
{
    [SerializeField] private TrainGameController controller;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponentInParent<TrainGameController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (controller != null) controller.PassengerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (controller != null) controller.PassengerExit(other);
    }
}