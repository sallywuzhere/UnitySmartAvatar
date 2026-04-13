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
- Download `kokoro-v1.0.onnx` from `https://github.com/taylorchu/kokoro-onnx/releases/tag/v0.2.0`
- Save to: `Assets/StreamingAssets/Kokoro/kokoro.onnx`
## How to download KOKORO VOICES
Install Python 3, then from the repo root:
```bash
pip install -r requirements.txt
python3 getVoices.py
```
This fetches every voice from [hexgrad/Kokoro-82M](https://huggingface.co/hexgrad/Kokoro-82M) via the HuggingFace API, converts them from PyTorch `.pt` to the `.npy` format KokoroSharpUnity requires, and drops them in `Assets/StreamingAssets/Kokoro/voices`.

# Whisper (STT) + Local LLM

Two scripts that let your avatar hear and think locally (no cloud APIs, no third-party data sharing).

- **`Assets/Whisper.cs`** — speech-to-text running fully on-device via Unity's InferenceEngine.
- **`Assets/LLM.cs`** — conversational LLM client for any OpenAI-compatible endpoint. Works out of the box with GPT4All, Ollama, LM Studio, vLLM, etc.

## Whisper setup

1. Download the model weights (~400 MB, gitignored):
   ```bash
   ./download_models.sh
   ```
   This fetches four `.onnx` files from [huggingface.co/unity/sentis-whisper-tiny](https://huggingface.co/unity/sentis-whisper-tiny) into `Assets/Data/Models/`.

2. Attach the `Whisper` script to a GameObject in your scene. In the inspector, set these references:
   - `Json File` → `Assets/Data/vocab.json` (tokenizer, already committed)
   - `Audio Encoder` / `Audio Decoder 1` / `Audio Decoder 2` / `Log Mel Spectro` → the four `.onnx` files you just downloaded

3. Call `await whisper.GetTranscription(audioClip)` with a mono 16 kHz AudioClip (≤30s) to get back a transcription string.

## Local LLM setup

The `LLM` component talks to any OpenAI-compatible Chat Completions endpoint. We have tested on Gpt4All:
- **GPT4All** — [nomic.ai/gpt4all](https://www.nomic.ai/gpt4all) (default; runs on `http://localhost:4891`)

### Steps

1. Install and launch your LLM of choice, then enable its local API server.

2. Attach the `LLM` script to a GameObject. Configure in the inspector:
   - `Api Url` → your endpoint (defaults to GPT4All's `http://localhost:4891/v1/chat/completions`)
   - `Model` → the model name your server expects
   - `Character Prompt` → your avatar's personality / system prompt

3. Call `await llm.GetResponse(userText)` to get a reply. The script maintains a rolling conversation history.

## Required packages

Already added to `Packages/manifest.json`:
- `com.unity.ai.inference` — runs the Whisper ONNX model
- `com.unity.nuget.newtonsoft-json` — parses the Whisper tokenizer vocab