import numpy as np
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import train_test_split
from sklearn.metrics import r2_score, mean_absolute_error
import joblib

# 1. Load the processed data
print("Loading processed data...")
X = np.load('X_train.npy')
y = np.load('y_train.npy')

X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

print(f"Training on {X_train.shape[0]} samples. This may take a moment...")
model = RandomForestRegressor(n_estimators=100, max_depth=15, n_jobs=-1, random_state=42)
model.fit(X_train, y_train)
# Get importance scores
importances = model.feature_importances_
indices = np.argsort(importances)[::-1] # Sort highest to lowest

print("Feature Ranking (Top 10):")
for f in range(10):
    print(f"{f + 1}. Channel {indices[f] + 1} (Score: {importances[indices[f]]:.4f})")
# 4. Evaluate the Model
y_pred = model.predict(X_test)
r2 = r2_score(y_test, y_pred)
mae = mean_absolute_error(y_test, y_pred)

print("\n" + "="*30)
print("TRAINING COMPLETE")
print(f"R² Score: {r2:.4f} (1.0 is perfect)")
print(f"Mean Absolute Error: {mae:.4f}")
print("="*30)

# 5. Save the trained model to a file
joblib.dump(model, 'grasp_force_model.pkl')
print("Model saved as: grasp_force_model.pkl")