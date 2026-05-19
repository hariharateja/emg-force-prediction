import numpy as np
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import train_test_split
from scipy import signal
import joblib

# --- 1. Load your data ---
X = np.load('X_train.npy')
y = np.load('y_train.npy')

# Indices are Channel-1: [7, 4, 17, 6]
top_4_indices = [7, 4, 17, 6]
X_4ch = X[:, top_4_indices]

# --- 2. FIX: Constrained Time Lag Alignment ---
def align_data(X_data, y_data):
    # Cross-correlate normalized signals
    correlation = signal.correlate(y_data - np.mean(y_data), 
                                   X_data[:, 0] - np.mean(X_data[:, 0]), mode='full')
    lags = signal.correlation_lags(len(y_data), len(X_data), mode='full')
    
    # CONSTRAINT: Only look for a lag within +/- 5 windows (500ms)
    # Since your data is 100ms per step, 5 windows = 500ms
    max_lag_windows = 5 
    mask = (lags >= 0) & (lags <= max_lag_windows)
    
    filtered_lags = lags[mask]
    filtered_correlation = correlation[mask]
    
    # Find the peak within the realistic range
    best_lag = filtered_lags[np.argmax(filtered_correlation)]
    
    print(f"--- Alignment Report ---")
    print(f"Detected Sync Lag: {best_lag} windows")
    print(f"Estimated Time:    {best_lag * 100} ms")
    print(f"------------------------")

    # Shift the signals
    if best_lag > 0:
        X_aligned = X_data[:-best_lag]
        y_aligned = y_data[best_lag:]
    elif best_lag < 0:
        X_aligned = X_data[-best_lag:]
        y_aligned = y_data[:best_lag]
    else:
        X_aligned, y_aligned = X_data, y_data
        
    return X_aligned, y_aligned

# Apply the correction
X_final, y_final = align_data(X_4ch, y)

# --- 3. Split the ALIGNED 4-channel data ---
X_train, X_test, y_train, y_test = train_test_split(X_final, y_final, test_size=0.2, random_state=42)

# --- 4. Train the "Specialist" Model ---
print("Training the Synchronized 4-Channel Specialist Model...")
model_4ch = RandomForestRegressor(n_estimators=100, max_depth=15, n_jobs=-1, random_state=42)
model_4ch.fit(X_train, y_train)

# --- 5. Save and Evaluate ---
joblib.dump(model_4ch, 'final_4ch_model.pkl')
score = model_4ch.score(X_test, y_test)
print(f"Final Synchronized R2 Score: {score:.4f}")