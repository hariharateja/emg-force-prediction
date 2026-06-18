# EMG-Based Grasp Force Prediction + Unity Virtual Hand

Real-time grasp force estimation from surface EMG signals, driving a virtual hand in Unity to pick up and lift objects.

---

## What This Project Does

Surface EMG signals from the forearm are processed, fed into a machine learning model, and the predicted force is streamed over UDP to Unity — where a procedurally-built virtual hand curls its fingers and physically lifts a weight object.

```
Forearm EMG (2 channels)
  → Signal Processing (bandpass + notch filter + MVC normalization)
  → RMS Feature Extraction (200ms windows @ 10Hz)
  → Random Forest Model → Predicted Grasp Force (0.0 – 1.0)
  → UDP (port 5005) → Unity
  → Virtual Hand Finger Animation + Object Lifting
```

---

## Project Journey

### Step 1 — Raw Data Processing (`process_all.py`)
- Dataset: putEMG (5 subjects, 24-channel sEMG + load cell force @ 5120 Hz)
- Applied 4th-order Butterworth bandpass filter (20–450 Hz)
- Applied 50 Hz IIR notch filter (powerline noise removal)
- Normalized each subject's signals using MVC (Maximum Voluntary Contraction) calibration
- Extracted RMS features over 200ms sliding windows with 100ms step → ~10 Hz feature rate
- Output: `X_train.npy` (16,353 × 24) and `y_train.npy` (16,353,)

### Step 2 — Channel Optimization (`pipeline_2ch.py`)
Reduced 24 channels down to 2, following this process:

1. **Linear Regression sweep** over all 276 possible channel pairs (fast 5-fold CV ranking)
2. **Random Forest re-test** on top 20 pairs (honest estimate with 3-fold CV)
3. **Time-lag sweep** — tested offsets 0–400ms to find optimal EMG → force delay
4. **Final model comparison** — Random Forest vs Gradient Boosting on best pair + lag

**Result:**
| Setting | Value |
|---|---|
| Best channels | **Ch5 + Ch14** (0-indexed: 4, 13) |
| Optimal time lag | **100ms** (1 window) |
| Best model | **RandomForest** |
| Saved as | `final_2ch_model.pkl` + `final_2ch_meta.pkl` |

### Step 3 — Real-Time Simulator (`esp_simulator.py`)
Simulates ESP32 hardware for testing the full pipeline on a PC:
- Loads `final_2ch_model.pkl` and metadata
- Replays preprocessed EMG features from `X_train.npy`
- Predicts force at 10 Hz, normalizes to 0.0–1.0 range
- Sends JSON packets over UDP to port 5005

### Step 4 — Unity Virtual Hand
Four C# scripts drive the virtual hand simulation:

| Script | Role |
|---|---|
| `GraspForceReceiver.cs` | Listens on UDP 5005, animates finger joints via Lerp |
| `HandBuilder.cs` | Editor tool — builds the full hand scene in one click |
| `HandLifter.cs` | Proximity-based gripping and lifting of objects |
| `LiftableObject.cs` | Marks an object as liftable, handles reset if it falls |

---

## Repository Structure

```
emg-force-prediction/
├── Data/                     # Raw putEMG HDF5 files (5 subjects, ~2GB)
├── model/                    # Python virtual environment
│
├── process_all.py            # Step 1: Preprocess raw HDF5 → X_train.npy, y_train.npy
├── pipeline_2ch.py           # Step 2: Find best 2-channel pair + lag, train final model
├── esp_simulator.py          # Step 3: Simulate ESP32, stream predictions over UDP
│
├── GraspForceReceiver.cs     # Unity: receive UDP force, animate fingers
├── HandBuilder.cs            # Unity: one-click hand scene builder (Editor tool)
├── HandLifter.cs             # Unity: grip and lift physics
├── LiftableObject.cs         # Unity: liftable object definition
│
├── final_2ch_model.pkl       # Trained 2-channel RandomForest model
├── final_2ch_meta.pkl        # Metadata: channels=[4,13], lag=100ms, R² score
├── X_train.npy               # Preprocessed EMG features (16353 x 24)
├── y_train.npy               # Force labels (16353,)
│
├── force_prediction_results.png   # Predicted vs actual force plot
├── residual_analysis.png          # Error distribution plot
└── LICENSE
```

