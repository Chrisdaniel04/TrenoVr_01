using UnityEngine;

public class TrainSpeedLimiter : MonoBehaviour
{
    [SerializeField] private float speedLimit = 100f;
    [SerializeField] private float fuelPenalty = 100f;
    [SerializeField] private TrainGameController trainController;

    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Limitator"))
            return;


        if (trainController != null)
        {
            float normalizedLimit = speedLimit / 100f;
            trainController.LimitSpeed(normalizedLimit, fuelPenalty);
        }
        
    }
}