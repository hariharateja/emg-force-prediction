import h5py
import numpy as np
import joblib
from scipy.signal import butter, filtfilt, iirnotch
from sklearn.ensemble import RandomForestRegressor
from sklearn.metrics import r2_score

# --- Configuration using your specific filenames ---
TRAIN_FILE = 'Data/emg_force-03-repeats_long-2018-06-14-12-51-19-410.hdf5'
TEST_FILE = 'Data/emg_force-04-repeats_long-2018-06-18-15-22-01-896.hdf5'

# Our optimized Top 4 Channels: 8, 5, 18, 7 (Zero-indexed: 7, 4, 17, 6)
TOP_4_INDICES = [7, 4, 17, 6] 

def process_raw_h5(file_path):
    print(f"Reading raw data from: {file_path}")
    
    with h5py.File(file_path, 'r') as f:
        # Access the root group (usually 'data' or the first key)
        # In putEMG HDF5s, pandas stores the table under 'df' or the root
        key = list(f.keys())[0]
        group = f[key]
        
        # Pull raw values from the HDF5 datasets
        # columns are usually: [Timestamp, EMG_1...EMG_24, FORCE_1...FORCE_10, etc.]
        # We need to find the indices of the columns we want.
        data = group['table'][:]
        
        # Based on putEMG structure: 
        # EMG_1 to EMG_24 are usually columns 1 to 24
        # FORCE_1 is usually column 25
        emg_raw = data['column_values'][:, 1:25] 
        force_raw = data['column_values'][:, 25] 

    # --- DSP Filtering ---
    fs = 5120
    b_n, a_n = iirnotch(50, 30, fs)
    b_b, a_b = butter(4, [20, 450], btype='band', fs=fs)
    
    filtered = filtfilt(b_n, a_n, emg_raw, axis=0)
    filtered = filtfilt(b_b, a_b, filtered, axis=0)
    
    # --- RMS Windowing (200ms windows / 100ms step) ---
    win, step = 1024, 512
    num_wins = (len(filtered) - win) // step
    
    X, y = [], []
    for i in range(num_wins):
        s, e = i * step, i * step + win
        # Feature: RMS per channel
        X.append(np.sqrt(np.mean(filtered[s:e]**2, axis=0)))
        # Target: Mean force for the window
        y.append(np.mean(force_raw[s:e]))
        
    return np.array(X), np.array(y)

# --- Execution ---
print("Running Cross-Subject Validation...")
X_train_all, y_train = process_raw_h5(TRAIN_FILE)
X_test_all, y_test = process_raw_h5(TEST_FILE)

# Select Top 4 Features
X_train = X_train_all[:, TOP_4_INDICES]
X_test = X_test_all[:, TOP_4_INDICES]

# Train Specialist Model
model = RandomForestRegressor(n_estimators=100, max_depth=15, n_jobs=-1, random_state=42)
model.fit(X_train, y_train)

# Test on new subject
y_pred = model.predict(X_test)
r2 = r2_score(y_test, y_pred)

print("\n" + "="*45)
print(f"RESULTS: Sub 03 (Train) -> Sub 04 (Test)")
print(f"4-Channel R2 Score: {r2:.4f}")
print("="*45)