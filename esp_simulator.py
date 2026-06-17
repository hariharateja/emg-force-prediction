import numpy as np
import joblib
import socket
import json
import time
import sys

# --- CONFIGURATION ---
UDP_IP = "127.0.0.1"
UDP_PORT = 5005
STEP_INTERVAL_SEC = 0.100  # 100ms per window (10Hz)

def draw_bar(val, max_chars=15):
    """Generates a simple ASCII bar representing a value between 0.0 and 1.0"""
    val = max(0.0, min(1.0, val))
    filled = int(round(val * max_chars))
    return "[" + "█" * filled + "░" * (max_chars - filled) + "]"

def main():
    print("=" * 60)
    print("           EMG-TO-FORCE ESP SIMULATOR (UDP)")
    print("=" * 60)
    
    # 1. Load trained 2-channel model & metadata
    print("Loading model and metadata...")
    try:
        model = joblib.load('final_2ch_model.pkl')
        meta = joblib.load('final_2ch_meta.pkl')
        print("Model loaded successfully!")
        print(f"  Model Type: {meta.get('model_type', 'N/A')}")
        print(f"  Best Channels: {meta.get('channels', 'N/A')} ({', '.join(meta.get('channel_names', []))})")
        print(f"  Trained Time Lag: {meta.get('lag_ms', 0)} ms")
        print(f"  Model Test R² Score: {meta.get('r2_test', 0.0):.4f}")
    except Exception as e:
        print(f"Error loading model or metadata: {e}")
        print("Please ensure 'final_2ch_model.pkl' and 'final_2ch_meta.pkl' exist in the workspace.")
        sys.exit(1)
        
    # Extract channel indexes and lag
    best_channels = meta['channels']  # e.g., [4, 13] for Ch5 and Ch14
    lag_windows = meta.get('lag_windows', 1)
    
    # 2. Load EMG feature dataset
    print("\nLoading dataset (X_train.npy, y_train.npy)...")
    try:
        X = np.load('X_train.npy')  # shape: (N, 24)
        y = np.load('y_train.npy')  # shape: (N,)
        print(f"Loaded feature matrix: {X.shape}")
        print(f"Loaded force vector:   {y.shape}")
    except Exception as e:
        print(f"Error loading datasets: {e}")
        print("Please check X_train.npy and y_train.npy files.")
        sys.exit(1)
        
    # Align the dataset using the trained lag offset
    # X_final corresponds to EMG features at time t
    # y_final corresponds to actual force at time t + lag
    if lag_windows > 0:
        X_final = X[:-lag_windows, best_channels]
        y_final = y[lag_windows:]
    else:
        X_final = X[:, best_channels]
        y_final = y
        
    n_samples = len(X_final)
    print(f"Prepared {n_samples} steps of simulated EMG data.")
    
    # 3. Initialize UDP Socket
    print(f"\nInitializing UDP socket to send to {UDP_IP}:{UDP_PORT}...")
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    
    print("\nPress Ctrl+C to stop simulation at any time.")
    print("=" * 60)
    print(f"{'Step':<6} | {'Actual Force (GT)':<22} | {'Predicted Force':<22} | {'Diff':<6}")
    print("-" * 60)
    
    # Find the first index where actual force rises above baseline (3.0)
    spikes = np.where(y_final > 3.0)[0]
    start_step = int(max(0, spikes[0] - 20)) if len(spikes) > 0 else 0
    active_length = int(n_samples - start_step)
    print(f"First force activity detected at step {int(spikes[0]) if len(spikes) > 0 else 0}.")
    print(f"Starting simulation at step {start_step} to bypass the silent prefix ({start_step * STEP_INTERVAL_SEC:.1f} seconds).")

    try:
        step = 0
        while True:
            # Wrap around loop within the active part of the dataset
            idx = start_step + (step % active_length)
            
            # Get current step features and target force
            features = X_final[idx]      # shape: (2,)
            actual_force = float(y_final[idx])
            
            # Predict force
            pred_force = float(model.predict(features.reshape(1, -1))[0])
            
            # Normalize forces from [3.0, 7.0] range to [0.0, 1.0] range for Unity receiver
            norm_actual = (actual_force - 3.0) / 4.0
            norm_pred = (pred_force - 3.0) / 4.0
            
            # Clip normalized values between 0.0 and 1.0
            actual_force_norm = max(0.0, min(1.0, norm_actual))
            pred_force_norm = max(0.0, min(1.0, norm_pred))
            
            # Prepare payload
            payload = {
                "predicted_force": pred_force_norm,
                "actual_force": actual_force_norm,
                "step": idx
            }
            
            # Send over UDP
            message = json.dumps(payload).encode('utf-8')
            sock.sendto(message, (UDP_IP, UDP_PORT))
            
            # Visual ASCII bars
            actual_bar = draw_bar(actual_force_norm, 10)
            pred_bar = draw_bar(pred_force_norm, 10)
            diff = pred_force_norm - actual_force_norm
            
            # Print status update inline
            sys.stdout.write(
                f"\r{idx:<6} | {actual_bar} {actual_force_norm:.2f} | {pred_bar} {pred_force_norm:.2f} | {diff:+.2f}"
            )
            sys.stdout.flush()
            
            # Sleep to match real-time (100ms window size)
            time.sleep(STEP_INTERVAL_SEC)
            step += 1
            
    except KeyboardInterrupt:
        print("\n\nSimulation stopped by user.")
    finally:
        sock.close()
        print("UDP socket closed. Goodbye!")

if __name__ == "__main__":
    main()
