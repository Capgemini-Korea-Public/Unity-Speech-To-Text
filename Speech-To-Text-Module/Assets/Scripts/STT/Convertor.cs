using System.Diagnostics;
using System.IO;
using UnityEngine;
using Cysharp.Threading.Tasks;

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

        // Processed Audio Convert to Text
        switch (STTManager.Instance.STTModel)
        {
            case ESTTType.OpenAIWhisper:
                await WhisperManager.Instance.AskWhisper();
                break;
            case ESTTType.SentisWhisper:
                await SentisWhisperManager.Instance.AskSentisWhisper();
                break ;
        }
    }

    #region Audio Verification
    private bool IsValidAudioFormat(string filePath)
    {
        UnityEngine.Debug.Log("Audio Format Verification");

        string extension = Path.GetExtension(filePath);
        return ExtensionMethods.whisperExtensions.Contains(extension);
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

    #endregion
}
