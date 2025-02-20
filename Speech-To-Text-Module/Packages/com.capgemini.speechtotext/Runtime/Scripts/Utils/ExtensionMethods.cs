using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ExtensionMethods
{
    // whisper support extension
    public static readonly HashSet<string> whisperExtensions = new HashSet<string>
    {
        ".mp3", ".mp4", ".wav", ".ogg", ".mpeg",//".m4a",  ".mpga", ".webm", ".flac", ".oga" <= not supported in AudioType
    };

    public static void RemoveProcessedAudioFile()
    {
        string folderPath = Path.Combine(Application.dataPath, "AudioProcessings");
         
        if (Directory.Exists(folderPath))
        {
            foreach (string file in Directory.GetFiles(folderPath))
            {
                if (Path.GetFileNameWithoutExtension(file) == "FolderDesc") { continue; }
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