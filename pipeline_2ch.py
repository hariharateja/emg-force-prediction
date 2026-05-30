"""
Full pipeline: find best 2-channel pair + optimal time lag, train final model.
Steps:
  1. Sweep all 276 channel pairs with LinearRegression (fast CV ranking)
  2. Re-test top 20 pairs with RandomForest (honest estimate)
  3. Test lag offsets 0-3 windows on best pair
  4. Train final model (RF vs GBM) on best pair + best lag
  5. Save everything
"""

import numpy as np
import joblib
import time
from itertools import combinations
from sklearn.linear_model import LinearRegression
from sklearn.ensemble import RandomForestRegressor, GradientBoostingRegressor
from sklearn.model_selection import cross_val_score, KFold, train_test_split
from sklearn.metrics import r2_score, mean_squared_error, mean_absolute_error

np.random.seed(42)

# ── Load data ──────────────────────────────────────────────────────────────────
X = np.load('X_train.npy')   # (16353, 24)  RMS features
y = np.load('y_train.npy')   # (16353,)     force labels

print(f"Data loaded: X={X.shape}, y={y.shape}")
print(f"Force range: {y.min():.2f} – {y.max():.2f}\n")

# ── Step 1: All 276 pairs ranked by LinearRegression CV R² ────────────────────
print("="*60)
print("STEP 1 — Sweeping all 276 pairs (LinearRegression, 5-fold CV)")
print("="*60)

kf = KFold(n_splits=5, shuffle=True, random_state=42)
lr = LinearRegression()

pair_scores = []
t0 = time.time()
for ch_a, ch_b in combinations(range(24), 2):
    X2 = X[:, [ch_a, ch_b]]
    scores = cross_val_score(lr, X2, y, cv=kf, scoring='r2', n_jobs=-1)
    pair_scores.append(((ch_a, ch_b), scores.mean()))

pair_scores.sort(key=lambda x: x[1], reverse=True)
print(f"Done in {time.time()-t0:.1f}s\n")

print(f"{'Rank':<6} {'Pair':<14} {'LR R²':>8}")
print("-"*32)
for rank, ((a, b), score) in enumerate(pair_scores[:20], 1):
    print(f"{rank:<6} Ch{a+1}+Ch{b+1:<8} {score:>8.4f}")

# ── Step 2: Top 20 pairs with RandomForest ────────────────────────────────────
print("\n" + "="*60)
print("STEP 2 — Top 20 pairs with RandomForest (50 trees, 3-fold CV)")
print("="*60)

top20_pairs = [p for p, _ in pair_scores[:20]]
rf_quick = RandomForestRegressor(n_estimators=50, max_depth=12, n_jobs=-1, random_state=42)
kf3 = KFold(n_splits=3, shuffle=True, random_state=42)

rf_pair_scores = []
for ch_a, ch_b in top20_pairs:
    X2 = X[:, [ch_a, ch_b]]
    scores = cross_val_score(rf_quick, X2, y, cv=kf3, scoring='r2', n_jobs=-1)
    rf_pair_scores.append(((ch_a, ch_b), scores.mean()))

rf_pair_scores.sort(key=lambda x: x[1], reverse=True)

print(f"\n{'Rank':<6} {'Pair':<14} {'RF R²':>8}")
print("-"*32)
for rank, ((a, b), score) in enumerate(rf_pair_scores[:10], 1):
    print(f"{rank:<6} Ch{a+1}+Ch{b+1:<8} {score:>8.4f}")

best_pair = rf_pair_scores[0][0]
print(f"\n>>> Best pair: Ch{best_pair[0]+1} + Ch{best_pair[1]+1}")

# ── Step 3: Optimal time lag ───────────────────────────────────────────────────
print("\n" + "="*60)
print("STEP 3 — Time lag sweep (0–3 windows = 0–300ms)")
print("="*60)
print("Window step = 100ms, so lag=1 means EMG leads force by 100ms\n")

# Each row in X corresponds to a 100ms step (512 samples @ 5120Hz).
# A positive lag means: use EMG from `lag` windows earlier to predict current force.
# i.e.  X_lagged[i] = X[i],  y_lagged[i] = y[i + lag]
# This shifts force labels forward relative to EMG.

ch_a, ch_b = best_pair
X2_full = X[:, [ch_a, ch_b]]

