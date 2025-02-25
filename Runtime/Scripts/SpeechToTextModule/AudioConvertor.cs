using System.Diagnostics;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Text;


namespace SpeechToTextUnity
{
    public static class AudioConvertor
    {
        public const string AudioProcessingString = "AudioProcessings";
        public const string PluginString = "Plugins";
        private static readonly HashSet<string> whisperExtensions = new HashSet<string> { ".mp3", ".mp4", ".wav", ".ogg", ".mpeg" }; // whisper support extension , ".m4a",  ".mpga", ".webm", ".flac", ".oga" <= not supported in AudioType 

        public static async Task<string> ConvertAudioToText(string filePath, ESTTModelType modelType, int maxConvertedAudioLength)
        {
            float curTime = Time.time;
            string outputString = "";

            // check file exist in given path
            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogWarning("File Not Exist: " + filePath);
                return null;
            }

            // check file format is supported by Whisper
            if (!IsValidAudioFormat(filePath))
            {
                UnityEngine.Debug.LogWarning("Invalid File Extension: " + Path.GetExtension(filePath));
                return null;
            }

            string _filePath = filePath;

            // if given file format is .mp4, convert file format
            if (Path.GetExtension(filePath) == ".mp4")
            {
                string wavFilePath = await ConvertToWav(filePath);
                if (wavFilePath != null)
                {
                    _filePath = wavFilePath;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Fail Convert .mp4 File to .wav");
                    return null;
                }
            }

            // Remove Noise, Convert sample rate to 16kHz, Change to mono channel
            string processedFilePath = await AudioProcessing(filePath);
            if (processedFilePath != null)
            {
                _filePath = processedFilePath;
            }
            else
            {
                UnityEngine.Debug.LogWarning("Fail to processing Audio");
                return null;
            }

            AudioClip curAudioInput = await LoadAudio(_filePath);

            // check audio input length over maximum seconds
            if (IsAudioOverMaximumSeconds(curAudioInput, maxConvertedAudioLength))
            {
                UnityEngine.Debug.Log("Audio length over maximum seconds");
                outputString = await SplitAudio(curAudioInput, outputString, maxConvertedAudioLength, modelType);
            }
            else
            {
                outputString = await ConvertByModel(modelType, curAudioInput);
            }

            UnityEngine.Debug.Log($"Speech To Text Module Duration : {Time.time - curTime}");
            return outputString;
        }

        private static bool IsValidAudioFormat(string filePath)
        {
            UnityEngine.Debug.Log("Audio Format Verification");

            string extension = Path.GetExtension(filePath);
            return whisperExtensions.Contains(extension);
        }

        private static bool IsAudioOverMaximumSeconds(AudioClip audioClip, int maxConvertedAudioLength)
        {
            UnityEngine.Debug.Log("Check Audio Length");

            if (audioClip.length > maxConvertedAudioLength)
                return true;
            else
                return false;
        }

