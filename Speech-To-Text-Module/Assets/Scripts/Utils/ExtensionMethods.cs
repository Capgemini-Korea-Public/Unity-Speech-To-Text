using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods
{
    // whisper support extension
    public static readonly HashSet<string> whisperExtensions = new HashSet<string>
    {
        ".mp3", ".mp4", ".mpeg", ".mpga", ".m4a", ".wav", ".webm", ".flac",  ".oga", ".ogg"
    };

    private static async UniTask UnloadUnusedResourcesAsync()
    {
        await Resources.UnloadUnusedAssets();
        Debug.Log("Unused resources have been unloaded.");
    }
}