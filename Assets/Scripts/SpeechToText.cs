using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

// Original implementation of InferenceEngine-based Speech to Text, adapted from the HuggingFace Whisper Tiny example:
// https://huggingface.co/unity/inference-engine-whisper-tiny/blob/main/RunWhisper.cs
public class SpeechToText : MonoBehaviour
{
    public Action<string> AudioTranscribed;

    // Tokenizer
    [SerializeField] private TextAsset _jsonFile;
    [SerializeField] private Unity.InferenceEngine.ModelAsset _audioDecoder1;
    [SerializeField] private Unity.InferenceEngine.ModelAsset _audioDecoder2;
    [SerializeField] private Unity.InferenceEngine.ModelAsset _audioEncoder;
    [SerializeField] private Unity.InferenceEngine.ModelAsset _logMelSpectro;

    // InferenceEngine WORKERS
    // 
    // A spectrogram is a visual representation of sound frequencies over time. It shows:
    // X-axis: Time
    // Y-axis: Frequency (pitch)
    // Color/Intensity: Amplitude (loudness)
    // Since raw audio data (a waveform) is too complex for AI to understand directly,
    // we convert it into a spectrogram, which represents how frequencies evolve over time!
    private Unity.InferenceEngine.Worker _decoder1, _decoder2, _encoder, _spectrogram;
    private Unity.InferenceEngine.Worker _argmax;

    // InferenceEngine enums
    private const int CHATGPT_MAX_TOKENS = 100;
    private const int INFERRENCE_ENGINE_END_OF_TEXT = 50257;
    private const int INFERRENCE_ENGINE_START_OF_TRANSCRIPT = 50258;
    private const int INFERRENCE_ENGINE_ENGLISH = 50259;
    private const int INFERRENCE_ENGINE_TRANSCRIBE = 50359;
    private const int INFERRENCE_ENGINE_NO_TIME_STAMPS = 50363;
    private const int INFERRENCE_ENGINE_START_TIME = 50364;

    private int[] _whiteSpaceCharacters = new int[256];
    private string[] _vocabTokens;
    private int _transcribedTokenCount = 0;
    private bool _isTranscribingInputAudio = false;
    private string _audioInputTranscription = "";

    private const int MAX_SAMPLES = 30 * 16000; // Maximum size of audioClip (30s at 16kHz)
    private NativeArray<int> _transcribedTokens;
    private Unity.InferenceEngine.Tensor<float> _encodedAudio;

    // Audio processing
    //Awaitable m_Awaitable;
    private NativeArray<int> _lastToken;
    private Unity.InferenceEngine.Tensor<int> _lastTokenTensor;
    private Unity.InferenceEngine.Tensor<int> _transcribedTokensTensor;
    private Unity.InferenceEngine.Tensor<float> _audioInputTensor;

    private void Awake()
    {
        SetupTokensFromVocabFile(_jsonFile);
        SetupWhiteSpaceShifts();
    }

    public async Task<string> GetTranscription(AudioClip audioClip)
    {
        if(_isTranscribingInputAudio)
        {
            Debug.LogWarning("[SpeechToText] You have requested BeginTranscription, but a transcription is already in progress.");
            return "";
        }

        // Setup workers
        SetupInferenceEngineWorkers();

        // Prepare input to be processed
        EncodeAudioClip(audioClip);

        // Process input as transcription
        await BeginTranscription();

        // Clean up transcription related stuff for more input
        CleanupInferenceEngineWorkers();

        // Return the string we've built across multiple inference steps
        return _audioInputTranscription;
    }

    private void SetupInferenceEngineWorkers()
    {
        _decoder1 = new Unity.InferenceEngine.Worker(Unity.InferenceEngine.ModelLoader.Load(_audioDecoder1), Unity.InferenceEngine.BackendType.GPUCompute);
        _decoder2 = new Unity.InferenceEngine.Worker(Unity.InferenceEngine.ModelLoader.Load(_audioDecoder2), Unity.InferenceEngine.BackendType.GPUCompute);
        _encoder = new Unity.InferenceEngine.Worker(Unity.InferenceEngine.ModelLoader.Load(_audioEncoder), Unity.InferenceEngine.BackendType.GPUCompute);
        _spectrogram = new Unity.InferenceEngine.Worker(Unity.InferenceEngine.ModelLoader.Load(_logMelSpectro), Unity.InferenceEngine.BackendType.GPUCompute);

        Unity.InferenceEngine.FunctionalGraph graph = new Unity.InferenceEngine.FunctionalGraph();
        var input = graph.AddInput(Unity.InferenceEngine.DataType.Float, new Unity.InferenceEngine.DynamicTensorShape(1, 1, 51865));
        var amax = Unity.InferenceEngine.Functional.ArgMax(input, -1, false);
        var selectTokenModel = graph.Compile(amax);
        _argmax = new Unity.InferenceEngine.Worker(selectTokenModel, Unity.InferenceEngine.BackendType.GPUCompute);

        // Setup transcribed token native array
        _transcribedTokens = new NativeArray<int>(CHATGPT_MAX_TOKENS, Allocator.Persistent);
        _transcribedTokens[0] = INFERRENCE_ENGINE_START_OF_TRANSCRIPT;
        _transcribedTokens[1] = INFERRENCE_ENGINE_ENGLISH;
        _transcribedTokens[2] = INFERRENCE_ENGINE_TRANSCRIBE;
        _transcribedTokenCount = 3;
    }

