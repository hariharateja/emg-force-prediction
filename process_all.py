import numpy as np
import h5py
from scipy.signal import butter, filtfilt, iirnotch
from scipy.interpolate import interp1d
import glob
import os

# --- CONFIGURATION ---
FS = 5120
WINDOW_SIZE = 1024
STEP_SIZE = 512
SUBJECTS = ['03', '04', '05', '06', '07']

def apply_filters(data):
    data = np.array(data, dtype=float)
    if data.ndim == 1: data = data.reshape(-1, 1)
    b_band, a_band = butter(4, [20, 450], btype='band', fs=FS)
    b_notch, a_notch = iirnotch(50, 30, fs=FS)
    clean = np.zeros_like(data)
    for col in range(data.shape[1]):
        sig = np.ascontiguousarray(data[:, col]).astype(float)
        sig = filtfilt(b_band, a_band, sig)
        sig = filtfilt(b_notch, a_notch, sig)
        clean[:, col] = sig
    return clean

def get_features(emg_data, target_force):
    X, y = [], []
    if len(target_force) != len(emg_data):
        x_old = np.linspace(0, 1, len(target_force))
        x_new = np.linspace(0, 1, len(emg_data))
        f_interp = interp1d(x_old, target_force, kind='linear', fill_value="extrapolate")
        target_force = f_interp(x_new)

    for i in range(0, len(emg_data) - WINDOW_SIZE, STEP_SIZE):
        window = emg_data[i : i + WINDOW_SIZE]
        rms = np.sqrt(np.mean(window**2, axis=0))
        X.append(rms)
        y.append(target_force[i + WINDOW_SIZE])
    return np.array(X), np.array(y)

def load_by_position(path):
    """Bypasses names entirely and loads by column index."""
    with h5py.File(path, 'r') as f:
        dset = f['data']['table']
        data_raw = dset[()] 
        
        # Access all fields in the structured array
        names = data_raw.dtype.names
        
        # Convert the whole thing to a standard 2D float matrix
        # This handles whatever names the creator used
        full_matrix = np.column_stack([data_raw[name].astype(np.float64) for name in names])
        
        # In putEMG:
        # Columns 0-23 are usually the 24 EMG channels
        # Column 24 (or the last one) is usually TRAJ_GT
        emg_matrix = full_matrix[:, :24]
        traj_array = full_matrix[:, -1] # Takes the very last column
        
        return emg_matrix, traj_array

# --- MASTER EXECUTION ---
all_X, all_y = [], []
print("--- Starting POSITION-BASED Preprocessing ---")

for sub in SUBJECTS:
    print(f"Processing Subject {sub}...", end=" ")
    try:
        mvc_files = sorted(glob.glob(f"data/emg_force-{sub}-mvc-*.hdf5"))
        seq_files = sorted(glob.glob(f"data/emg_force-{sub}-sequential-*.hdf5"))
        
        if not mvc_files or not seq_files:
            print("FAILED (Files missing)")
            continue

        emg_mvc, _ = load_by_position(mvc_files[-1])
        emg_seq, force_seq = load_by_position(seq_files[-1])
        
        # 1. Calibration
        filtered_mvc = apply_filters(emg_mvc)
        mvc_scale = np.percentile(np.abs(filtered_mvc), 95, axis=0).flatten()
        
        # 2. Process and Normalize
        emg_clean = apply_filters(emg_seq)
        emg_norm = emg_clean / (mvc_scale + 1e-9)
        
        # 3. Align and Window
        X_sub, y_sub = get_features(emg_norm, force_seq)
        all_X.append(X_sub)
        all_y.append(y_sub)
        print("SUCCESS")
        
    except Exception as e:
        print(f"ERROR: {e}")

if all_X:
    np.save('X_train.npy', np.vstack(all_X))
    np.save('y_train.npy', np.concatenate(all_y))
    print("\n" + "="*40)
    print("SUCCESS! Saved X_train.npy and y_train.npy")
    print("="*40)
    print(all_X[0].shape)
else:
    print("\nCRITICAL ERROR: No data was processed. Check your 'data/' folder.")