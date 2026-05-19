import numpy as np
import joblib
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import train_test_split
from sklearn.metrics import r2_score, mean_squared_error

# 1. Load data
X = np.load('X_train.npy')
y = np.load('y_train.npy')

# 2. Use Top 4 Channels [8, 5, 18, 7]
top_4_indices = [7, 4, 17, 6]
X_4ch = X[:, top_4_indices]

# 3. SHUFFLED SPLIT (This fixes the 0.0000 R2 issue)
# This ensures training and testing both have a mix of force levels
X_train, X_test, y_train, y_test = train_test_split(
    X_4ch, y, test_size=0.2, random_state=42, shuffle=True
)

print(f"Training on {len(X_train)} samples...")
print(f"Testing on {len(X_test)} samples...")

# 4. Train Model
model = RandomForestRegressor(n_estimators=100, max_depth=15, n_jobs=-1, random_state=42)
model.fit(X_train, y_train)

# 5. Evaluate
y_pred = model.predict(X_test)
r2 = r2_score(y_test, y_pred)
mse = mean_squared_error(y_test, y_pred)

print("\n" + "="*40)
print("VALIDATED 4-CHANNEL RESULTS (SHUFFLED)")
print(f"R-Squared (R2) Score: {r2:.4f}")
print(f"Mean Squared Error:   {mse:.4f}")
print("="*40)