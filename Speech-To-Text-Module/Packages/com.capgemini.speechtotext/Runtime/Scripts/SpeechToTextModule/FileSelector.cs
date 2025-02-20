using UnityEngine;
using UnityEditor;

namespace SpeechToTextUnity
{
    public static class FileSelector
    {
        public static string FileSelect()
        {
            string filePath = EditorUtility.OpenFilePanel("Select Audio File", "", "");
            if (!string.IsNullOrEmpty(filePath))
            {
                return filePath;
            }
            else
            {
                Debug.LogWarning("Invalid File Path");
                return null;
            }
        }
    }
}

