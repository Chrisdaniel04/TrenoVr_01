using TMPro;
using UnityEngine;

public class TrainSpeedDisplay : MonoBehaviour
{
    [SerializeField] private TrainGameController trainController;
    [SerializeField] private TextMeshPro speedText;
    [SerializeField] private TextMeshPro fuelText;
    [SerializeField] private bool useKmH = true;

    private void Update()
    {
        if (trainController == null || speedText == null)
            return;

        float speed = trainController.GetCurrentSpeed();
        float fuel = trainController.GetFuelMeters();

        if (useKmH)
        {
            speed *= 100f;
        }

        speedText.text = speed.ToString("F1");
        fuelText.text = fuel.ToString("F1");

    }
}
