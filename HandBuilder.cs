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
        const float palmCX = -0.05f;   // slightly left so fingers wrap toward +X
        const float palmCY = 0f;       // controller already at cylinder-centre height
        const float palmCZ = -0.02f;   // palm slightly behind controller centre

        // ── Palm — YZ plane, face pointing +Z ───────────────────────────────
        GameObject palmWrist = MakePrimitive(PrimitiveType.Cube, "Palm_Wrist", ctrl);
        palmWrist.transform.localPosition = new Vector3(palmCX, palmCY - 0.10f, palmCZ);
        palmWrist.transform.localScale    = new Vector3(0.06f, 0.06f, 0.05f);
        SetColor(palmWrist, Skin(0.95f));

        GameObject palmMid = MakePrimitive(PrimitiveType.Cube, "Palm_Mid", ctrl);
        palmMid.transform.localPosition = new Vector3(palmCX, palmCY, palmCZ);
        palmMid.transform.localScale    = new Vector3(0.06f, 0.14f, 0.05f);
        SetColor(palmMid, Skin(1.00f));

        GameObject palmKnuckle = MakePrimitive(PrimitiveType.Cube, "Palm_Knuckle", ctrl);
        palmKnuckle.transform.localPosition = new Vector3(palmCX, palmCY + 0.06f, palmCZ);
        palmKnuckle.transform.localScale    = new Vector3(0.06f, 0.10f, 0.05f);
        SetColor(palmKnuckle, Skin(1.00f));

        // Wrist stub
        GameObject wristStub = MakePrimitive(PrimitiveType.Cylinder, "Wrist", ctrl);
        wristStub.transform.localPosition = new Vector3(palmCX, palmCY - 0.16f, palmCZ);
        wristStub.transform.localScale    = new Vector3(0.05f, 0.04f, 0.05f);
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

        // ── Thumb — opposing from +X side ────────────────────────────────────
        AddFinger("Thumb", palmCX + 0.12f, palmCY - 0.10f,
                  0.12f, 0.09f, 0.07f, 0.055f,
                  Quaternion.Euler(0f, 0f, 180f));

        // ── Four fingers — spread along Y (vertical) with gaps ───────────────
        AddFinger("Index",  palmCX, palmCY - 0.06f,  0.16f, 0.12f, 0.08f, 0.042f);
        AddFinger("Middle", palmCX, palmCY - 0.005f, 0.18f, 0.14f, 0.10f, 0.046f);
        AddFinger("Ring",   palmCX, palmCY + 0.05f,  0.16f, 0.12f, 0.08f, 0.040f);
        AddFinger("Pinky",  palmCX, palmCY + 0.10f,  0.12f, 0.09f, 0.06f, 0.034f);

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

        // ── Floor ─────────────────────────────────────────────────────────────
        GameObject floor = MakePrimitive(PrimitiveType.Cube, "Simulation_Floor", null);
        floor.transform.SetPositionAndRotation(
            new Vector3(0f, -0.05f, 0f), Quaternion.identity);
        floor.transform.localScale = new Vector3(4f, 0.1f, 4f);
        SetColor(floor, new Color(0.52f, 0.52f, 0.52f));

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
        lifter.approachOffset     = 0.45f;   // slide +Z toward cylinder
        lifter.approachSpeed      = 3.0f;
        lifter.maxLiftingCapacity = 5.0f;
        lifter.gripRange          = 0.30f;
        lifter.gripHoldTime       = 0.40f;
        lifter.palmHeight         = 0f;      // palm at controller height = cylinder centre
        lifter.liftSpeed          = 2.0f;
        lifter.liftHeight         = 1.0f;

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
