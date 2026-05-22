using UnityEngine;

public class TrainCardCarrier : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;

    private Transform originalParent;
    private bool isCarried = false;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        originalParent = transform.parent;
    }

    public void AttachToTrain(Transform train)
    {
        if (isCarried || train == null) return;

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        transform.SetParent(train, true);

        isCarried = true;
    }

    public void DetachFromTrain()
    {
        if (!isCarried) return;

        transform.SetParent(originalParent, true);


        isCarried = false;
    }
}