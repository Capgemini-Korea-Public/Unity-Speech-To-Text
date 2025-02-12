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
        UIManager.Instance.UpdateFileName(FilePath);
    }

    public async void AskWhisper()
    {
        if (!File.Exists(FilePath))
        {
            Debug.LogError("File Not Exist: " + FilePath);
            return;
        }

        if(!IsValidAudioFormat(FilePath) )
        {
            Debug.LogError("Invalid File Extension: " + Path.GetExtension(FilePath));
            return;
        }

        var req = new CreateAudioTranscriptionsRequest
        {
            File = FilePath,
            Model = "whisper-1",
            Language = "ko",
        };

        var res = await openAI.CreateAudioTranscription(req);
        Assert.NotNull(res); // null 체크
        
        Debug.Log(res);
        ConvertedText = res.Text;
        UIManager.Instance.UpdateOutputText(ConvertedText);
    }

    #region Audio Verification
    private bool IsValidAudioFormat(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        Debug.Log("오디오 파일 형식 검증");
        return ExtensionMethods.whisperExtensions.Contains(extension);
    }

    #endregion
}
