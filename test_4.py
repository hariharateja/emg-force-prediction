import numpy as np
import joblib
from sklearn.metrics import mean_squared_error, mean_absolute_error, r2_score
from sklearn.model_selection import train_test_split
from scipy import signal
import matplotlib.pyplot as plt

# 1. Updated Alignment Function with Biologically Realistic Constraints
def align_data(X_data, y_data):
    """Finds the EMD lag and aligns signals using 0-500ms constraint."""
    # Cross-correlate force and the strongest EMG channel
    correlation = signal.correlate(y_data - np.mean(y_data), 
                                   X_data[:, 0] - np.mean(X_data[:, 0]), mode='full')
    lags = signal.correlation_lags(len(y_data), len(X_data), mode='full')
    
    # CONSTRAINT: Only look for POSITIVE lags between 0 and 5 windows (0-500ms)
    # This matches the logic used in your train_4.py
    mask = (lags >= 0) & (lags <= 5)
    filtered_lags = lags[mask]
    filtered_correlation = correlation[mask]
    
    best_lag = filtered_lags[np.argmax(filtered_correlation)]
    
    print(f"--- Testing Sync Report ---")
    print(f"Detected Sync Lag: {best_lag} windows")
    print(f"Estimated Time:    {best_lag * 100} ms")
    print(f"---------------------------")

    if best_lag > 0:
        X_aligned = X_data[:-best_lag]
        y_aligned = y_data[best_lag:]
    else:
        X_aligned, y_aligned = X_data, y_data
        
    return X_aligned, y_aligned

# 2. Load the 4-channel model and the raw processed data
# Ensure 'final_4ch_model.pkl' was saved using the updated train_4.py
model = joblib.load('final_4ch_model.pkl')
X = np.load('X_train.npy')
y = np.load('y_train.npy')

# 3. Select the 4 channels and ALIGN them before the split
top_4_indices = [7, 4, 17, 6]
X_4ch = X[:, top_4_indices]

# Apply the same alignment used during training
X_final, y_final = align_data(X_4ch, y)

# 4. Create the Test Split on synchronized data
# We use the same random_state (42) to isolate the exact same test samples
_, X_test, _, y_test = train_test_split(X_final, y_final, test_size=0.2, random_state=42)

# 5. Predict
y_pred = model.predict(X_test)

# 6. Calculate Metrics
mse = mean_squared_error(y_test, y_pred)
mae = mean_absolute_error(y_test, y_pred)
r2 = r2_score(y_test, y_pred)

print("\n" + "="*40)
print("FINAL SYNCHRONIZED PERFORMANCE METRICS")
print(f"Mean Squared Error (MSE): {mse:.6f}")
print(f"Mean Absolute Error (MAE): {mae:.6f}")
print(f"R-Squared (R2) Score:     {r2:.4f}")
print("="*40)

# 7. Residual Analysis Plot
residuals = y_test - y_pred
plt.figure(figsize=(10, 6))
plt.scatter(y_pred, residuals, alpha=0.1, color='purple')
plt.axhline(y=0, color='black', linestyle='--')
plt.title('Residual Plot: Predicted Force vs. Error (Synced Model)', fontsize=14)
plt.xlabel('Predicted Force (Normalized)', fontsize=12)
plt.ylabel('Error (Actual - Predicted)', fontsize=12)
plt.grid(True, alpha=0.2)

plt.savefig('residual_analysis.png')
print("Residual plot saved as residual_analysis.png")
plt.show()