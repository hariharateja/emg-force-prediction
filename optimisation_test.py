import numpy as np
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import train_test_split
import joblib

# Load full data
X = np.load('X_train.npy')
y = np.load('y_train.npy')

# The indices from your ranking (Channel 8 is index 7, etc.)
top_indices = [7, 4, 17, 6, 19, 9, 5, 22] 

print(f"{'Sensors':<10} | {'R2 Score':<10} | {'MAE':<10}")
print("-" * 35)

for n in [8, 4, 2, 1]:
    # Select only the top N features
    X_sub = X[:, top_indices[:n]]
    
    X_train, X_test, y_train, y_test = train_test_split(X_sub, y, test_size=0.2, random_state=42)
    
    # Train a smaller model
    model_sub = RandomForestRegressor(n_estimators=100, max_depth=12, n_jobs=-1)
    model_sub.fit(X_train, y_train)
    
    score = model_sub.score(X_test, y_test)
    mae = np.mean(np.abs(model_sub.predict(X_test) - y_test))
    
    print(f"{n:<10} | {score:<10.4f} | {mae:<10.4f}")
    
    # Save the 4-channel model specifically for your hardware
    if n == 4:
        joblib.dump(model_sub, 'grasp_force_model_4ch.pkl')