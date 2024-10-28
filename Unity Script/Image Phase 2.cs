using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Threading.Tasks;
using OpenAI;
using Samples.Whisper;
using NAudio.Wave;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;

public class ImagePhase2 : MonoBehaviour
{
    public RawImage cameraDisplay;
    public Button openCameraButton;
    public Button takePictureButton;
    public TextMeshProUGUI responseText;
    private WebCamTexture webCamTexture;

    public AudioSource audioSource;
    public TextMeshProUGUI wishperText;

    public GameObject recordingObject;
    public GameObject RecodeWave;
    public GameObject RecodeAnswerWave;
    public GameObject CharacterAnswerWave;
    public GameObject Listening;
    public readonly int duration = 5;

    private AudioClip clip;
    private bool isRecording;
    private readonly string fileName = "output.wav";
    private OpenAIApi openai;

    public Animator waveanimator;
    public float volumeThreshold = 0.01f;
    public float volumeUpdateInterval = 0.1f;

    private AudioHandler audioHandler;
    public AudioClip ttsclip;

    private readonly string SERVER_URL_1 = "http://localhost:4000/process-image";
    private readonly string SERVER_URL_2 = "http://localhost:5000/image-to-text";
    private readonly string SERVER_URL_3 = "http://localhost:6000/upload";

    void Start()
    {
        openCameraButton.onClick.AddListener(OpenCamera);
        takePictureButton.onClick.AddListener(TakePicture);
        takePictureButton.interactable = false;

        Listening.SetActive(false);
        RecodeWave.SetActive(false);
        RecodeAnswerWave.SetActive(false);
        CharacterAnswerWave.SetActive(false);
    }

    void OpenCamera()
    {
        if (webCamTexture == null)
        {
            webCamTexture = new WebCamTexture();
        }

        cameraDisplay.texture = webCamTexture;
        webCamTexture.Play();
        takePictureButton.interactable = true;
    }

    void TakePicture()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            Texture2D photo = new Texture2D(webCamTexture.width, webCamTexture.height);
            photo.SetPixels(webCamTexture.GetPixels());
            photo.Apply();
            string photoPath = Path.Combine(Application.persistentDataPath, "photo.png");
            File.WriteAllBytes(photoPath, photo.EncodeToPNG());
            responseText.text = "";

