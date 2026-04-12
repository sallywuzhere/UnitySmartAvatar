using UnityEngine;
using KokoroSharp;
using KokoroSharp.Core;
using UnityEngine.InputSystem;

public class TestKokoro : MonoBehaviour
{
    private KokoroTTS _kokoroTTS;
    private int _currVoiceIndex = 0;
    private KokoroVoice[] _allVoices;
    private KokoroLanguage[] _allLanguages;

    void Start()
    {
        _kokoroTTS = KokoroTTS.LoadModel();
        _allLanguages = new KokoroLanguage[] { KokoroLanguage.AmericanEnglish,
                                                KokoroLanguage.BritishEnglish,
                                                KokoroLanguage.Japanese,
                                                KokoroLanguage.MandarinChinese,
                                                KokoroLanguage.Spanish,
                                                KokoroLanguage.French,
                                                KokoroLanguage.Hindi,
                                                KokoroLanguage.Italian,
                                                KokoroLanguage.BrazilianPortuguese };
        _allVoices = KokoroVoiceManager.GetVoices(_allLanguages, KokoroGender.Both).ToArray();

        Debug.Log("Press Space to cycle through all available voices and Speak().");
        Speak();
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Speak();
        }
    }

    void Speak()
    {
        KokoroVoice voice = KokoroVoiceManager.GetVoice(_allVoices[_currVoiceIndex].Name);
        Debug.Log("Voice: " + voice.Name);
        _kokoroTTS.SpeakFast("Hello world", voice);

        _currVoiceIndex = (_currVoiceIndex + 1) % _allVoices.Length;
    }
}
