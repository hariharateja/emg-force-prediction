import numpy as np
import joblib
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import train_test_split
from sklearn.metrics import r2_score, mean_squared_error, mean_absolute_error

# 1. Load your preprocessed data
X = np.load('X_train.npy')
y = np.load('y_train.npy')

# 2. Your Top 4 Indices (Channel 8, 5, 18, 7)
# Note: Indices are Channel - 1
top_indices = [7, 4, 17, 6] 

print(f"{'Channels':<10} | {'R2 Score':<10} | {'MSE':<10} | {'MAE':<10}")
print("-" * 50)

results = {}

for n in [1, 2, 3, 4]:
    # Select subset of features
    current_indices = top_indices[:n]
    X_sub = X[:, current_indices]
    
    # Split data (use same random state for consistency)
    X_train, X_test, y_train, y_test = train_test_split(
        X_sub, y, test_size=0.2, random_state=42
    )
    
    # Initialize and train model
    # Using 100 estimators and depth 15 as per our optimized setup
    model = RandomForestRegressor(n_estimators=100, max_depth=15, n_jobs=-1, random_state=42)
    model.fit(X_train, y_train)
    
    # Predict and calculate metrics
    y_pred = model.predict(X_test)
    r2 = r2_score(y_test, y_pred)
    mse = mean_squared_error(y_test, y_pred)
    mae = mean_absolute_error(y_test, y_pred)
    
    print(f"{n:<10} | {r2:<10.4f} | {mse:<10.4f} | {mae:<10.4f}")
    
    # Save the 4-channel model specifically as your final production version
    if n == 4:
        joblib.dump(model, 'final_4ch_model.pkl')
        print("--> Saved final_4ch_model.pkl")