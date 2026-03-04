import h5py

# Pick one of your downloaded files
file_path = "data/emg_force-03-mvc-2018-06-14-12-49-19-096.hdf5"

with h5py.File(file_path, 'r') as f:
    print(f"File Keys: {list(f.keys())}")
    def print_structure(name, obj):
        print(f"  {name} (Type: {type(obj)})")
    f.visititems(print_structure)