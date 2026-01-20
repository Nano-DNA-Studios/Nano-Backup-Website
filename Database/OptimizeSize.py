import os
import json
import matplotlib.pyplot as plt
from collections import defaultdict

def get_compression_stats(base_path, variations):
    file_data = defaultdict(dict)
    total_folder_sizes = {}

    print("Starting scan of optimized folders...")
    for var in variations:
        folder_name = f"Class Backup SQL Test{var}"
        folder_path = os.path.join(base_path, folder_name)
        
        if not os.path.exists(folder_path):
            continue

        total_size = 0
        for root, _, files in os.walk(folder_path):
            for f in files:
                if f.endswith('.7z'):
                    path = os.path.join(root, f)
                    size_mb = os.path.getsize(path) / (1024.0 * 1024.0)
                    file_data[f][folder_name] = size_mb
                    total_size += size_mb
        
        total_folder_sizes[folder_name] = total_size
        print(f"Processed {folder_name}: Total {total_size:.2f} MB")
        
    return dict(file_data), total_folder_sizes

def Plot(sizes, title):
    
    labels = sorted(sizes.keys(), key=lambda x: int(x.replace('MB', '')))
    values = [sizes[l] for l in labels]

    plt.subplot(1, 1, 1)
    plt.title(f'Total Storage Size vs. Solid Block Size ({title})')
    plt.ylabel('Size (MB)')
    plt.xlabel('Block Size Variation')

    plt.plot(labels, values)
    plt.tight_layout()
    plt.savefig(f'Comparison-{title}.png')
    plt.show()
    
def PlotFullComparison(file_variations, label, labels):
    try:
        y_values = [(file_variations[l] / file_variations[list(file_variations.keys())[0]]) * 100.0 for l in file_variations.keys()]
        plt.plot(labels, y_values, label=label)
    except KeyError:
        print(f"Skipping plot for {label}: Missing data for some variations")

# Config
ROOT_PATH = "E:/Backups/Important Backup/School"
LABELS = ["1MB", "2MB", "4MB", "8MB", "16MB", "32MB", "64MB", "Solid Block (Original)"]
VARIANTS = [" - Optimized - 1MB", " - Optimized - 2MB", " - Optimized - 4MB", " - Optimized - 8MB", " - Optimized - 16MB", " - Optimized - 32MB", " - Optimized - 64MB", ""]
JSON_OUT = "CompressionResults.json"

individual_files, total_totals = get_compression_stats(ROOT_PATH, VARIANTS)

with open(JSON_OUT, 'w') as f:
    json.dump({"individual_files": individual_files, "total_sizes": total_totals}, f, indent=4)

plt.figure(figsize=(10, 6))
plt.title('Compression Efficiency vs. Solid Block Size')
plt.ylabel('Percentage of Original Size (%)')
plt.xlabel('Block Size Variation')

# 4. Process Labels (Fix for subscriptable error)
all_filenames = list(individual_files.keys())
if all_filenames:
    first_file = all_filenames[0]

    for file_name, variations in individual_files.items():
        PlotFullComparison(variations, file_name, LABELS)

    plt.legend(loc='lower left', fontsize='small', ncol=2)
    plt.ylim((87, 102))
    plt.tight_layout()
    plt.savefig('Comparison.png')
    plt.show()
else:
    print("No files found to plot.")