import numpy as np
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import train_test_split
import joblib

# 1. Load your data
X = np.load('X_train.npy')
y = np.load('y_train.npy')

# Indices are Channel-1: [7, 4, 17, 6]
top_4_indices = [7, 4, 17, 6]
X_4ch = X[:, top_4_indices]

# 3. Split the 4-channel data
X_train, X_test, y_train, y_test = train_test_split(X_4ch, y, test_size=0.2, random_state=42)

# 4. Train the "Specialist" Model
print("Training the 4-Channel Specialist Model...")
model_4ch = RandomForestRegressor(n_estimators=100, max_depth=15, n_jobs=-1)
model_4ch.fit(X_train, y_train)

# 5. Save this as your final "Brain"
joblib.dump(model_4ch, 'final_4ch_model.pkl')

score = model_4ch.score(X_test, y_test)
print(f"Final 4-Channel R2 Score: {score:.4f}")