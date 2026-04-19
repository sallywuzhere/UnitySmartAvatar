#!/usr/bin/env bash
# Downloads the Whisper ONNX model weights from Hugging Face into Assets/Data/Models.
# Source: https://huggingface.co/unity/inference-engine-whisper-tiny

set -euo pipefail

BASE_URL="https://huggingface.co/unity/inference-engine-whisper-tiny/resolve/main/models"
DEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/Assets/Data/Models"

FILES=(
  "logmel_spectrogram.onnx"
  "encoder_model.onnx"
  "decoder_model.onnx"
  "decoder_with_past_model.onnx"
)

mkdir -p "${DEST_DIR}"

for f in "${FILES[@]}"; do
  out="${DEST_DIR}/${f}"
  if [[ -s "${out}" ]]; then
    echo "✓ ${f} already exists, skipping."
    continue
  fi
  echo "↓ Downloading ${BASE_URL}/${f} ..."
  curl -L --fail --progress-bar -o "${out}" "${BASE_URL}/${f}"
done

echo "All Whisper models are in ${DEST_DIR}"