---

## Running the Virtual Hand Demo

### Requirements
```bash
# Python dependencies (use the included venv)
model/bin/pip install numpy scikit-learn scipy h5py joblib
```

### Step 1 — Run the Python Simulator
```bash
cd emg-force-prediction
model/bin/python esp_simulator.py
```
You should see ASCII bars updating in real time showing predicted vs actual force.

### Step 2 — Unity Setup (one-time)

1. Open **Unity Hub** → New Project → **3D Core** → name it `EMG_Hand_Simulation`
2. Drag all four `.cs` files into Unity's `Assets/` folder
3. Wait for Unity to compile (a few seconds)
4. In the top menu: **Tools → EMG Hand Simulation → Build Virtual Hand**

This automatically creates:
- `Hand_Controller` — parent object with `GraspForceReceiver` + `HandLifter` attached
- `Palm` + 5 fingers (`Index`, `Middle`, `Ring`, `Pinky`, `Thumb`) each with Base/Mid/Tip joints
- `Weight_Object` (1 kg cylinder) resting on a `Simulation_Floor`
- All finger joints pre-assigned to the receiver's joint array

### Step 3 — Play

1. Make sure `esp_simulator.py` is running in terminal
2. Click **Play** in Unity
3. Watch the hand curl its fingers as force increases — when grip force is sufficient, the hand lifts the weight object

### Configuration (Inspector on Hand_Controller)

| Parameter | Default | Effect |
|---|---|---|
| `lerpSpeed` | 12 | Smoothing speed — higher = snappier fingers |
| `closedAngle` | 80° | Maximum finger curl angle |
| `maxLiftingCapacity` | 5 kg | Max liftable weight at 100% force |
| `gripRange` | 0.15 m | Palm-to-object distance that triggers finger curl + grip |
| `gripHoldTime` | 0.4 s | Force must sustain this long before grip locks |
| `liftSpeed` | 2.0 | Speed of hand moving upward |
| `liftHeight` | 1.0 m | How high the hand lifts |

---

## Unity Object Interaction Approach

The virtual hand uses a **kinematic hand with proximity-gated finger curl and FixedJoint attachment**. The interaction follows a four-phase sequence that mimics how a real hand reaches for, grasps, lifts, and releases an object.

### Is Physics Enabled?

**Partially.** The hand itself is a kinematic Rigidbody (its motion is scripted, not driven by the physics solver), but the object interaction uses real Unity physics:

- The `Weight_Object` has a **Rigidbody** (1 kg mass). It starts kinematic and switches to dynamic (gravity-enabled) once gripped.
- Grip attachment uses a **FixedJoint** — a real physics joint that locks the object to the hand's Rigidbody.
- On release, the joint is destroyed and the object **falls under real gravity** with actual velocity.
- The fingers **do not collide** with the object. Instead, grip is triggered by a proximity check (palm-to-object distance ≤ `gripRange`). This was a deliberate choice — collision-based grasping with small articulated colliders caused the fingers to push the object away before they could wrap around it, producing unrealistic results.

### Four-Phase Grasp Sequence

```
Phase 1: APPROACH        Phase 2: CURL           Phase 3: GRIP + LIFT     Phase 4: RELEASE
                                                                          
  Hand ──→ Object         Fingers close            Hand rises              Joint destroyed
  Fingers stay OPEN       around cylinder           with object             Object falls
  (force drives motion)   (palm reached object)     (FixedJoint)            under gravity
```

**Phase 1 — Approach (fingers open):**
When the EMG-predicted force exceeds `minApproachForce` (0.1), the hand extends forward along +Z toward the cylinder. During this phase, `allowFingerCurl` is `false` — the fingers remain straight and open, straddling the cylinder (thumb on one side, four fingers on the other). The thumb and fingers are positioned at X = ±0.15 m, outside the cylinder radius (0.125 m), so the cylinder fits between them.

