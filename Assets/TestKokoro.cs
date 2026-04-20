using System.Threading.Tasks;
using KokoroSharp;
using KokoroSharp.Core;
using UnityEngine;
using UnityEngine.InputSystem;

// Push-to-talk voice loop:
//   hold Space → record mic
//   release    → Whisper (STT) → LLM → Kokoro (TTS)
public class TestKokoro : MonoBehaviour
{
    [Header("AI Components")]
    [SerializeField] private Whisper _whisper;
    [SerializeField] private LLM _llm;

    [Header("Mic")]
    [Tooltip("Max seconds captured per utterance. Whisper truncates at 30s anyway.")]
    [SerializeField] private int _maxRecordingSeconds = 30;

    [Header("Voice")]
    [Tooltip("Exact Kokoro voice name to use (e.g. af_bella). Leave blank to pick the first American English voice.")]
    [SerializeField] private string _voiceName = "";

    // Whisper expects mono 16 kHz audio
    private const int MIC_SAMPLE_RATE = 16000;

    private KokoroTTS _kokoroTTS;
    private KokoroVoice _voice;
    private AudioClip _micClip;
    private string _micDevice;
    private bool _isRecording;

    void Start()
    {
        // Load Kokoro + pick a voice
        _kokoroTTS = KokoroTTS.LoadModel();
        _voice = string.IsNullOrEmpty(_voiceName)
            ? KokoroVoiceManager.GetVoices(KokoroLanguage.AmericanEnglish)[0]
            : KokoroVoiceManager.GetVoice(_voiceName);
        Debug.Log($"[Kokoro] Using voice: {_voice.Name}");

        // Pick mic device
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected — voice loop disabled.");
            return;
        }
        _micDevice = Microphone.devices[0];
        Debug.Log($"[Mic] Using device: {_micDevice}");

        Debug.Log("Hold SPACE to talk. Release to hear the avatar respond.");
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartRecording();
        }
        else if (Keyboard.current.spaceKey.wasReleasedThisFrame && _isRecording)                                                                                                                      
        {
            _ = RunLoop();
        }
    }

    private async Task RunLoop()
    {                                                                                                                                                                                             
        try
        {
            await StopRecordingAndProcess();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Voice loop failed: {e}");
        }
    } 

    private void StartRecording()
    {
        _micClip = Microphone.Start(_micDevice, loop: false, _maxRecordingSeconds, MIC_SAMPLE_RATE);
        _isRecording = true;
        Debug.Log("[Mic] Recording...");
    }

    private async Task StopRecordingAndProcess()
    {
        Microphone.End(_micDevice);
        _isRecording = false;
        Debug.Log("[Mic] Stopped. Transcribing...");

        // STT
        string userText = await _whisper.GetTranscription(_micClip);
        Debug.Log($"[Whisper] You said: \"{userText}\"");
        if (string.IsNullOrWhiteSpace(userText)) return;

        // LLM
        string reply = await _llm.GetResponse(userText);
        Debug.Log($"[LLM] Replied: \"{reply}\"");
        if (string.IsNullOrWhiteSpace(reply)) return;

        // TTS struggles with apostrophes, so remove them
        _kokoroTTS.Speak(reply.Replace("'", ""), _voice);
    }
}
