using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

public class Convertor : MonoBehaviour
{
    public void Convert()
    {
        ConvertAudioToText();
    }

    private async UniTask ConvertAudioToText()
    {
        string filePath = STTManager.Instance.FilePath;

        // check file exist in given path
        if (!File.Exists(filePath))
        {
            UnityEngine.Debug.LogError("File Not Exist: " + filePath);
            return;
        }

        // check file format is supported by Whisper
        if (!IsValidAudioFormat(filePath))
        {
            UnityEngine.Debug.LogError("Invalid File Extension: " + Path.GetExtension(filePath));
            return;
        }

        // if given file format is .mp4, convert file format
        if (Path.GetExtension(filePath) == ".mp4")
        {
            string wavFilePath = await ConvertToWav(filePath);
            if (wavFilePath != null)
            {
                STTManager.Instance.SetFilePath(wavFilePath);
            }
            else
            {
                UnityEngine.Debug.LogError("Fail Convert .mp4 File to .wav");
                return;
            }
        }

        // Remove Noise
        string noiseRemovedFilePath = await ReduceNoise(filePath);
        if (noiseRemovedFilePath != null)
        {
            STTManager.Instance.SetFilePath(noiseRemovedFilePath);
        }
        else
        {
            UnityEngine.Debug.LogError("Fail to Remove Noise");
        }

        // check audio input length over 30 seconds
        AudioClip curAudioInput = await LoadAudio();
        string outputString = "";

        //if (IsAudioOverMaximumSeconds(curAudioInput))
        //{
        //    UnityEngine.Debug.Log("Audio Length Over 30 seconds");
        //    List<AudioClip> audioClips = new List<AudioClip>();
        //    SplitAudio(curAudioInput, audioClips);

        //    foreach(var audioClip in audioClips)
        //    {
        //        outputString += await ConvertByModel(audioClip);
        //    }
        //}
        //else
        //{
        //    outputString += await ConvertByModel(curAudioInput);
        //}

        outputString += await ConvertByModel(curAudioInput);
        UIManager.Instance.UpdateOutputText(outputString);
    }

    #region Audio Verification
    private bool IsValidAudioFormat(string filePath)
    {
        UnityEngine.Debug.Log("Audio Format Verification");

        string extension = Path.GetExtension(filePath);
        return ExtensionMethods.whisperExtensions.Contains(extension);
    }

    private bool IsAudioOverMaximumSeconds(AudioClip audioClip)
    {
        UnityEngine.Debug.Log("Check Audio Length");

        if (audioClip.length > STTManager.Instance.MaximumAudioLength)
            return true;
        else
            return false;
    }
    #endregion

    #region Audio Processing
    private async UniTask<bool> ExecuteFFmpegProcess(string argument, string outputPath)
    {
        string ffmpegPath = Path.Combine(Application.dataPath, "Plugins/ffmpeg/bin/ffmpeg.exe");
        // mac os
        if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
        {
            ffmpegPath = "/usr/local/bin/ffmpeg";  // Mac
        }

        // ProcessStartInfo
        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = ffmpegPath, // execute program
            Arguments = argument, // argument
            RedirectStandardOutput = true, // Capture the external process's output output in C#
            RedirectStandardError = true, // Capture the external process's error output in C#
            UseShellExecute = false, // Execute without using the shell
            CreateNoWindow = true // Run without creating a window
        };

        Process process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();
        process.WaitForExit(); 

