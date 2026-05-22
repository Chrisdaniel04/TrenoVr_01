using UnityEngine;

public class TrainEndGameTrigger : MonoBehaviour
{
    [SerializeField] private TrainGameController controller;
    [SerializeField] private string trainTag = "Train";

    private void OnTriggerEnter(Collider other)
    {
        Transform root = other.transform.root;

        if (!root.CompareTag(trainTag))
            return;



        if (controller == null)
        {
            return;
        }

        controller.ApplyEndGameFromTrigger();
    }
}