using UnityEngine;

public class TrainCardTriggerRelay : MonoBehaviour
{
	[SerializeField] private TrainGameController controller;

	private void Awake()
	{
		if (controller == null)
			controller = GetComponentInParent<TrainGameController>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (controller != null)
			controller.CardEnter(other);
	}

	private void OnTriggerExit(Collider other)
	{
		if (controller != null)
			controller.CardExit(other);
	}
}