            // Step 3: Send the image to Server 1
            StartCoroutine(SendImageToServer(SERVER_URL_1, photo));
            StopCamera();
        }
    }

    private string server2Response;
    private string server3Response;

    IEnumerator SendImageToServer(string serverUrl, Texture2D image)
    {
        byte[] imageData = image.EncodeToPNG();
        WWWForm form = new WWWForm();
        form.AddBinaryData("image", imageData, "image.png", "image/png");

        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                UpdateResponseText($"Error with {serverUrl}: {www.error}");
            }
            else
            {
                string serverResponse = www.downloadHandler.text;
                UpdateResponseText($"Response from {serverUrl}: {serverResponse}");

                // Server 1 Response Handling
                if (serverUrl == SERVER_URL_1)
                {
                    StartCoroutine(ProcessServerResponse(serverResponse, image));
                }
                else if (serverUrl == SERVER_URL_2)
                {
                    server2Response = ExtractTextFromResponse(serverResponse);
                    CheckAndSendToGPT();
                }
                else if (serverUrl == SERVER_URL_3)
                {
                    server3Response = ExtractCaptionFromResponse(serverResponse);
                    CheckAndSendToGPT();
                }
            }
        }
    }


    void UpdateResponseText(string message)
    {
        responseText.text += message + "\n";
    }


    void StopCamera()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
            takePictureButton.interactable = false;
        }
    }

    void OnDisable()
    {
        StopCamera();
    }

    private void StartRecording()
    {
        //Debug.Log("Starting recording...");
        isRecording = true;
        wishperText.text = "...";
        wishperText.color = new Color32(0, 42, 96, 255);
        clip = Microphone.Start(null, true, duration, 44100);

        if (clip == null)
        {
            //Debug.LogError("Microphone failed to start recording.");
            return;
        }

        StartCoroutine(StopRecordingAfterDelay(duration));
        StartCoroutine(UpdateVolume());

        recordingObject.SetActive(false);
        RecodeWave.SetActive(true);
        RecodeAnswerWave.SetActive(true);
        Listening.SetActive(true);
    }

    private IEnumerator StopRecordingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StopRecording();
    }
    private void StopRecording()
    {
        //Debug.Log("Stopping recording...");
        isRecording = false;
        Microphone.End(null);
        StartCoroutine(ProcessRecording());

        recordingObject.SetActive(true);
        RecodeWave.SetActive(false);
        RecodeAnswerWave.SetActive(false);
        Listening.SetActive(false);
    }
    private IEnumerator ProcessRecording()
    {
        //Debug.Log("Processing recording...");

        if (clip == null)
        {
            //Debug.LogError("AudioClip is null.");
            yield break;
        }

        byte[] data = SaveWav.Save(fileName, clip);
        yield return StartCoroutine(SendAudioToWhisper(data));
    }
    private string detectedTextGlobal;
    private IEnumerator ProcessServerResponse(string serverResponse, Texture2D image)
    {
        detectedTextGlobal = ExtractTextFromResponse(serverResponse);
        Debug.Log("Extracted Text Detected: " + detectedTextGlobal);

        if (serverResponse.Contains("Text detected") && serverResponse.Contains("Objects detected"))
        {
            Debug.Log("Text and objects detected. Sending to Server 2 and 3.");
            StartCoroutine(SendImageToServer(SERVER_URL_2, image));
            StartCoroutine(SendImageToServer(SERVER_URL_3, image));
            StartRecording();
        }
        else if (serverResponse.Contains("Text detected") && serverResponse.Contains("No objects detected"))
        {
            Debug.Log("Only text detected. Sending to Server 2.");
            StartCoroutine(SendImageToServer(SERVER_URL_2, image));
            StartRecording();
        }
        else if (serverResponse.Contains("No text detected") && serverResponse.Contains("Objects detected"))
        {
            Debug.Log("Only objects detected. Sending to Server 3.");
            StartCoroutine(SendImageToServer(SERVER_URL_3, image));
            StartRecording();
        }
        else if (serverResponse.Contains("No text or objects detected"))
        {
            Debug.Log("No text or objects detected. No further action required.");
            UpdateResponseText("No further action taken: No text or objects detected.");
        }

        yield return null;
    }

    private void CheckAndSendToGPT()
    {
        if (!string.IsNullOrEmpty(server2Response) && !string.IsNullOrEmpty(server3Response))
        {
            Debug.Log("Both Server 2 and Server 3 have responded. Sending combined data to GPT.");
            StartCoroutine(SendToGPT());
        }
        else if (!string.IsNullOrEmpty(server2Response) && string.IsNullOrEmpty(server3Response))
        {
            Debug.Log("Server 2 has responded. Waiting for Server 3.");
        }
        else if (string.IsNullOrEmpty(server2Response) && !string.IsNullOrEmpty(server3Response))
        {
            Debug.Log("Server 3 has responded. Waiting for Server 2.");
        }
    }

    private string ExtractTextFromResponse(string serverResponse)
    {
        var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(serverResponse);
        if (jsonResponse.TryGetValue("text", out string extractedText))
        {
            return extractedText;
        }
        return string.Empty;
    }
    private string ExtractCaptionFromResponse(string serverResponse)
    {
        var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(serverResponse);
        if (jsonResponse.TryGetValue("caption", out string caption))
        {
            return caption;
        }
        return string.Empty;
    }
    [Serializable]
    public class TranscriptionResponse
    {
        public string transcription;
    }
    private string whisperTranscription;
    private IEnumerator SendAudioToWhisper(byte[] audioData)
    {
        Debug.Log("Sending audio to Whisper...");

        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", audioData, "audio.mp3", "audio/mp3");

        string url = "http://localhost:8000/transcribe";
        UnityWebRequest request = UnityWebRequest.Post(url, form);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error sending audio: " + request.error);
            yield break;
        }

        string jsonResponse = request.downloadHandler.text;
        Debug.Log("Transcription response received: " + jsonResponse);

        var transcriptionData = JsonUtility.FromJson<TranscriptionResponse>(jsonResponse);
        if (transcriptionData != null && !string.IsNullOrEmpty(transcriptionData.transcription))
        {
            Debug.Log("Transcription: " + transcriptionData.transcription);

            whisperTranscription = transcriptionData.transcription;

            if (wishperText != null)
            {
                wishperText.text = transcriptionData.transcription;
            }

            StartCoroutine(SendToGPT());
        }
        else
        {
            Debug.LogError("Failed to retrieve transcription from response.");
        }
    }
    private async Task<string> GetLlamaResponse(string userInput)
    {
        string contentToSend = $"I process book images using Tesseract for OCR text and the BLIP model for image-text retrieval. I'll provide outputs from both models and ask queries based on them. Please use the outputs to answer my questions.Give the answer only";
        var messages = new
        {
            model = "llama3.2:1b",
            prompt = $"{contentToSend}\n{userInput}"
        };

        string url = "http://localhost:11434/v1/completions";

        try
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messages));
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("Sending request to Ollama with payload: " + JsonConvert.SerializeObject(messages));

            var tcs = new TaskCompletionSource<UnityWebRequest.Result>();
            request.SendWebRequest().completed += asyncOp => tcs.SetResult(request.result);

            await tcs.Task;

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Connection error: " + request.error);
                return null;
            }

            string responseText = request.downloadHandler.text;
            Debug.Log("Response from Ollama: " + responseText);

            var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseText);
            if (jsonResponse.TryGetValue("text", out object llamaResponse))
            {
                return llamaResponse.ToString().Trim();
            }
            else
            {
                Debug.LogWarning("No 'text' field found in Ollama response.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Exception occurred: " + e.Message);
        }

        return null;
    }


    private IEnumerator SendToGPT()
    {
        Debug.Log("Sending combined responses from Server 2 and Server 3 along with transcription to GPT...");

        string combinedInput = $"BLIP Model Output = {server3Response}\nOCR Model Output = {server2Response}\nQuerie = {whisperTranscription}";
        Debug.Log("Combined input to GPT: " + combinedInput);

        Task<string> gptResponseTask = GetLlamaResponse(combinedInput);
        while (!gptResponseTask.IsCompleted)
        {
            yield return null;
        }

        string gptResponse = gptResponseTask.Result;
        if (!string.IsNullOrEmpty(gptResponse))
        {
            Debug.Log("GPT response: " + gptResponse);
            responseText.text = gptResponse;
            yield return StartCoroutine(RequestTTS(gptResponse));
        }
    }

    private IEnumerator RequestTTS(string text)
    {
        string url = "http://localhost:7000/synthesize";
        WWWForm form = new WWWForm();
        form.AddField("text", text);

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string audioPath = Path.Combine(Application.dataPath, "Audio", "output.mp3");
            File.WriteAllBytes(audioPath, request.downloadHandler.data);
            StartCoroutine(LoadAudioClip(audioPath));
        }
        else
        {
            Debug.LogError("Error with TTS request: " + request.error);
        }
    }
}
