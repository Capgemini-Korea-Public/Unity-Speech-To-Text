using UnityEngine;
using UnityEditor;

public class FileSelector : MonoBehaviour
{
    // Directory에서 파일 선택
    public void FileSelect()
    {
        // Title, Directory, File Type 순서
        string filePath = EditorUtility.OpenFilePanel("Select Audio File", "", "");

        if(!string.IsNullOrEmpty(filePath))
            STTManager.Instance.SetFilePath(filePath);
        else
            Debug.LogError("Invalid File");
    }
}