lag_results = []
rf_lag = RandomForestRegressor(n_estimators=100, max_depth=15, n_jobs=-1, random_state=42)

for lag in range(5):   # 0 to 400ms
    if lag == 0:
        X_lag = X2_full
        y_lag = y
    else:
        X_lag = X2_full[:-lag]      # drop last `lag` rows from EMG
        y_lag = y[lag:]             # drop first `lag` rows from force

    scores = cross_val_score(rf_lag, X_lag, y_lag, cv=kf3, scoring='r2', n_jobs=-1)
    lag_results.append((lag, scores.mean(), scores.std()))
    print(f"  Lag {lag} ({lag*100:>3d}ms): R²={scores.mean():.4f} ± {scores.std():.4f}")

best_lag = max(lag_results, key=lambda x: x[1])[0]
print(f"\n>>> Best lag: {best_lag} windows = {best_lag*100}ms")

# ── Step 4: Train final model — RF vs GBM ────────────────────────────────────
print("\n" + "="*60)
print("STEP 4 — Final model comparison (RF vs GBM)")
print("="*60)

# Apply best lag
if best_lag == 0:
    X_final = X2_full
    y_final = y
else:
    X_final = X2_full[:-best_lag]
    y_final = y[best_lag:]

# 80/20 random split (data is concatenated across subjects, so random is correct)
X_train, X_test, y_train, y_test = train_test_split(
    X_final, y_final, test_size=0.2, random_state=42
)

models = {
    'RandomForest': RandomForestRegressor(
        n_estimators=200, max_depth=15, min_samples_leaf=2,
        n_jobs=-1, random_state=42
    ),
    'GradientBoosting': GradientBoostingRegressor(
        n_estimators=300, max_depth=5, learning_rate=0.05,
        subsample=0.8, random_state=42
    ),
}

best_model_name = None
best_r2 = -np.inf
results = {}

for name, model in models.items():
    t0 = time.time()
    model.fit(X_train, y_train)
    y_pred = model.predict(X_test)
    r2  = r2_score(y_test, y_pred)
    mse = mean_squared_error(y_test, y_pred)
    mae = mean_absolute_error(y_test, y_pred)
    elapsed = time.time() - t0
    results[name] = dict(model=model, r2=r2, mse=mse, mae=mae)
    print(f"\n{name}  ({elapsed:.1f}s)")
    print(f"  R²  = {r2:.4f}")
    print(f"  MSE = {mse:.4f}")
    print(f"  MAE = {mae:.4f}")
    if r2 > best_r2:
        best_r2 = r2
        best_model_name = name

# ── Step 5: Save final model + metadata ──────────────────────────────────────
print("\n" + "="*60)
print("STEP 5 — Saving final model")
print("="*60)

best_model = results[best_model_name]['model']
joblib.dump(best_model, 'final_2ch_model.pkl')

meta = {
    'channels': [ch_a, ch_b],
    'channel_names': [f'Ch{ch_a+1}', f'Ch{ch_b+1}'],
    'lag_windows': best_lag,
    'lag_ms': best_lag * 100,
    'model_type': best_model_name,
    'r2_test': best_r2,
    'mse_test': results[best_model_name]['mse'],
    'mae_test': results[best_model_name]['mae'],
}
joblib.dump(meta, 'final_2ch_meta.pkl')

print(f"\nSaved: final_2ch_model.pkl")
print(f"Saved: final_2ch_meta.pkl")
print(f"\n{'='*60}")
print(f"FINAL RESULT")
print(f"{'='*60}")
print(f"  Model   : {best_model_name}")
print(f"  Channels: Ch{ch_a+1} + Ch{ch_b+1}  (0-indexed: {ch_a}, {ch_b})")
print(f"  Lag     : {best_lag} windows ({best_lag*100}ms)")
print(f"  R²      : {best_r2:.4f}")
print(f"  MSE     : {results[best_model_name]['mse']:.4f}")
print(f"  MAE     : {results[best_model_name]['mae']:.4f}")

# ── Bonus: show feature importance ───────────────────────────────────────────
if hasattr(best_model, 'feature_importances_'):
    fi = best_model.feature_importances_
    print(f"\n  Feature importances:")
    print(f"    Ch{ch_a+1}: {fi[0]:.4f}")
    print(f"    Ch{ch_b+1}: {fi[1]:.4f}")
