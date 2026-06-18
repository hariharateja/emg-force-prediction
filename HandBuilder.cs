#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → EMG Hand Simulation → Build Virtual Hand
///
/// BOTTLE GRIP — full sequence: Approach → Grab → Lift.
///
///   Cylinder rests on the floor at the origin.
///   Hand starts BEHIND the cylinder (-Z), at the same height.
///   When force arrives via UDP:
///     1. Hand approaches along +Z (forward) toward the cylinder.
///     2. Fingers curl sideways (+X) around the lateral surface.
///     3. Thumb opposes from +X, curling toward -X.
///     4. When force × capacity ≥ object weight → FixedJoint → lift +Y.
///     5. Force drops → release → hand retracts.
///
///   Curl axis (0,1,0):  +Z → +X  (verified: R_y(90°)·ẑ = x̂ ).
///   Thumb Euler(0,0,180) flips the axis so it curls +Z → -X (opposing).
/// </summary>
public class HandBuilder : EditorWindow
{
    [MenuItem("Tools/EMG Hand Simulation/Build Virtual Hand")]
    public static void BuildHand()
    {
        // ── Tear down previous build ─────────────────────────────────────────
        foreach (string n in new[]
        {
            "Hand_Controller", "Simulation_Floor", "Weight_Object", "Table",
            "palm", "index-finger"
        })
        {
            GameObject old = GameObject.Find(n);
            if (old != null) Undo.DestroyObjectImmediate(old);
        }

        // ── Hand_Controller — behind the cylinder, at cylinder-centre height ─
        //    Cylinder centre will be at (0, 0.25, 0).
        //    Controller rests at Z = -0.55 so the hand is visibly behind.
        GameObject ctrl = MakeEmpty("Hand_Controller");
        ctrl.transform.SetPositionAndRotation(
            new Vector3(0f, 0.25f, -0.55f), Quaternion.identity);
        Rigidbody ctrlRb = Undo.AddComponent<Rigidbody>(ctrl);
        ctrlRb.isKinematic = true;

        // ── All positions below are LOCAL to the controller ──────────────────
        //    Cylinder radius = 0.125 m.  Thumb and fingers must straddle it in X
        //    so the cylinder fits between them when the hand approaches.
        const float palmCX = 0f;
        const float palmCY = 0f;       // controller at cylinder-centre height
        const float palmCZ = -0.02f;   // palm slightly behind controller centre

        // ── Palm — wide enough to span from thumb side to finger side ────────
        GameObject palm = MakePrimitive(PrimitiveType.Cube, "Palm_Main", ctrl);
        palm.transform.localPosition = new Vector3(palmCX, palmCY, palmCZ);
        palm.transform.localScale    = new Vector3(0.30f, 0.14f, 0.04f);
        SetColor(palm, Skin(1.00f));

        // Wrist — behind the palm
        GameObject wristStub = MakePrimitive(PrimitiveType.Cube, "Wrist", ctrl);
        wristStub.transform.localPosition = new Vector3(palmCX, palmCY, palmCZ - 0.08f);
        wristStub.transform.localScale    = new Vector3(0.14f, 0.10f, 0.12f);
        SetColor(wristStub, Skin(0.92f));

        // ── Capsule phalanx extending in +Z ──────────────────────────────────
        void PhalanxForward(string pname, GameObject parent, float len, float diam, Color col)
        {
            GameObject cap = MakePrimitive(PrimitiveType.Capsule, pname, parent);
            cap.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cap.transform.localScale    = new Vector3(diam, len * 0.5f, diam);
            cap.transform.localPosition = new Vector3(0f, 0f, len * 0.5f);
            SetColor(cap, col);
        }

        void KnuckleSphere(string kname, GameObject parent, float diam)
        {
            GameObject k = MakePrimitive(PrimitiveType.Sphere, kname, parent);
            k.transform.localPosition = Vector3.zero;
            k.transform.localScale    = Vector3.one * diam;
            SetColor(k, Skin(1.02f));
        }

        // Finger pivot Z — just in front of the palm face
        const float fingerStartZ = palmCZ + 0.05f; // = 0.03

        var joints      = new List<Transform>();
        var multipliers = new List<float>();

        void AddFinger(string fname, float x, float y,
                       float pLen, float mLen, float dLen, float diam,
                       Quaternion baseRot = default)
        {
            if (baseRot == default) baseRot = Quaternion.identity;

            // MCP pivot
            GameObject pp = MakeEmpty(fname + "_ProxPivot");
            pp.transform.SetParent(ctrl.transform, false);
            pp.transform.localPosition = new Vector3(x, y, fingerStartZ);
            pp.transform.localRotation = baseRot;
            joints.Add(pp.transform);
            multipliers.Add(1.00f);
            KnuckleSphere(fname + "_K1", pp, diam * 1.15f);
            PhalanxForward(fname + "_Prox", pp, pLen, diam, Skin(1.00f));

            // PIP pivot
            GameObject mp = MakeEmpty(fname + "_MidPivot");
            mp.transform.SetParent(pp.transform, false);
            mp.transform.localPosition = new Vector3(0f, 0f, pLen);
            mp.transform.localRotation = Quaternion.identity;
            joints.Add(mp.transform);
            multipliers.Add(0.78f);
            KnuckleSphere(fname + "_K2", mp, diam * 1.05f);
            PhalanxForward(fname + "_Mid", mp, mLen, diam * 0.90f, Skin(0.93f));

            // DIP pivot
            GameObject dp = MakeEmpty(fname + "_DisPivot");
            dp.transform.SetParent(mp.transform, false);
            dp.transform.localPosition = new Vector3(0f, 0f, mLen);
            dp.transform.localRotation = Quaternion.identity;
            joints.Add(dp.transform);
            multipliers.Add(0.52f);
            KnuckleSphere(fname + "_K3", dp, diam * 0.95f);
            PhalanxForward(fname + "_Dis", dp, dLen, diam * 0.80f, Skin(0.85f));
        }

        // ── Thumb — far side (-X), outside the cylinder radius (0.125) ─────
        //    Curls toward +X (wraps around the -X side of the cylinder)
        AddFinger("Thumb", -0.15f, palmCY + 0.02f,
                  0.10f, 0.08f, 0.06f, 0.05f,
                  Quaternion.identity);

        // ── Four fingers — near side (+X), outside the cylinder radius ───────
        //    Curl toward -X (wrap around the +X side of the cylinder)
        //    Spread vertically along the cylinder height
        AddFinger("Index",  0.15f, palmCY + 0.04f,
                  0.14f, 0.10f, 0.07f, 0.04f,
                  Quaternion.Euler(0f, 0f, 180f));
        AddFinger("Middle", 0.15f, palmCY + 0.01f,
                  0.15f, 0.11f, 0.08f, 0.042f,
                  Quaternion.Euler(0f, 0f, 180f));
        AddFinger("Ring",   0.15f, palmCY - 0.02f,
                  0.13f, 0.10f, 0.07f, 0.038f,
                  Quaternion.Euler(0f, 0f, 180f));
        AddFinger("Pinky",  0.15f, palmCY - 0.05f,
                  0.10f, 0.08f, 0.05f, 0.032f,
                  Quaternion.Euler(0f, 0f, 180f));

        // ── Floor (thick slab, top surface at Y = 0) ─────────────────────────
        GameObject floor = MakePrimitive(PrimitiveType.Cube, "Simulation_Floor", null);
        floor.transform.SetPositionAndRotation(
            new Vector3(0f, -0.25f, 0f), Quaternion.identity);
        floor.transform.localScale = new Vector3(4f, 0.5f, 4f);
        SetColor(floor, new Color(0.52f, 0.52f, 0.52f));

        // ── Cylinder on the floor ────────────────────────────────────────────
        // Scale (0.25, 0.25, 0.25) → height 0.50 m, diameter 0.25 m.
        // Bottom at Y = 0, centre at Y = 0.25.
        GameObject weight = MakePrimitive(PrimitiveType.Cylinder, "Weight_Object", null);
        weight.transform.SetPositionAndRotation(
            new Vector3(0f, 0.25f, 0f), Quaternion.identity);
        weight.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        SetColor(weight, new Color(0.18f, 0.46f, 0.85f));

        Rigidbody wRb = Undo.AddComponent<Rigidbody>(weight);
        wRb.mass = 1.0f;
        wRb.isKinematic = true;   // stays put until HandLifter grips it
        wRb.useGravity = false;   // no gravity until gripped
        LiftableObject liftable = Undo.AddComponent<LiftableObject>(weight);
        liftable.weightKg = 1.0f;

        // ── GraspForceReceiver ────────────────────────────────────────────────
        GraspForceReceiver recv = Undo.AddComponent<GraspForceReceiver>(ctrl);
        recv.port            = 5005;
        recv.lerpSpeed       = 12f;
        recv.rotationAxis    = new Vector3(0f, 1f, 0f);  // +Z → +X
        recv.openAngle       = 0f;
        recv.closedAngle     = 80f;
        recv.fingerJoints    = joints.ToArray();
        recv.curlMultipliers = multipliers.ToArray();

        // ── HandLifter — approach → grip → lift ───────────────────────────────
        HandLifter lifter = Undo.AddComponent<HandLifter>(ctrl);
        lifter.receiver           = recv;
        lifter.targetObject       = liftable;
        lifter.minApproachForce   = 0.10f;
        lifter.approachOffset     = 0.50f;   // slide +Z until palm touches cylinder
        lifter.approachSpeed      = 3.0f;
        lifter.maxLiftingCapacity = 5.0f;
        lifter.gripRange          = 0.15f;  // tight — only curl when palm is near the surface
        lifter.gripHoldTime       = 0.40f;
        lifter.palmHeight         = 0f;      // palm at controller height = cylinder centre
        lifter.liftSpeed          = 2.0f;
        lifter.liftHeight         = 1.0f;

        // ── Camera — close-up on hand-object interaction ─────────────────────
        // Disable every camera except the one we configure so stale sample-scene
        // cameras don't fight for the Game view.
        Camera chosen = null;
        foreach (Camera cam in Object.FindObjectsOfType<Camera>())
        {
            if (chosen == null)
            {
                chosen = cam;
            }
            else
            {
                cam.enabled = false;
                AudioListener al = cam.GetComponent<AudioListener>();
                if (al != null) al.enabled = false;
            }
        }

        if (chosen != null)
        {
            Undo.RecordObject(chosen.transform, "Reposition Camera");
            Undo.RecordObject(chosen, "Adjust Camera");

            // Position: to the right and above the interaction zone,
            // looking straight at the space between hand and object.
            // Dark background instead of skybox
            chosen.clearFlags = CameraClearFlags.SolidColor;
            chosen.backgroundColor = new Color(0.15f, 0.15f, 0.18f);

            // Tight close-up, eye-level with the cylinder
            chosen.transform.position = new Vector3(0.25f, 0.30f, -0.20f);
            chosen.transform.LookAt(new Vector3(0f, 0.25f, -0.05f));

            chosen.nearClipPlane = 0.005f;
            chosen.fieldOfView   = 40f;

            EditorUtility.SetDirty(chosen);
            Debug.Log("[HandBuilder] Camera repositioned for close-up hand-object view.");
        }

        EditorUtility.SetDirty(recv);
        EditorUtility.SetDirty(lifter);
        if (ctrl.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ctrl.scene);

        Selection.activeGameObject = ctrl;
        Debug.Log("[HandBuilder] Bottle grip — approach → grab lateral surface → lift. Hit Play.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static Color Skin(float b) => new Color(0.87f * b, 0.70f * b, 0.55f * b);

    static GameObject MakeEmpty(string name)
    {
        GameObject old = GameObject.Find(name);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var obj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        return obj;
    }

    static GameObject MakePrimitive(PrimitiveType type, string name, GameObject parent)
    {
        GameObject old = GameObject.Find(name);
        if (old != null) Undo.DestroyObjectImmediate(old);
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        if (parent != null)
            obj.transform.SetParent(parent.transform, false);
        return obj;
    }

    static void SetColor(GameObject obj, Color color)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;
        r.material = new Material(r.sharedMaterial) { color = color };
    }
}
#endif