**Phase 2 — Finger Curl (palm reaches object):**
Once the palm is within `gripRange` (0.15 m) of the object, `allowFingerCurl` flips to `true`. The fingers begin curling inward proportionally to the EMG force — thumb curls toward +X, four fingers curl toward -X, closing around the cylinder from both sides. This creates the visual wrap-around grip.

**Phase 3 — Grip Lock + Lift:**
If the EMG force is sufficient (`force × maxLiftingCapacity ≥ objectWeight`) and sustained for `gripHoldTime` (0.4 s), a `FixedJoint` is created between the object and the hand. The object's Rigidbody switches to non-kinematic with gravity enabled, and the hand rises to `liftHeight` (1.0 m), carrying the object.

**Phase 4 — Release:**
When force drops below the threshold, the `FixedJoint` is destroyed. The object falls under real gravity. The hand retracts to its rest position and fingers open.

### Hand Geometry

The hand is procedurally generated by `HandBuilder.cs` (Editor tool: **Tools → EMG Hand Simulation → Build Virtual Hand**):

| Part | Geometry | Details |
|---|---|---|
| Palm | Cube (0.30 × 0.14 × 0.04 m) | Wide enough to span both sides of the cylinder |
| Wrist | Cube (0.14 × 0.10 × 0.12 m) | Behind the palm |
| Thumb (1) | 3 capsule segments (MCP → PIP → DIP) | Far side (X = -0.15), curls toward +X |
| Index–Pinky (4) | 3 capsule segments each | Near side (X = +0.15), curl toward -X |
| Total joints | 15 (3 per finger × 5 fingers) | Each with independent curl multiplier |

Curl multipliers create realistic joint differentiation: base joints curl at 100%, mid joints at 78%, and fingertip joints at 52% of the target angle.

### Physics Summary

| Aspect | Physics? | Details |
|---|---|---|
| Hand movement | No | Kinematic Rigidbody, moved via `MovePosition()` |
| Finger animation | No | Scripted rotation driven by EMG force via UDP |
| Finger-object collision | No | Proximity-based (avoids push-away instability) |
| Object Rigidbody | Yes | 1 kg mass, switches to dynamic on grip |
| Grip joint | Yes | `FixedJoint` (infinite break force/torque) |
| Object gravity on release | Yes | Falls under real gravity with physics velocity |
| Floor collision | Yes | Object rests on thick collision slab |

### Why Not Full Collision-Based Grasping?

We tested collision-based interaction (finger colliders physically touching the object to trigger the grip). The result: because the hand is kinematic (its motion is scripted by EMG, not by the physics solver), the finger colliders **pushed the object sideways and toppled it** before the grip could engage. Full collision-based grasping requires articulated physics fingers with joint motors, mass tuning, and solver iteration configuration — significantly more complex and prone to instability (jitter, tunnelling, exploding joints) for a demonstration of this kind. The proximity-based approach produces a clean, deterministic grasp animation that faithfully represents the EMG → force → grasp → lift pipeline.

---

## Signal Processing Details

| Parameter | Value |
|---|---|
| Sampling rate | 5120 Hz |
| Bandpass filter | 20–450 Hz (4th-order Butterworth) |
| Notch filter | 50 Hz (powerline) |
| Normalization | Per-subject MVC (95th percentile of filtered MVC envelope) |
| Feature | RMS over 200ms window |
| Step size | 100ms (→ 10 Hz feature rate) |
| Time lag correction | 100ms (EMG leads force) |

---

## Dataset

putEMG — High-density surface EMG dataset for hand gesture recognition  
Biolab, Poznan University of Technology  
5 subjects, 24 channels, synchronized grip force @ 5120 Hz

---

## Hardware Target

For deployment on real hardware:
- **Sensors:** BioAmp EXG Pill (place on Ch5 and Ch14 positions = proximal forearm flexor region)
- **MCU:** ESP32 (runs inference + sends UDP over WiFi)
- **Force feedback:** Load cell + HX711 for calibration
