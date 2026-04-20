# UnitySmartAvatar
This project demonstrates a Smart Avatar in Unity. The Smart Avatar can be run completely offline, using local models for Speech to Text, Text to Speech, and an LLM Brain. It illustrates three types of animation: Runtime LipSync, Animation Clips, and Procedural animation.

## Installation Instructions
- Install Unity Hub: https://docs.unity.com/en-us/hub/install-hub
    - Use the Hub to install Unity 6000.3.13f1 (or latest LTS)
        - Optional: Install Android support for Unity to avoid an error later
- Install VSCode (or equivalent IDE)
    - Once loaded, install Unity plugin
- Install GPT4All v3.10.0 or newer: https://www.nomic.ai/gpt4all
    - Once loaded, install the Llama model
    - Turn on API support: `Settings > Application > Enable Local API Server` so that Unity can connect via local command line!
- Download this UnitySmartAvatar project: https://github.com/sallywuzhere/UnitySmartAvatar
    - Download `kokoro.onnx` from `https://github.com/taylorchu/kokoro-onnx/releases/tag/v0.2.0`
        - Save to: `Assets/StreamingAssets/Kokoro/kokoro.onnx` within UnitySmartAvatar
        - This is the model needed to run Text to Speech
    - Double-click to run `download_models.sh`
        - This fetches four `.onnx` files from [huggingface.co/unity/sentis-whisper-tiny](https://huggingface.co/unity/sentis-whisper-tiny) into `Assets/Data/Models/`.
        - The will save to `Assets/Data/Models` within UnitySmartAvatar
        - These are the models needed to run Speech to Text
    - Open UnitySmartAvatar project via the Unity Hub
        - Open Main.unity
        - Find the SmartUnityKun prefab; In the inspector, set these references on the SpeechToText component:
            - `Audio Encoder` / `Audio Decoder 1` / `Audio Decoder 2` / `Log Mel Spectro` must be set to the four `.onnx` files you downloaded
            - You will need to do the same thing in RoundtripTest.unity, if you wish to use that scene for testing.
        - If you see an error message, hit Ignore
        - If you launch the project and see this error in the Console:
            - `Library\PackageCache\com.unipotent.kokorosharpunity@554c775e4e00\Runtime\KokoroSharp\Processing\Tokenizer.cs(10,13): error CS0234: The type or namespace name 'Android' does not exist in the namespace 'Unity' (are you missing an assembly reference?)`
            - Double click the message to open Tokenizer.cs; commend out line 10, `//using Unity.Android.Gradle;`

Optional:

- Use VRoid Editor to create Avatars: https://vroid.com/en/studio
- Use Photopea to edit Avatar textures: https://www.photopea.com/


## How to download _more_ KOKORO VOICES
If you want more voices, you have options!
Install Python 3, then from the repo root:
```bash
pip install -r requirements.txt
python3 getVoices.py
```
This fetches every voice from [hexgrad/Kokoro-82M](https://huggingface.co/hexgrad/Kokoro-82M) via the HuggingFace API, converts them from PyTorch `.pt` to the `.npy` format KokoroSharpUnity requires, and drops them in `Assets/StreamingAssets/Kokoro/voices`.

## Additional Credits
- Unity-kun avatar: https://github.com/sallywuzhere/UnityKun
- uLipSync package: https://github.com/hecomi/uLipSync
- Kokoro Sharp package: https://github.com/unipotent/KokoroSharpUnity.git

