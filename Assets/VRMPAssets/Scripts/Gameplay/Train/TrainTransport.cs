using UnityEngine;

public class TrainTransport : MonoBehaviour
{
    public Vector3 localDirection = Vector3.forward;
    public float speed = 2f;

    private void OnCollisionStay(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb == null) return;

        Vector3 worldDir = transform.TransformDirection(localDirection.normalized);

        Vector3 velocity = rb.linearVelocity;
        velocity.x = worldDir.x * speed;
        velocity.z = worldDir.z * speed;

        rb.linearVelocity = velocity;
    }
}