        if (File.Exists(outputPath))
        {
            UnityEngine.Debug.Log($"Execute Success: {outputPath}");
            return true;
        }
        else
        {
            UnityEngine.Debug.LogError("Execute Failed");
            return false;
        }
    }

    private async UniTask<string> ConvertToWav(string filePath)
    {
        UnityEngine.Debug.Log(".wav Converted");

        string directoryPath = Path.Combine(Application.dataPath, "AudioProcessings");
        string baseFileName = Path.GetFileNameWithoutExtension(filePath);
        string outputPath = Path.Combine(directoryPath, baseFileName + ".wav");

        int count = 1;
        while (File.Exists(outputPath))
        {
            string newFileName = baseFileName + count.ToString(); 
            outputPath = Path.Combine(directoryPath, newFileName + ".wav");
            count++;  
        }

        // ffmpeg arguments, mp4 to wav, whisper - 16000 sr, mono -ac 1
        string arguments = $"-i \"{filePath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 \"{outputPath}\"";

        if (await ExecuteFFmpegProcess(arguments, outputPath))
            return outputPath;
        else
            return null;
    }

    private async UniTask<string> ReduceNoise(string filePath)
    {
        UnityEngine.Debug.Log("Reduce Noise");

        string directoryPath = Path.Combine(Application.dataPath, "AudioProcessings");
        string baseFileName = Path.GetFileNameWithoutExtension(filePath);
        string outputPath = Path.Combine(directoryPath, baseFileName + Path.GetExtension(filePath));

        int count = 1;
        while (File.Exists(outputPath))
        {
            string newFileName = baseFileName + count.ToString();
            outputPath = Path.Combine(directoryPath, newFileName + Path.GetExtension(filePath));
            count++;
        }

        // reduce noise & sample rate 16 & mono
        string arguments = $"-i \"{filePath}\" -af \"afftdn=nf=-25\" -ar 16000 -ac 1  \"{outputPath}\"";
        

        if (await ExecuteFFmpegProcess(arguments, outputPath))
            return outputPath;
        else
            return null;
    }

    private void SplitAudio(AudioClip audioClip, List<AudioClip> audioClips)
    {        
        float audioLength = audioClip.length;
        float splitDuration = STTManager.Instance.MaximumAudioLength;
        float curTime = 0f;

        while (Mathf.Abs(audioLength - curTime) >= splitDuration) // while remain audio data over 30 seconds
        {
            float endTime = Mathf.Min(curTime + splitDuration, audioLength);
            audioClips.Add(CutAudioClip(audioClip, curTime, endTime));
            curTime = endTime;
        }
    }

    private AudioClip CutAudioClip(AudioClip clip, float startTime, float endTime)
    {
        int startSample = Mathf.FloorToInt(startTime * clip.frequency);
        int endSample = Mathf.FloorToInt(endTime * clip.frequency);

        int newClipLength = Mathf.Min(endSample - startSample, STTManager.Instance.MaximumAudioLength);
            
        float[] samples = new float[newClipLength];
        clip.GetData(samples, startSample);

        AudioClip newClip = AudioClip.Create("newClip", newClipLength, clip.samples, clip.frequency, false);
        newClip.SetData(samples, 0);

        return newClip;
    }

    public async UniTask<AudioClip> LoadAudio()
    {
        string filePath = "file://" + STTManager.Instance.FilePath;
        string fileExtension = Path.GetExtension(filePath).ToLower();

        AudioType audioType = GetAudioType(fileExtension);
        if (audioType == AudioType.UNKNOWN)
        {
            UnityEngine.Debug.LogError("Unsupported AudioType: " + fileExtension);
            return null;
        }

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(filePath, audioType))
        {
            await www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.Log("Successfully Audio Load ");
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                audioClip.name = Path.GetFileName(filePath);
                return audioClip;
            }
            else
            {
                UnityEngine.Debug.LogError("Failed Audio Load: " + www.error);
                return null;
            }
        }
    }

    private AudioType GetAudioType(string extension)
    {
        switch (extension)
        {
            case ".mp3":
            case ".mpeg":
                return AudioType.MPEG;
            case ".wav":
                return AudioType.WAV;
            case ".ogg":
                return AudioType.OGGVORBIS;
            default:
                return AudioType.UNKNOWN;
        }
    }
    #endregion

    #region Convert
    private async UniTask<string> ConvertByModel(AudioClip audioClip)
    {
        // Processed Audio Convert to Text
        switch (STTManager.Instance.STTModel)
        {
            case ESTTType.OpenAIWhisper:
                return await WhisperManager.Instance.AskWhisper(audioClip);
            case ESTTType.SentisWhisper:
                return await SentisWhisperManager.Instance.AskSentisWhisper(audioClip);
            default:
                return null;
        }
    }
    #endregion
}
