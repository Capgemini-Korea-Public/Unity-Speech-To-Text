using UnityEngine;
using OpenAI;
using NUnit.Framework;
using System.IO;

public class WhisperManager : Singleton<WhisperManager>
{
    [field: Header("File Info")]
    [field: SerializeField] public string FilePath { get; private set; }
    [field: Header("Converted Text")]
    [field: SerializeField] public string ConvertedText { get; private set; }

    private OpenAIApi openAI = new OpenAIApi();

    public void SetFilePath(string filePath)
    {
        FilePath = filePath;
        UIManager.Instance.UpdateFileName();
    }

    public async void AskWhisper()
    {
        if (!File.Exists(FilePath))
        {
            Debug.LogError("File not Exist: " + FilePath);
            return;
        }

        var req = new CreateAudioTranscriptionsRequest
        {
            File = FilePath,
            Model = "whisper-1",
            Language = "ko"
        };

        var res = await openAI.CreateAudioTranscription(req);
        Assert.NotNull(res);

        ConvertedText = res.Text;
        UIManager.Instance.UpdateOutputText();
    }
}
