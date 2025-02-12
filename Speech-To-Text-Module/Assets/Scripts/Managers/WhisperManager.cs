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
        if (isConverting) return; // 중복 실행 방지
        isConverting = true;

        var req = new CreateAudioTranscriptionsRequest
        {
            File = STTManager.Instance.FilePath,
            Model = "whisper-1",
            Language = "ko",
        };

        var res = await openAI.CreateAudioTranscription(req);

        isConverting = false;
        Assert.NotNull(res); // null 체크
        
        Debug.Log(res);
        STTManager.Instance.SetConvertedText( res.Text);
    }
}
