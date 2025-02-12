using UnityEngine;
using OpenAI;
using NUnit.Framework;

public class WhisperManager : Singleton<WhisperManager>
{
    private OpenAIApi openAI = new OpenAIApi();

    public async void AskWhisper()
    {
        var req = new CreateAudioTranscriptionsRequest
        {
            File = STTManager.Instance.FilePath,
            Model = "whisper-1",
            Language = "ko",
        };

        var res = await openAI.CreateAudioTranscription(req);
        Assert.NotNull(res); // null 체크
        
        Debug.Log(res);
        STTManager.Instance.SetConvertedText( res.Text);
    }
}
