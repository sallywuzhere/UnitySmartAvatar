using System.Threading.Tasks;
using KokoroSharp;
using KokoroSharp.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class RoundTripTest : MonoBehaviour
{
    // Speech to text, which we will use to conver the Microphone input
    // to text, in order to send to the LLM
    [SerializeField] private SpeechToText _speechToText;
    // LLM running locally, to generate a response to the user's input,
    // which was converted from voice to text.
    [SerializeField] private LLM _llm;
    [SerializeField] private TextMeshProUGUI _debugText;
    
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
            return;
        }
        _micDeviceName = Microphone.devices[0];
    }

    public void SpeakDebugText()
    {
        _kokoroTTS.Speak(_debugText.text, _voice);
    }

    public async void SpeakLLMReply()
    {
        // Get a response from the LLM
        string reply = await _llm.GetResponse(_debugText.text);

        // Make sure we have a valid answer!
        if (string.IsNullOrWhiteSpace(reply))
        {
            reply = "ERROR: No response from LLM.";
        }

        // Set the Debug text to the response
        _debugText.text = reply;

        // Speak the debug text
        SpeakDebugText();
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartRecording();
        }
        else if (Keyboard.current.spaceKey.wasReleasedThisFrame && _isRecording)                                                                                                                      
        {
            StopRecording();
        }
    } 

    private void StartRecording()
    {
        _inputAudioClip = Microphone.Start(_micDeviceName, loop: false, MAX_RECORDING_LENGTH_SECONDS, MIC_SAMPLE_RATE);
        _isRecording = true;

        Debug.Log("[RoundTripTest] Recording started. Speak now...");
    }

    private async Task StopRecording()
    {
        // Stop recording user input
        Microphone.End(_micDeviceName);
        _isRecording = false;

        await GetTranscription_Async();
    }

    private async Task GetTranscription_Async()
    {
        // Process user input from speech to text
        string text = await _speechToText.GetTranscription(_inputAudioClip);

        // Make sure we got something
        if (string.IsNullOrWhiteSpace(text))
        {
            text = "ERROR: No speech detected.";
        }

        // Set the debug text
        _debugText.text = text;
    }
}
