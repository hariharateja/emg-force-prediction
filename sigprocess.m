clear; close all;

fs = 1000;        % Sampling frequency
t = 0:1/fs:5;     % 5 seconds

% Simulated EMG (noise + bursts)
emg = 0.05*randn(size(t));
emg(t > 1 & t < 2) = emg(t > 1 & t < 2) + 0.5*randn(1,sum(t > 1 & t < 2));
emg(t > 3 & t < 4) = emg(t > 3 & t < 4) + 0.8*randn(1,sum(t > 3 & t < 4));

%% Bandpass filter (20–450 Hz)
bp = designfilt('bandpassiir', ...
    'FilterOrder',4,...
    'HalfPowerFrequency1',20,...
    'HalfPowerFrequency2',450,...
    'SampleRate',fs);

emg_bp = filtfilt(bp, emg);

%% 50 Hz Notch
notch = designfilt('bandstopiir', ...
    'FilterOrder',2,...
    'HalfPowerFrequency1',49,...
    'HalfPowerFrequency2',51,...
    'SampleRate',fs);

emg_filt = filtfilt(notch, emg_bp);

%% Rectification
emg_rect = abs(emg_filt);

%% Envelope (low-pass 5 Hz)
lp = designfilt('lowpassiir', ...
    'FilterOrder',4,...
    'HalfPowerFrequency',5,...
    'SampleRate',fs);

emg_env = filtfilt(lp, emg_rect);

%% Normalize
emg_norm = emg_env / max(emg_env);

%% Plot
figure;
subplot(3,1,1)
plot(t, emg), title('Raw EMG')

subplot(3,1,2)
plot(t, emg_filt), title('Filtered EMG')

subplot(3,1,3)
plot(t, emg_norm), title('Normalized EMG Envelope')
xlabel('Time (s)')