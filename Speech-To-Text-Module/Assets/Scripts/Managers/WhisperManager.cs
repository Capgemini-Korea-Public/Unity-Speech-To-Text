using UnityEngine;
using OpenAI;
using System.IO;
using NUnit.Framework;
using System.Threading.Tasks;

public class WhisperManager : Singleton<WhisperManager>
{
    [Header("Output")]
    [SerializeField] private string outputString = "";

    private OpenAIApi openAI = new OpenAIApi();

    [ContextMenu ("AskWhisper")]
    public async Task<string> AskWhisper(AudioClip audioClip)
    {
        if (STTManager.Instance.IsTranscribing()) return null; // avoid duplicate execution 
        STTManager.Instance.SetTranscribeStatus(true);

        string filePath = Path.Combine(Application.dataPath, "AudioProcessings", audioClip.name);
        var req = new CreateAudioTranscriptionsRequest
        {
            File = filePath,
            Model = "whisper-1", 
            Language = "en", // target language
        };

        var res = await openAI.CreateAudioTranscription(req);

        STTManager.Instance.SetTranscribeStatus(false);
        Assert.NotNull(res); //response null check
        
        Debug.Log(res);
        outputString = res.Text;

        ExtensionMethods.RemoveProcessedAudioFile();
        STTManager.Instance.SetConvertedText(outputString);
        return outputString;
    }
}
