# UnitySmartAvatar
locally hosted smart avatars for Unity

# Embedded Kokoro Elements
## How to download KOKORO ONNX file
- Download `kokoro-v1.0.onnx` from `https://github.com/taylorchu/kokoro-onnx/releases/tag/v0.2.0`
- Save to: `Assets/StreamingAssets/Kokoro/kokoro.onnx`
## How to download KOKORO VOICES
- Install python; install BeautifulSoup4 for python
- Run `getVoices.py` to download ALL voices from HuggingFace
- Due to the relative location of `getVoices.py`, files should land in `Assets/StreamingAssets/Kokoro/voices`