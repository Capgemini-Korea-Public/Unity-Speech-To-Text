using UnityEngine;
using UnityEditor;
using System.IO;

public class FileSelector : MonoBehaviour
{
    // file select in directory 
    public void FileSelect()
    {
        // Title, Directory, File Type 
        string filePath = EditorUtility.OpenFilePanel("Select Audio File", "", "");

        // Init UI
        STTManager.Instance.Initialize();
        UIManager.Instance.UpdateFileName(STTManager.Instance.FilePath);
        UIManager.Instance.UpdateOutputText(STTManager.Instance.ConvertedText);

        if (!string.IsNullOrEmpty(filePath))
        {
            STTManager.Instance.SetFilePath(filePath);
            UIManager.Instance.UpdateFileName(Path.GetFileName(filePath));
        }
        else
        {
            Debug.LogWarning("Invalid File");
        }         
    }
}
