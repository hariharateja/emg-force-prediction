import numpy as np
import matplotlib.pyplot as plt
import joblib
from sklearn.model_selection import train_test_split

# 1. Load data and model
X = np.load('X_train.npy')
y = np.load('y_train.npy')
model = joblib.load('grasp_force_model.pkl')

# 2. Get the same test split we used in training
_, X_test, _, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

# 3. Predict on a subset of the test data (first 500 samples for clarity)
n_samples = 500
y_pred = model.predict(X_test[:n_samples])
y_actual = y_test[:n_samples]

# 4. Plot
plt.figure(figsize=(14, 6))
plt.plot(y_actual, label='Actual Force (Load Cell)', color='red', linewidth=2, alpha=0.7)
plt.plot(y_pred, label='Predicted Force (Random Forest)', color='blue', linestyle='--', linewidth=2)

plt.title('EMG-to-Grasp Force Estimation Performance', fontsize=16)
plt.xlabel('Time Windows (Samples)', fontsize=12)
plt.ylabel('Normalized Force (0.0 to 1.0)', fontsize=12)
plt.legend()
plt.grid(True, alpha=0.3)

# Save the plot for your report
plt.savefig('force_prediction_results.png')
print("Plot saved as force_prediction_results.png")
plt.show()