    private void CleanupInferenceEngineWorkers()
    {
        // InferenceEngine worker disposal
        _decoder1.Dispose();
        _decoder2.Dispose();
        _encoder.Dispose();
        _spectrogram.Dispose();
        _argmax.Dispose();

        // Tensor disposal
        _audioInputTensor?.Dispose();
        _lastTokenTensor?.Dispose();
        _transcribedTokensTensor?.Dispose();
        _encodedAudio?.Dispose();

        // Native Array disposal
        if (_transcribedTokens.IsCreated)
        {
            _transcribedTokens.Dispose();   
        }
        if (_lastToken.IsCreated)
        {
            _lastToken.Dispose();
        }
    }

    private void EncodeAudioClip(AudioClip audioClip)
    {
        float[] audioClipData = new float[MAX_SAMPLES]; // expects specific numbers, not just min
        audioClip.GetData(audioClipData, 0);

        // Populate our audio input tensor
        _audioInputTensor = new Unity.InferenceEngine.Tensor<float>(new Unity.InferenceEngine.TensorShape(1, audioClipData.Length), audioClipData);

        // Converts raw audio tensor into a spectrogram (visualizer of sound frequencies over time)
        _spectrogram.Schedule(_audioInputTensor);

        // A log-mel spectrogram is an enhanced spectrogram that mimics human hearing, instead of
        // just raw data. It's called "log" because it uses a logarithmic scale to balance loud
        // and quiet sounds out, more like a human ear might.
        // So, here we retrieve the log-mel spectrogram tensor from the model.
        var logmel = _spectrogram.PeekOutput() as Unity.InferenceEngine.Tensor<float>;

        // Feeds the spectrogram into a neural network encoder
        _encoder.Schedule(logmel);

        // Retrieves the AI-ready encoded audio tensor from the model
        _encodedAudio = _encoder.PeekOutput() as Unity.InferenceEngine.Tensor<float>;
    }

    private async Task BeginTranscription()
    {
        // Clear.
        _audioInputTranscription = "";
        _isTranscribingInputAudio = true;

        _transcribedTokensTensor = new Unity.InferenceEngine.Tensor<int>(new Unity.InferenceEngine.TensorShape(1, CHATGPT_MAX_TOKENS));
        Unity.InferenceEngine.ComputeTensorData.Pin(_transcribedTokensTensor);
        _transcribedTokensTensor.Reshape(new Unity.InferenceEngine.TensorShape(1, _transcribedTokenCount));
        _transcribedTokensTensor.dataOnBackend.Upload<int>(_transcribedTokens, _transcribedTokenCount);

        _lastToken = new NativeArray<int>(1, Allocator.Persistent);
        _lastToken[0] = INFERRENCE_ENGINE_NO_TIME_STAMPS;
        _lastTokenTensor = new Unity.InferenceEngine.Tensor<int>(new Unity.InferenceEngine.TensorShape(1, 1), new[] { INFERRENCE_ENGINE_NO_TIME_STAMPS });

        while (true)
        {
            if (!_isTranscribingInputAudio || _transcribedTokenCount >= (_transcribedTokens.Length - 1))
            {
                AudioTranscribed?.Invoke(_audioInputTranscription);
                return;
            }
            await InferenceStep();
        }
    }

