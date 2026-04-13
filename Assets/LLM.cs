using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// Talks to a locally-hosted LLM with an OpenAI-compatible Chat Completions API.
// Default target is GPT4All (https://www.nomic.ai/gpt4all) on http://localhost:4891
// but any OpenAI-shaped endpoint works (e.g. Ollama, LM Studio, vLLM).
public class LLM : MonoBehaviour
{
    [Tooltip("OpenAI-compatible endpoint. GPT4All default is http://localhost:4891/v1/chat/completions")]
    [SerializeField] private string apiUrl = "http://localhost:4891/v1/chat/completions";

    [Tooltip("Model name passed to the endpoint. For GPT4All this should match an installed model.")]
    [SerializeField] private string model = "gpt-3.5-turbo";

    [Tooltip("Max tokens per response.")]
    [SerializeField] private int maxTokens = 200;

    [Tooltip("Oldest-to-newest message window size including the system prompt.")]
    [SerializeField] private int maxHistoryLength = 10;

    [TextArea(4, 12)]
    [Tooltip("System prompt that sets your avatar's personality and behavior.")]
    [SerializeField] private string characterPrompt =
        "You are a helpful, friendly assistant voiced by a Unity avatar. " +
        "Keep responses to 15 words or less. Be engaging and conversational.";

    private List<Message> _conversationHistory;

    private void Awake()
    {
        _conversationHistory = new List<Message>
        {
            new Message { role = "system", content = characterPrompt }
        };
    }

    public async Task<string> GetResponse(string userMessage)
    {
        Debug.Log("Sending message to LLM: " + userMessage);
        string response = await SendRequestAsync(userMessage);
        Debug.Log("LLM response: " + response);
        return response;
    }

    private async Task<string> SendRequestAsync(string message)
    {
        _conversationHistory.Add(new Message { role = "user", content = message });

        // Trim old messages, keeping the system prompt at index 0
        if (_conversationHistory.Count > maxHistoryLength)
        {
            _conversationHistory.RemoveAt(1);
        }

        // Manually build JSON payload (JsonUtility can't serialize List<Message> cleanly)
        StringBuilder jsonPayload = new StringBuilder();
        jsonPayload.Append("{");
        jsonPayload.Append("\"model\": \"" + model + "\", ");
        jsonPayload.Append("\"max_tokens\": " + maxTokens + ", ");
        jsonPayload.Append("\"messages\": [");
        for (int i = 0; i < _conversationHistory.Count; i++)
        {
            string safeContent = _conversationHistory[i].content.Replace("\"", "\\\"");
            jsonPayload.Append("{\"role\": \"" + _conversationHistory[i].role + "\", \"content\": \"" + safeContent + "\"}");
            if (i < _conversationHistory.Count - 1) jsonPayload.Append(", ");
        }
        jsonPayload.Append("]}");

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload.ToString());
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                LLMResponse response = JsonUtility.FromJson<LLMResponse>(request.downloadHandler.text);
                if (response != null && response.choices.Length > 0)
                {
                    string reply = response.choices[0].message.content;
                    _conversationHistory.Add(new Message { role = "assistant", content = reply });
                    return reply;
                }
            }
            else
            {
                Debug.LogError($"LLM Error: {request.error} (Code: {request.responseCode})");
                Debug.LogError("Full Response: " + request.downloadHandler.text);
                return "Failed to connect to LLM!";
            }
        }

        return "LLM query failed without error!";
    }
}

[Serializable]
public class LLMResponse
{
    public Choice[] choices;
}

[Serializable]
public class Choice
{
    public Message message;
}

[Serializable]
public class Message
{
    public string role;
    public string content;
}
