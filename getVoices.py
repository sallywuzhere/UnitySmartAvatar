"""
Downloads Kokoro voices from hexgrad/Kokoro-82M on HuggingFace and converts them
from PyTorch .pt format to the NumPy .npy format that KokoroSharpUnity loads.

Also downloads any .md docs from the repo root for reference.

Run from the repo root:
    pip install -r requirements.txt
    python3 getVoices.py
"""
import os
import requests
import torch
import numpy as np

REPO = "hexgrad/Kokoro-82M"
HF_API = f"https://huggingface.co/api/models/{REPO}/tree/main"
HF_RESOLVE = f"https://huggingface.co/{REPO}/resolve/main"

PROJECT_ROOT = os.path.dirname(os.path.abspath(__file__))
VOICES_DIR = os.path.join(PROJECT_ROOT, "Assets/StreamingAssets/Kokoro/voices")
os.makedirs(VOICES_DIR, exist_ok=True)


def list_files(subpath=""):
    """List all files at a given path in the HuggingFace repo via the API."""
    url = f"{HF_API}/{subpath}" if subpath else HF_API
    r = requests.get(url)
    r.raise_for_status()
    return [entry["path"] for entry in r.json() if entry["type"] == "file"]


def download(repo_path):
    """Download a file from the repo to VOICES_DIR by its repo-relative path."""
    filename = os.path.basename(repo_path)
    dest = os.path.join(VOICES_DIR, filename)
    print(f"Downloading {filename}")
    with open(dest, "wb") as f:
        f.write(requests.get(f"{HF_RESOLVE}/{repo_path}").content)


# 1. Download .md docs from repo root
for path in list_files():
    if path.endswith(".md"):
        download(path)

# 2. Download all .pt voice files
for path in list_files("voices"):
    if path.endswith(".pt"):
        download(path)

# 3. Convert each .pt tensor to a float32 .npy array (the format KokoroSharp needs)
for filename in os.listdir(VOICES_DIR):
    if not filename.endswith(".pt"):
        continue
    pt_path = os.path.join(VOICES_DIR, filename)
    try:
        features = torch.load(pt_path, weights_only=False).numpy().astype(np.float32)
        np.save(pt_path.replace(".pt", ".npy"), features)
        os.remove(pt_path)
    except Exception as e:
        print(f"Error converting {filename}: {e}")

# 4. Summary
files = sorted(os.listdir(VOICES_DIR))
print(f"\n{len(files)} files in {VOICES_DIR}:")
for f in files:
    print(f"  {f}")
