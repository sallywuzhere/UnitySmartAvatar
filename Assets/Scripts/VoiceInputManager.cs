using System.Threading.Tasks;
using KokoroSharp;
using KokoroSharp.Core;
using UnityEngine;
using UnityEngine.InputSystem;

public class VoiceInputManager : MonoBehaviour
{
    // Speech to text, which we will use to conver the Microphone input
    // to text, in order to send to the LLM
    [SerializeField] private SpeechToText _speechToText;
    // LLM running locally, to generate a response to the user's input,
    // which was converted from voice to text.
    [SerializeField] private LLM _llm;
    // Control what we are looking at
    [SerializeField] private IdleLookAround _idleLookAround;
    
    // Text to Speech setup
    private KokoroTTS _kokoroTTS;
    private KokoroVoice _voice;
    
    // Microphone input setup
    private string _micDeviceName;
    private AudioClip _inputAudioClip;
    private bool _isRecording;

    // Refers to a voice file in StreamingAssets/Kokoro/voices.
    // See ReadMe for how to add more voices.
    private const string VOICE_NAME = "bm_fable";
    // InferenceEngine expects mono 16 kHz audio
    private const int MIC_SAMPLE_RATE = 16000;
    // Don't go crazy.
    private const int MAX_RECORDING_LENGTH_SECONDS = 30;

    void Start()
    {
        // Load Kokoro for text-to-speech and pick a voice
        _kokoroTTS = KokoroTTS.LoadModel();
        _voice = KokoroVoiceManager.GetVoice(VOICE_NAME);

        // Pick mic device
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[VoiceInputManager] No microphone detected. Voice loop disabled.");
            return;
        }
        _micDeviceName = Microphone.devices[0];
        Debug.Log($"[VoiceInputManager] Using device: {_micDeviceName}");

        Debug.Log("[VoiceInputManager] Hold SPACE to talk. Release to hear the avatar respond.");
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
        _inputAudioClip = Microphone.Start(_micDeviceName, loop: false, MAX_RECORDING_LENGTH_SECONDS, MIC_SAMPLE_RATE);
        _isRecording = true;

        _idleLookAround.StartLookingAtCamera();

        Debug.Log("[VoiceInputManager] Recording started. Speak now...");
    }

    private async Task StopRecordingAndProcess()
    {
        // Stop recording user input
        Microphone.End(_micDeviceName);
        _isRecording = false;

        _idleLookAround.StopLookingAtCamera(delaySeconds: 5);

        // Process user input from speech to text
        Debug.Log("[VoiceInputManager] Recording stopped. Processing Speech to Text.");
        string userText = await _speechToText.GetTranscription(_inputAudioClip);
        Debug.Log($"[VoiceInputManager] User said: \"{userText}\"");
        // Return early if we couldn't get any text from the user's voice input.
        if (string.IsNullOrWhiteSpace(userText))
        {
            Debug.LogWarning("[VoiceInputManager] No speech detected, returning early.");
            return;
        }

        // Get a response from the LLM
        Debug.Log($"[VoiceInputManager] Submitting User text to LLM.");
        string reply = await _llm.GetResponse(userText);
        Debug.Log($"[VoiceInputManager] LLM Replied: \"{reply}\"");
        if (string.IsNullOrWhiteSpace(reply))
        {
            Debug.LogWarning("[VoiceInputManager] No response from LLM, returning early.");
            return;
        }

        // Kokoro cannot pronounce apostrophes or asterisks, so remove them :(
        Debug.Log("[VoiceInputManager] Generating voice and speaking reply!");
        _kokoroTTS.Speak(reply.Replace("I'm", "I am").Replace("'", "").Replace("*", ""), _voice);
    }
}