        private static async Task<bool> ExecuteFFmpegProcess(string argument, string outputPath)
        {
            // Window - Download ffmpeg and locate the ffmpeg.exe file, then place it in the Assets/Plugins folder.
            string ffmpegPath = Path.Combine(Application.dataPath, "Plugins/ffmpeg.exe");
            // Mac - Download ffmpeg and locate the folder path where the ffmpeg.dll file is installed, then modify the code below.
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                ffmpegPath = "/usr/local/bin/ffmpeg";
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

            try
            {
                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    string errorLog = await process.StandardError.ReadToEndAsync();

                    while (!process.HasExited)
                    {
                        await Task.Delay(100);
                    }

                    if (!string.IsNullOrEmpty(errorLog))
                    {
                        // UnityEngine.Debug.LogWarning($"FFmpeg Error: {errorLog}");
                    }

                    if (File.Exists(outputPath))
                    {
                        UnityEngine.Debug.Log($"Execute Success: {outputPath}");
                        return true;
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"Execute Failed: {outputPath} file not found in this path");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"FFmpeg Execution Failed: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        private static async Task<string> ConvertToWav(string filePath)
        {
            UnityEngine.Debug.Log(".wav Converted");

            string directoryPath = Path.Combine(Application.dataPath, AudioProcessingString);
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

        private static async Task<string> AudioProcessing(string filePath)
        {
            UnityEngine.Debug.Log("Reduce Noise");

            string directoryPath = Path.Combine(Application.dataPath, AudioProcessingString);
            string baseFileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            string outputPath = Path.Combine(directoryPath, baseFileName + Path.GetExtension(filePath));

            int count = 1;
            while (File.Exists(outputPath))
            {
                outputPath = Path.Combine(directoryPath, $"{baseFileName}{count++}{extension}");
            }

            // reduce noise & sample rate 16 & mono
            string arguments = $"-i \"{filePath}\" -af \"afftdn=nf=-25\" -ar 16000 -ac 1  \"{outputPath}\"";

            if (await ExecuteFFmpegProcess(arguments, outputPath))
                return outputPath;
            else
                return null;
        }

        private static async Task<string> SplitAudio(AudioClip audioClip, string outputString, int maxConvertedAudioLength, ESTTModelType modelType)
        {
            float audioLength = audioClip.length;
            float splitDuration = maxConvertedAudioLength;
            float curTime = 0f;
            string splitAudioOutputString = outputString;

            while (curTime < audioLength)
            {
                float endTime = Mathf.Min(curTime + splitDuration, audioLength);
                AudioClip splitAudio = CutAudioClip(audioClip, curTime, endTime);
                if (splitAudio != null && splitAudio.length >= 0.1f) // if split audio is too short or split process has error, splitAudio return null
                    splitAudioOutputString += await ConvertByModel(modelType, splitAudio);
                curTime = endTime;
            }

            return splitAudioOutputString;
        }

        private static AudioClip CutAudioClip(AudioClip clip, float startTime, float endTime)
        {
            int startSample = Mathf.FloorToInt(startTime * clip.frequency);
            int endSample = Mathf.FloorToInt(endTime * clip.frequency);

            int newClipLength = Mathf.Min(endSample - startSample, clip.samples - startSample);
            if (newClipLength <= 1) return null;

            float[] samples = new float[newClipLength * clip.channels];
            clip.GetData(samples, startSample);

            AudioClip newClip = AudioClip.Create(clip.name, newClipLength, clip.channels, clip.frequency, false);
            newClip.SetData(samples, 0);

            UnityEngine.Debug.Log($"New Clip: {startTime} ~ {endTime}, Length: {newClip.length}, Name: {newClip.name} ");

            string outputPath = Path.Combine(Application.dataPath, AudioProcessingString, newClip.name);
            SaveAudioClip(newClip, outputPath);

            return newClip;
        }

        private static void SaveAudioClip(AudioClip clip, string path)
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            byte[] byteArray = ConvertAudioClipToWav(clip, samples);

            File.WriteAllBytes(path, byteArray);
            UnityEngine.Debug.Log($"AudioClip save in this path : {path}");
        }

        // AudioClip -> .wav 
        private static byte[] ConvertAudioClipToWav(AudioClip clip, float[] samples)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    int sampleRate = clip.frequency;
                    int channels = clip.channels;
                    int sampleCount = samples.Length;

                    // .wav header
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(36 + sampleCount * 2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                    writer.Write(16);
                    writer.Write((ushort)1);
                    writer.Write((ushort)channels);
                    writer.Write(sampleRate);
                    writer.Write(sampleRate * channels * 2);
                    writer.Write((ushort)(channels * 2));
                    writer.Write((ushort)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                    writer.Write(sampleCount * 2);

                    foreach (float sample in samples)
                    {
                        short intSample = (short)(sample * 32767);
                        writer.Write(intSample);
                    }
                }
                return stream.ToArray();
            }

        }

        public static async Task<AudioClip> LoadAudio(string filePath)
        {
            string LoadedFilePath = "file://" + filePath;
            string fileExtension = Path.GetExtension(LoadedFilePath).ToLower();

            AudioType audioType = GetAudioType(fileExtension);
            if (audioType == AudioType.UNKNOWN)
            {
                UnityEngine.Debug.LogWarning("Unsupported AudioType: " + fileExtension);
                return null;
            }

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(LoadedFilePath, audioType))
            {
                await www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    UnityEngine.Debug.Log("Successfully Audio Load ");
                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                    audioClip.name = Path.GetFileName(LoadedFilePath);
                    return audioClip;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Failed Audio Load: " + www.error);
                    return null;
                }
            }
        }

        private static AudioType GetAudioType(string extension)
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

        private static async Task<string> ConvertByModel(ESTTModelType eModelType, AudioClip audioClip)
        {
            // Processed Audio Convert to Text
            switch (eModelType)
            {
                case ESTTModelType.OpenAIWhisper:
                    return await SpeechToTextUnityModule.SpeechToTextFromAPI(audioClip);
                case ESTTModelType.SentisWhisper:
                    return await SpeechToTextUnityModule.SpeechToTextFromSentis(audioClip);
                default:
                    return null;
            }
        }

        public static void RemoveProcessedAudioFile()
        {
            string folderPath = Path.Combine(Application.dataPath, AudioProcessingString);

            if (Directory.Exists(folderPath))
            {
                foreach (string file in Directory.GetFiles(folderPath))
                {
                    File.Delete(file);
                }
                UnityEngine.Debug.Log("All files in 'AudioProcessings' have been deleted.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("'AudioProcessings' folder does not exist.");
            }
        }
    }
}