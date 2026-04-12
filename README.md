# UnitySmartAvatar
locally hosted smart avatars for Unity

# Kokoro Package
You must install the Kokoro package for this project to work.
Do so by:
1. Opening your Unity project
2. Opening the Package Manager (Window > Package Manager)
3. Click the "+" button in the top-left
4. Select "Add package from git URL..."
5. Enter the repository URL: `https://github.com/unipotent/KokoroSharpUnity.git`
6. Click "Add"

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
- Install python; install BeautifulSoup4 for python
- Run `getVoices.py` to download ALL voices from HuggingFace
- Due to the relative location of `getVoices.py`, files should land in `Assets/StreamingAssets/Kokoro/voices`