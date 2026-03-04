# emg-force-prediction
An experimental platform for synchronized EMG and grip force acquisition and continuous force estimation.
# EMG-Based Grasp Force Estimation

This project focuses on building a low-cost experimental system to estimate human grasp force using surface electromyography (EMG) signals. By collecting synchronized EMG and grip force data, the system studies how muscle activation translates into mechanical force, forming the basis for intuitive control in prosthetic hands and assistive robotic devices.

The project integrates biosignal acquisition, embedded systems, signal processing, and machine learning into a single research-oriented platform.

---

## Motivation

Modern prosthetic and assistive devices require control methods that reflect not only *which* action a user intends, but also *how strongly* they intend to perform it. However, many current EMG-based systems rely on simple thresholding rather than continuous force estimation.

This project explores the research question:

**Can surface EMG signals be used to predict human grasp force in real time?**

---

## System Overview

The system consists of:
- Surface EMG electrodes for measuring forearm muscle activity
- A load-cell-based grip device to measure actual grasp force
- A microcontroller (ESP32 preferred) for synchronized data acquisition
- A signal processing pipeline for EMG feature extraction
- A regression or machine learning model to estimate force from EMG

The core relationship studied is:

---

## Objectives

- Design and build a grip force measurement device using a load cell
- Acquire clean surface EMG signals from relevant forearm muscles
- Develop synchronized EMG and force data acquisition
- Perform EMG signal processing (filtering, rectification, envelope extraction)
- Model the EMG–force relationship using regression or machine learning
- Evaluate estimation accuracy, delay, and repeatability

---
# EMG-to-Grasp Force Estimation Research Platform

This repository contains a complete pipeline for estimating human hand grasp force from surface Electromyography (sEMG) signals. The project focuses on optimizing high-density sensor data (24 channels) into a lightweight, 4-channel model suitable for real-time control on embedded hardware like the ESP32.

## 📂 Project Structure

### 🧠 Machine Learning Models (.pkl)
These files store the trained Random Forest Regressor weights.
* **`final_4ch_model.pkl`**: The production-ready model. Optimized for 4-channel input with an $R^2$ score of **0.9640**.
* **`grasp_force_model.pkl`**: Baseline model trained on the full 24-channel dataset.
* **`grasp_force_model_4ch.pkl`**: Checkpoint model from the optimization phase.

### 🐍 Core Processing Scripts (.py)
* **`process_all.py`**: The main data pipeline. It handles HDF5 unpacking, 50Hz notch filtering, 20-450Hz bandpass filtering, and MVC normalization.
* **`train_model.py`**: Initial training script used to establish baseline performance and calculate **Feature Importance** rankings.
* **`optimisation_test.py`**: A diagnostic script used to evaluate how model accuracy drops as sensors are removed (24 -> 16 -> 8 -> 4 -> 2 -> 1).
* **`train_4.py` & `test_4.py`**: Specialist scripts for training and validating the final 4-sensor architecture.
* **`inspect_h5.py`**: Utility to visualize the internal structure and metadata of the raw putEMG HDF5 files.
* **`oraganise.py`**: Manages file paths and automated data sorting.

### 📊 Data & Artifacts
* **`X_train.npy` / `y_train.npy`**: Preprocessed feature matrices (RMS values) and target force vectors in NumPy format.
* **`force_prediction_results.png`**: Plot comparing Actual Force vs. Predicted Force on unseen test data.
* **`residual_analysis.png`**: Residual plot used to verify error distribution and model bias.
* **`sigprocess.m`**: MATLAB implementation for additional signal verification.

---

## 🛠 Preprocessing Pipeline
To ensure high accuracy, the following steps are applied to the raw EMG data:
1.  **Filtering**: 4th order Butterworth bandpass (20-450Hz) and a Notch filter at 50Hz to remove power line interference.
2.  **Normalization**: Signals are scaled based on the Maximum Voluntary Contraction (MVC) trial of each subject.
3.  **Feature Extraction**: Root Mean Square (RMS) calculated over a 200ms sliding window.

## 📈 Performance Summary (4-Channel Model)
The model was reduced from 24 sensors to 4 while retaining **98.3%** of the original performance.

| Metric | Result |
| :--- | :--- |
| **R-Squared ($R^2$)** | **0.9640** |
| **Mean Absolute Error (MAE)** | **0.0671** |
| **Mean Squared Error (MSE)** | **0.0712** |

## 📍 Recommended Sensor Placement
For real-time implementation using **BioAmp EXG Pills**, sensors should be placed at the following locations on the right forearm:
1.  **Channel 8 & 7**: Proximal forearm (near elbow), targeting the Extensor Digitorum.
2.  **Channel 5**: Proximal forearm (near elbow), targeting the Flexor Carpi Radialis.
3.  **Channel 18**: Distal forearm (near wrist), targeting tendon-related force shifts.

---
*Dataset Source: [putEMG Dataset](https://biolab.put.poznan.pl/putemg-dataset)*
## Hardware

### EMG Front-End
- **BioAmp EXG Pill** (recommended)
- MyoWare 2.0 EMG Sensor (alternative)

### Processing
- ESP32 / Arduino Nano / Arduino Uno
- USB serial communication for real-time data streaming

### Sensors and Consumables
- Ag/AgCl disposable surface EMG electrodes
- Load cell with amplifier (e.g., HX711)
- Electrode cables, straps, skin preparation materials

---

## Software and Tools

- **Embedded Programming:** Arduino / ESP32 firmware
- **Signal Processing:** MATLAB or GNU Octave
- **Optional Framework:** BrainFlow (biosignal streaming)
- **Version Control & Documentation:** GitHub

---

## Signal Processing Pipeline

- Sampling rate: 250–1000 Hz
- Bandpass filtering (≈20–450 Hz)
- Optional 50 Hz notch filtering
- Signal rectification
- Envelope extraction (low-pass filtering)
- Calibration using rest and Maximum Voluntary Contraction (MVC)
- Mapping normalized EMG features to grip force

---

## Methodology (Planned)
(tentative)
1. Hardware setup and sensor calibration
2. Synchronized EMG and force data acquisition
3. EMG preprocessing and feature extraction
4. Time-delay alignment between EMG and force
5. Regression / ML-based force estimation
6. Quantitative evaluation and analysis

---

## Timeline (Tentative – To Be Refined)

- **Month 1:** Understanding EMG fundamentals, hardware setup, raw data acquisition
- **Month 2:** Signal processing, calibration, baseline force estimation
- **Month 3:** Model refinement, evaluation, report, and demonstration

---

## Expected Outcomes

- A working low-cost EMG-based grasp force sensing platform
- A dataset of synchronized EMG and grip force signals
- A model capable of predicting grasp force trends from EMG
- Quantitative evaluation of system performance and limitations
- Demonstration videos and technical report

---

## References

- De Luca, C. J., *The Use of Surface Electromyography in Biomechanics*
- PhysioNet EMG Signal Database
- MATLAB EMG Processing Examples
- BioAmp EXG Pill Documentation
- BrainFlow Open-Source Biosignal SDK

---

## Status

🚧 **Project in progress**
This repository will be updated progressively as the system is developed and evaluated.
