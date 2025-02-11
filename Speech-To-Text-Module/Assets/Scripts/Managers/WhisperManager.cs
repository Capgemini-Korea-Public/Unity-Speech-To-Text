using UnityEngine;
using OpenAI;
using System;
using NUnit.Framework;
using System.IO;

public class WhisperManager : MonoBehaviour
{
    private OpenAIApi openAI = new OpenAIApi();

    public async void AskWhisper()
    {
        string filePath = Environment.CurrentDirectory + "/Assets/Datas/hi.m4a";

        if (!File.Exists(filePath))
        {
            Debug.LogError("File not Exist: " + filePath);
            return;
        }

        var req = new CreateAudioTranscriptionsRequest
        {
            File = filePath,
            Model = "whisper-1",
            Language = "ko"
        };

        var res = await openAI.CreateAudioTranscription(req);
        Assert.NotNull(res);

        Debug.Log(res.Text);
    }
}
