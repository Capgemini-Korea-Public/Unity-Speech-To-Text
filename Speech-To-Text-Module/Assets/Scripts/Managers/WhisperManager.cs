using UnityEngine;
using OpenAI;
using NUnit.Framework;
using Cysharp.Threading.Tasks;

public class WhisperManager : Singleton<WhisperManager>
{
    private OpenAIApi openAI = new OpenAIApi();
    private bool isConverting = false;

    public async UniTask AskWhisper()
    {
        if (isConverting) return; // avoid duplicate execution 
        isConverting = true;

        var req = new CreateAudioTranscriptionsRequest
        {
            File = STTManager.Instance.FilePath,
            Model = "whisper-1", 
            Language = "en", // target language
        };

        var res = await openAI.CreateAudioTranscription(req);

        isConverting = false;
        Assert.NotNull(res); //response null check
        
        Debug.Log(res);
        STTManager.Instance.SetConvertedText( res.Text);
    }
}
