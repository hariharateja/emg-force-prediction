import os
import glob

def get_latest_files(data_dir, subjects=['03', '04', '05', '06', '07']):
    categories = ['mvc', 'sequential', 'repeats_long', 'repeats_short']
    organized_files = {sub: {cat: None for cat in categories} for sub in subjects}

    for sub in subjects:
        for cat in categories:
            pattern = os.path.join(data_dir, f"emg_force-{sub}-{cat}-*.hdf5")
            files = sorted(glob.glob(pattern))
            if files:
                organized_files[sub][cat] = files[-1]
    
    return organized_files

# Usage
DATA_PATH = 'data' # Simplified path
my_data = get_latest_files(DATA_PATH)

GREEN = '\033[92m'
RED = '\033[91m'
RESET = '\033[0m'

# Safety check for printing
sub_03_seq = my_data['03']['sequential']
if sub_03_seq:
    print(f"Latest Subject 03 Sequential file: {GREEN}{sub_03_seq}{RESET}")
else:
    print(f"Latest Subject 03 Sequential file: {RED}NOT FOUND{RESET}")