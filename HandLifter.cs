using UnityEngine;

/// <summary>
/// Drives a three-phase hand animation: Approach → Grip → Lift.
///
/// Phase 1 — Approach:  When force exceeds minApproachForce the hand extends
///                       forward (along its local +Z) by approachOffset so the
///                       fingers move into the object's wrap zone.
/// Phase 2 — Grip:      Once within gripRange AND force is sufficient, a
///                       FixedJoint locks the object after gripHoldTime seconds
///                       of sustained force (prevents instant snap-grip).
/// Phase 3 — Lift:      The Hand_Controller rises to liftHeight, carrying the
///                       attached object with it.
/// Release:             If force drops the joint is destroyed, the object falls,
///                       and the hand retracts to its rest position.
/// </summary>
public class HandLifter : MonoBehaviour
{
    [Header("Approach")]
    [Tooltip("Force level (0-1) that triggers the forward reach.")]
    public float minApproachForce = 0.10f;

    [Tooltip("How far (metres) the hand extends forward to reach the object.")]
    public float approachOffset = 0.30f;

    [Tooltip("Speed of the approach / retract movement.")]
    public float approachSpeed = 3f;

    [Header("Grip")]
    [Tooltip("Maximum lifting capacity at 100% grasp force (kg).")]
    public float maxLiftingCapacity = 5.0f;

    [Tooltip("Distance (metres) from the palm centre to trigger grip detection.")]
    public float gripRange = 0.70f;

    [Tooltip("Force must stay above threshold for this many seconds before the grip locks.")]
    public float gripHoldTime = 0.50f;

    [Tooltip("Height (Y world-units) of the palm above Hand_Controller. "
           + "Used to offset grip-distance check from controller origin.")]
    public float palmHeight = 1.0f;

    [Header("Lift")]
    [Tooltip("Speed of the upward lift movement.")]
    public float liftSpeed = 2.5f;

    [Tooltip("Y world-position the Hand_Controller rises to when lifting.")]
    public float liftHeight = 1.5f;

    [Header("References")]
    public GraspForceReceiver receiver;
    public LiftableObject targetObject;

    // ── private state ────────────────────────────────────────────────────────
    private Vector3 restPosition;
    private Quaternion restRotation;
    private bool isApproaching = false;
    private bool isHolding = false;
    private float gripHoldTimer = 0f;
    private float initialObjectY;

    private void Start()
    {
        restPosition = transform.position;
        restRotation = transform.rotation;

        if (receiver == null)
            receiver = GetComponent<GraspForceReceiver>();

        if (targetObject == null)
            targetObject = FindObjectOfType<LiftableObject>();

        if (targetObject != null)
            initialObjectY = targetObject.transform.position.y;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetSimulation();
            return;
        }
#else
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetSimulation();
            return;
        }
#endif

        if (receiver == null || targetObject == null) return;

        float force = receiver.currentAnimatedForce;

        // ── Phase 1: Approach ────────────────────────────────────────────────
        // When the user generates enough force, the hand reaches forward.
        isApproaching = force >= minApproachForce;

        // ── Phase 2: Grip detection ──────────────────────────────────────────
        // Measure distance from the palm centre (not the controller root) to the object.
        Vector3 palmPos = transform.position + Vector3.up * palmHeight;
        float distanceToPalm = Vector3.Distance(palmPos, targetObject.transform.position);

        float currentCapacity = force * maxLiftingCapacity;
        bool hasSufficientForce = currentCapacity >= targetObject.weightKg && force >= minApproachForce;
        bool inRange = distanceToPalm <= gripRange;

        // Only allow fingers to curl once the hand has reached the object
        receiver.allowFingerCurl = inRange || isHolding;

        if (inRange && hasSufficientForce && !isHolding)
        {
            // Accumulate hold time — grip only engages after sustained force.
            gripHoldTimer += Time.deltaTime;

            if (gripHoldTimer >= gripHoldTime)
            {
                AttachObject();
            }
        }
        else if (!hasSufficientForce || !inRange)
        {
            gripHoldTimer = 0f;

            if (isHolding)
                ReleaseObject(distanceToPalm <= gripRange && force >= minApproachForce);
        }

        // ── Phase 3: Move hand ───────────────────────────────────────────────
        Vector3 targetPos = restPosition;

        if (isApproaching || isHolding)
            targetPos += transform.forward * approachOffset;  // reach forward

        if (isHolding)
            targetPos.y = liftHeight;  // lift upward

        float speed = isHolding ? liftSpeed : approachSpeed;
        Vector3 newPos = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * speed);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && rb.isKinematic)
            rb.MovePosition(newPos);
        else
            transform.position = newPos;

        // ── Safety reset ─────────────────────────────────────────────────────
        if (!isHolding && targetObject.transform.position.y < initialObjectY - 3f)
        {
            targetObject.ResetPosition();
            Debug.Log("[HandLifter] Object reset to start position.");
        }
    }

    /// <summary>
    /// Resets the entire simulation: hand returns to rest, object resets,
    /// grip state clears, and force values zero out.
    /// </summary>
    public void ResetSimulation()
    {
        // Break any active grip
        if (isHolding)
        {
            FixedJoint joint = targetObject.gameObject.GetComponent<FixedJoint>();
            if (joint != null) Destroy(joint);
        }

        // Reset hand state
        isApproaching = false;
        isHolding = false;
        gripHoldTimer = 0f;

        // Reset hand position and orientation
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && rb.isKinematic)
            rb.MovePosition(restPosition);
        else
            transform.position = restPosition;
        transform.rotation = restRotation;

        // Reset the object
        if (targetObject != null)
            targetObject.ResetPosition();

        // Reset force receiver
        if (receiver != null)
        {
            receiver.currentAnimatedForce = 0f;
            receiver.currentActualForce = 0f;
        }

        Debug.Log("[HandLifter] Simulation reset to initial state.");
    }

    private void AttachObject()
    {
        isHolding = true;

        FixedJoint joint = targetObject.gameObject.GetComponent<FixedJoint>();
        if (joint == null)
        {
            joint = targetObject.gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = GetComponent<Rigidbody>();
            joint.breakForce = Mathf.Infinity;
            joint.breakTorque = Mathf.Infinity;
        }

        Rigidbody rb = targetObject.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }

        Debug.Log($"[HandLifter] Gripped! Capacity {receiver.currentAnimatedForce * maxLiftingCapacity:F2} kg " +
                  $">= Object {targetObject.weightKg:F2} kg. Lifting.");
    }

    private void ReleaseObject(bool lostForce)
    {
        isHolding = false;
        gripHoldTimer = 0f;

        FixedJoint joint = targetObject.gameObject.GetComponent<FixedJoint>();
        if (joint != null) Destroy(joint);

        Rigidbody rb = targetObject.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }

        Debug.Log(lostForce
            ? "[HandLifter] Grip lost — insufficient force. Object dropped."
            : "[HandLifter] Object released.");
    }

    private void OnDrawGizmosSelected()
    {
        // Yellow sphere shows the palm grip-detection zone in the editor.
        Vector3 palmPos = transform.position + Vector3.up * palmHeight;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(palmPos, gripRange);

        // Cyan line shows the approach direction.
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * approachOffset);
    }
}
