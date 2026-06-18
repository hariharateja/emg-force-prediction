using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to a liftable object. Tracks real collision contacts from finger
/// colliders and reports how many distinct fingers are currently touching.
/// </summary>
public class GraspContactDetector : MonoBehaviour
{
    [Tooltip("Minimum distinct fingers required to consider the object gripped.")]
    public int minFingersForGrip = 2;

    /// <summary>Number of distinct fingers currently in contact.</summary>
    public int UniqueFingerCount => activeFingers.Count;

    /// <summary>True when enough distinct fingers are touching.</summary>
    public bool IsGrasped => activeFingers.Count >= minFingersForGrip;

    // finger name -> set of collider instance IDs currently touching
    private Dictionary<string, HashSet<int>> fingerContacts = new Dictionary<string, HashSet<int>>();
    private HashSet<string> activeFingers = new HashSet<string>();

    private static readonly string[] FingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };

    private void OnCollisionEnter(Collision collision)
    {
        RegisterContact(collision.collider, add: true);
    }

    private void OnCollisionExit(Collision collision)
    {
        RegisterContact(collision.collider, add: false);
    }

    private void RegisterContact(Collider col, bool add)
    {
        string finger = IdentifyFinger(col.transform);
        if (finger == null) return;

        int id = col.GetInstanceID();

        if (!fingerContacts.ContainsKey(finger))
            fingerContacts[finger] = new HashSet<int>();

        if (add)
            fingerContacts[finger].Add(id);
        else
            fingerContacts[finger].Remove(id);

        // Rebuild active fingers set
        activeFingers.Clear();
        foreach (var kvp in fingerContacts)
            if (kvp.Value.Count > 0)
                activeFingers.Add(kvp.Key);
    }

    /// <summary>Walk up the transform hierarchy to find a known finger name.</summary>
    private string IdentifyFinger(Transform t)
    {
        while (t != null)
        {
            string name = t.gameObject.name;
            foreach (string f in FingerNames)
                if (name.StartsWith(f))
                    return f;
            t = t.parent;
        }
        return null;
    }

    public void ResetContacts()
    {
        fingerContacts.Clear();
        activeFingers.Clear();
    }
}
