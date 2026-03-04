import numpy as np
import joblib
from sklearn.metrics import mean_squared_error, mean_absolute_error, r2_score

# 1. Load the 4-channel model and the processed data
model = joblib.load('final_4ch_model.pkl')
X = np.load('X_train.npy')
y = np.load('y_train.npy')

# 2. Select the EXACT same 4 channels used in training (Channels 8, 5, 18, 7)
top_4_indices = [7, 4, 17, 6]
X_4ch = X[:, top_4_indices]

# 3. Create the Test Split (use the same random_state as before)
from sklearn.model_selection import train_test_split
_, X_test, _, y_test = train_test_split(X_4ch, y, test_size=0.2, random_state=42)

# 4. Predict
y_pred = model.predict(X_test)

# 5. Calculate Metrics
mse = mean_squared_error(y_test, y_pred)
mae = mean_absolute_error(y_test, y_pred)
r2 = r2_score(y_test, y_pred)

print("\n" + "="*40)
print("FINAL 4-CHANNEL PERFORMANCE METRICS")
print(f"Mean Squared Error (MSE): {mse:.6f}")
print(f"Mean Absolute Error (MAE): {mae:.6f}")
print(f"R-Squared (R2) Score:     {r2:.4f}")
print("="*40)

import matplotlib.pyplot as plt

# 1. Calculate Residuals
residuals = y_test - y_pred

# 2. Plot
plt.figure(figsize=(10, 6))
plt.scatter(y_pred, residuals, alpha=0.1, color='purple')
plt.axhline(y=0, color='black', linestyle='--')
plt.title('Residual Plot: Predicted Force vs. Error (4-Channel Model)', fontsize=14)
plt.xlabel('Predicted Force (Normalized)', fontsize=12)
plt.ylabel('Error (Actual - Predicted)', fontsize=12)
plt.grid(True, alpha=0.2)

plt.savefig('residual_analysis.png')
print("Residual plot saved as residual_analysis.png")
plt.show()