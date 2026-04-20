# UnitySmartAvatar
locally hosted smart avatars for Unity

# Kokoro Package
You may see an error about Android. To fix it:
- Install Android modules for Unity
  
OR, be a cowboy and:
- Open Library\PackageCache\com.unipotent.kokorosharpunity@554c775e4e00\Runtime\KokoroSharp\Processing\Tokenizer.cs
- Comment out the line `using Unity.Android.Gradle;`

# Embedded Kokoro Elements
## How to download KOKORO ONNX file
- Download `kokoro.onnx` from `https://github.com/taylorchu/kokoro-onnx/releases/tag/v0.2.0`
- Save to: `Assets/StreamingAssets/Kokoro/kokoro.onnx`
## How to download _more_ KOKORO VOICES
If you want more voices, you have options!
Install Python 3, then from the repo root:
```bash
pip install -r requirements.txt
python3 getVoices.py
```
This fetches every voice from [hexgrad/Kokoro-82M](https://huggingface.co/hexgrad/Kokoro-82M) via the HuggingFace API, converts them from PyTorch `.pt` to the `.npy` format KokoroSharpUnity requires, and drops them in `Assets/StreamingAssets/Kokoro/voices`.

# Local Speech to Text setup
Whisper is our local, offline Speech to Text solution.
1. Download the model weights (~400 MB, gitignored):
   ```bash
   ./download_models.sh
   ```
   This fetches four `.onnx` files from [huggingface.co/unity/sentis-whisper-tiny](https://huggingface.co/unity/sentis-whisper-tiny) into `Assets/Data/Models/`.

2. Find the UnityKun preab; In the inspector, set these references:
   - `Audio Encoder` / `Audio Decoder 1` / `Audio Decoder 2` / `Log Mel Spectro` → the four `.onnx` files you just downloaded

# Local LLM setup

The `LLM` component is the 'thinking brain' that takes user input and gives bot output.

We are using Gpt4All. To install it to your machine:
- **GPT4All** — [nomic.ai/gpt4all](https://www.nomic.ai/gpt4all) (default; runs on `http://localhost:4891`)
- Install this, and run a local session. Get to a point where you're chatting in the local Gpt4All interface, and you should be good!
