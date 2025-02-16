using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ExtensionMethods
{
    // whisper support extension
    public static readonly HashSet<string> whisperExtensions = new HashSet<string>
    {
        ".mp3", ".mp4", ".mpeg", ".mpga", ".m4a", ".wav", ".webm", ".flac",  ".oga", ".ogg"
    };

    public static void RemoveProcessedAudioFile()
    {
        string folderPath = Path.Combine(Application.dataPath, "AudioProcessings");

        if (Directory.Exists(folderPath))
        {
            foreach (string file in Directory.GetFiles(folderPath))
            {
                File.Delete(file);
            }
            Debug.Log("All files in 'AudioProcessings' have been deleted.");
        }
        else
        {
            Debug.LogWarning("'AudioProcessings' folder does not exist.");
        }
    }
}