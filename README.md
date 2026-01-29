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