    private void SetupTokensFromVocabFile(TextAsset jsonFile)
    {
        var vocab = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonFile.text);
        _vocabTokens = new string[vocab.Count];
        foreach (var item in vocab)
        {
            _vocabTokens[item.Value] = item.Key;
        }
    }

    private void SetupWhiteSpaceShifts()
    {
        for (int i = 0, n = 0; i < 256; i++)
        {
            if (IsWhiteSpace((char)i))
            {
                _whiteSpaceCharacters[n++] = i;
            }
        }
    }

    private async Awaitable InferenceStep()
    {
        _decoder1.SetInput("input_ids", _transcribedTokensTensor);
        _decoder1.SetInput("encoder_hidden_states", _encodedAudio);
        _decoder1.Schedule();

        var past_key_values_0_decoder_key = _decoder1.PeekOutput("present.0.decoder.key") as Unity.InferenceEngine.Tensor<float>;
        var past_key_values_0_decoder_value = _decoder1.PeekOutput("present.0.decoder.value") as Unity.InferenceEngine.Tensor<float>;
        var past_key_values_1_decoder_key = _decoder1.PeekOutput("present.1.decoder.key") as Unity.InferenceEngine.Tensor<float>;
        var past_key_values_1_decoder_value = _decoder1.PeekOutput("present.1.decoder.value") as Unity.InferenceEngine.Tensor<float>;
        var past_key_values_2_decoder_key = _decoder1.PeekOutput("present.2.decoder.key") as Unity.InferenceEngine.Tensor<float>;
        var past_key_values_2_decoder_value = _decoder1.PeekOutput("present.2.decoder.value") as Unity.InferenceEngine.Tensor<float>;
        var past_key_values_3_decoder_key = _decoder1.PeekOutput("present.3.decoder.key") as Unity.InferenceEngine.Tensor<float>;
        var past_key_values_3_decoder_value = _decoder1.PeekOutput("present.3.decoder.value") as Unity.InferenceEngine.Tensor<float>;

        var past_key_values_0_encoder_key = _decoder1.PeekOutput("present.0.encoder.key") as Unity.InferenceEngine.Tensor<float>;
        var past_key_values_0_encoder_value = _decoder1.PeekOutput("present.0.encoder.value") as Unity.InferenceEngine.Tensor<float>;
        var past_key_values_1_encoder_key = _decoder1.PeekOutput("present.1.encoder.key") as Unity.InferenceEngine.Tensor<float>;
        var past_key_values_1_encoder_value = _decoder1.PeekOutput("present.1.encoder.value") as Unity.InferenceEngine.Tensor<float>;
        var past_key_values_2_encoder_key = _decoder1.PeekOutput("present.2.encoder.key") as Unity.InferenceEngine.Tensor<float>;
        var past_key_values_2_encoder_value = _decoder1.PeekOutput("present.2.encoder.value") as Unity.InferenceEngine.Tensor<float>;
        var past_key_values_3_encoder_key = _decoder1.PeekOutput("present.3.encoder.key") as Unity.InferenceEngine.Tensor<float>;
        var past_key_values_3_encoder_value = _decoder1.PeekOutput("present.3.encoder.value") as Unity.InferenceEngine.Tensor<float>;

        _decoder2.SetInput("input_ids", _lastTokenTensor);
        _decoder2.SetInput("past_key_values.0.decoder.key", past_key_values_0_decoder_key);
        _decoder2.SetInput("past_key_values.0.decoder.value", past_key_values_0_decoder_value);
        _decoder2.SetInput("past_key_values.1.decoder.key", past_key_values_1_decoder_key);
        _decoder2.SetInput("past_key_values.1.decoder.value", past_key_values_1_decoder_value);
        _decoder2.SetInput("past_key_values.2.decoder.key", past_key_values_2_decoder_key);
        _decoder2.SetInput("past_key_values.2.decoder.value", past_key_values_2_decoder_value);
        _decoder2.SetInput("past_key_values.3.decoder.key", past_key_values_3_decoder_key);
        _decoder2.SetInput("past_key_values.3.decoder.value", past_key_values_3_decoder_value);

        _decoder2.SetInput("past_key_values.0.encoder.key", past_key_values_0_encoder_key);
        _decoder2.SetInput("past_key_values.0.encoder.value", past_key_values_0_encoder_value);
        _decoder2.SetInput("past_key_values.1.encoder.key", past_key_values_1_encoder_key);
        _decoder2.SetInput("past_key_values.1.encoder.value", past_key_values_1_encoder_value);
        _decoder2.SetInput("past_key_values.2.encoder.key", past_key_values_2_encoder_key);
        _decoder2.SetInput("past_key_values.2.encoder.value", past_key_values_2_encoder_value);
        _decoder2.SetInput("past_key_values.3.encoder.key", past_key_values_3_encoder_key);
        _decoder2.SetInput("past_key_values.3.encoder.value", past_key_values_3_encoder_value);

        _decoder2.Schedule();

        var logits = _decoder2.PeekOutput("logits") as Unity.InferenceEngine.Tensor<float>;
        _argmax.Schedule(logits);
        using var t_Token = await _argmax.PeekOutput().ReadbackAndCloneAsync() as Unity.InferenceEngine.Tensor<int>;
        int index = t_Token[0];

        // Transcribe next token
        _transcribedTokens[_transcribedTokenCount] = _lastToken[0];
        _lastToken[0] = index;
        _transcribedTokenCount++;
        _transcribedTokensTensor.Reshape(new Unity.InferenceEngine.TensorShape(1, _transcribedTokenCount));
        _transcribedTokensTensor.dataOnBackend.Upload<int>(_transcribedTokens, _transcribedTokenCount);
        _lastTokenTensor.dataOnBackend.Upload<int>(_lastToken, 1);

        if (index == INFERRENCE_ENGINE_END_OF_TEXT)
        {
            _isTranscribingInputAudio = false;
        }
        else if (index < _vocabTokens.Length)
        {
            _audioInputTranscription += GetUnicodeText(_vocabTokens[index]);
        }
    }

    private string GetUnicodeText(string text)
    {
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
        return Encoding.UTF8.GetString(bytes);
    }

    private string ShiftCharacterDown(string text)
    {
        string outText = "";
        foreach (char letter in text)
        {
            outText += ((int)letter <= 256) ? letter :
                (char)_whiteSpaceCharacters[(int)(letter - 256)];
        }
        return outText;
    }

    private bool IsWhiteSpace(char c)
    {
        return !(('!' <= c && c <= '~') || ('�' <= c && c <= '�') || ('�' <= c && c <= '�'));
    }

}
