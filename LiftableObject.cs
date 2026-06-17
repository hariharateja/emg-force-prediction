using UnityEngine;

public class LiftableObject : MonoBehaviour
{
    [Header("Object Properties")]
    [Tooltip("Weight of the object in kilograms. Must be <= HandLifter.maxLiftingCapacity to be picked up.")]
    public float weightKg = 1.0f;

    private Rigidbody rb;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    // Called by HandLifter when the object falls out of bounds or on full reset.
    public void ResetPosition()
    {
        // Detach from any parent (e.g. residual joint) before resetting.
        transform.SetParent(null);

        // Remove any FixedJoint left over from a grip
        FixedJoint joint = GetComponent<FixedJoint>();
        if (joint != null) Destroy(joint);

        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }
}
