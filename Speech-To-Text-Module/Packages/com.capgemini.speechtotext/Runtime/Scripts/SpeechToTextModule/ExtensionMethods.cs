using System.IO;
using UnityEngine;

namespace SpeechToTextUnity
{
    public static class ExtensionMethods
    {
        public static void RemoveProcessedAudioFile()
        {
            string folderPath = Path.Combine(Application.dataPath, AudioConvertor.AudioProcessingString);

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
}