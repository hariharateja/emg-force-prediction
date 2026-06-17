using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class GraspForceReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    public int port = 5005;

    [Header("Animation Settings")]
    [Tooltip("Smoothing speed. Higher = snappier, lower = smoother.")]
    public float lerpSpeed = 12f;

    [Tooltip("Local axis to rotate finger joints around (usually X).")]
    public Vector3 rotationAxis = new Vector3(1, 0, 0);

    [Tooltip("Angle at 0% force (hand fully open).")]
    public float openAngle = 0f;

    [Tooltip("Angle at 100% force (hand fully closed).")]
    public float closedAngle = 65f;

    [Header("Finger Joint Transforms")]
    [Tooltip("Assign base and mid joints here. Auto-sized with curlMultipliers below.")]
    public Transform[] fingerJoints;

    [Tooltip("Per-joint curl fraction (0-1). Base joints ~1.0, mid joints ~0.65. " +
             "Array auto-resizes to match fingerJoints.")]
    public float[] curlMultipliers;

    [Header("Real-time Status (Read Only)")]
    [Range(0f, 1f)] public float currentAnimatedForce = 0f;
    [Range(0f, 1f)] public float currentActualForce = 0f;

    // Internal state
    private Quaternion[] initialRotations;
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;
    private readonly object lockObject = new object();
    private float targetPredictedForce = 0f;
    private float targetActualForce = 0f;

    [Serializable]
    private class ForcePayload
    {
        public float predicted_force;
        public float actual_force;
        public int step;
    }

    private void Start()
    {
        // Capture each joint's initial local rotation.
        // Curl is applied ON TOP of this so pre-rotated joints (e.g. thumb) stay correct.
        if (fingerJoints != null)
        {
            initialRotations = new Quaternion[fingerJoints.Length];
            for (int i = 0; i < fingerJoints.Length; i++)
                initialRotations[i] = fingerJoints[i] != null
                    ? fingerJoints[i].localRotation
                    : Quaternion.identity;
        }

        SyncMultiplierArray();

        isRunning = true;
        receiveThread = new Thread(ReceiveData) { IsBackground = true };
        receiveThread.Start();
        Debug.Log($"[GraspForceReceiver] Listening on UDP port {port}.");
    }

    private void Update()
    {
        float targetForce;
        lock (lockObject)
        {
            targetForce = targetPredictedForce;
            currentActualForce = targetActualForce;
        }

        currentAnimatedForce = Mathf.Lerp(currentAnimatedForce, targetForce, Time.deltaTime * lerpSpeed);

        if (fingerJoints == null) return;

        float baseAngle = Mathf.Lerp(openAngle, closedAngle, currentAnimatedForce);

        for (int i = 0; i < fingerJoints.Length; i++)
        {
            if (fingerJoints[i] == null) continue;

            float multiplier = (curlMultipliers != null && i < curlMultipliers.Length)
                ? curlMultipliers[i] : 1f;

            float angle = baseAngle * multiplier;

            // Multiply the initial rotation by the curl delta so pre-rotated joints
            // (thumb) maintain their resting orientation while still curling correctly.
            fingerJoints[i].localRotation =
                initialRotations[i] * Quaternion.AngleAxis(angle, rotationAxis);
        }
    }

    private void ReceiveData()
    {
        try
        {
            udpClient = new UdpClient(port);
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, port);

            while (isRunning)
            {
                byte[] data = udpClient.Receive(ref remote);
                string json = Encoding.UTF8.GetString(data);
                ForcePayload payload = JsonUtility.FromJson<ForcePayload>(json);

                if (payload != null)
                {
                    lock (lockObject)
                    {
                        targetPredictedForce = payload.predicted_force;
                        targetActualForce = payload.actual_force;
                    }
                }
            }
        }
        catch (SocketException se)
        {
            // Expected when udpClient.Close() is called from StopUdp().
            if (isRunning)
                Debug.LogWarning($"[GraspForceReceiver] Socket closed: {se.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GraspForceReceiver] Receive error: {e.Message}");
        }
    }

    private void OnDisable() => StopUdp();
    private void OnDestroy() => StopUdp();

    private void StopUdp()
    {
        isRunning = false;
        udpClient?.Close();  // Causes ReceiveData to unblock and exit naturally — no Abort needed.
        udpClient = null;
        receiveThread = null;
    }

    // Keep curlMultipliers in sync with fingerJoints length (Inspector + runtime).
    private void OnValidate() => SyncMultiplierArray();

    private void SyncMultiplierArray()
    {
        if (fingerJoints == null) return;

        int needed = fingerJoints.Length;
        if (curlMultipliers != null && curlMultipliers.Length == needed) return;

        float[] updated = new float[needed];
        for (int i = 0; i < needed; i++)
            updated[i] = (curlMultipliers != null && i < curlMultipliers.Length)
                ? curlMultipliers[i] : 1f;
        curlMultipliers = updated;
    }
}
