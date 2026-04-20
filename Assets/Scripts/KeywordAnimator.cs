using UnityEngine;

public class KeywordAnimator : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private SpeechToText _speechToText;
    [SerializeField] private LLM _llm;

    private void Start()
    {
        _llm.OnResponseReceived += HandleLLMResponse;
        _speechToText.AudioTranscribed += HandleAudioTranscribed;
    }

    private void OnDestroy()
    {
        _llm.OnResponseReceived -= HandleLLMResponse;
        _speechToText.AudioTranscribed -= HandleAudioTranscribed;
    }

    private void HandleAudioTranscribed(string transcript)
    {
        if(transcript.ToLower().Contains("applause"))
        {
            _animator.SetTrigger("Clap");
        }
    }

    private void HandleLLMResponse(string response)
    {
        // Point at the user anytime he uses this slang word
        if (response.ToLower().Contains("mate"))
        {
            _animator.SetTrigger("Point");
        }
    }
}
