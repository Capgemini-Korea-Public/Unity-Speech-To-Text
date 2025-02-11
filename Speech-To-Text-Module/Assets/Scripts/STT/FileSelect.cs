using UnityEngine;
using UnityEditor;

public class FileSelect : MonoBehaviour
{
    // Directory에서 파일 선택
    public void Select()
    {
        // Title, Directory ,File Type 순으로 적기
        string filePath = EditorUtility.OpenFilePanel("Select Audio File", "", "");

        // TODO :: 음성 파일인지 체크
        if(!string.IsNullOrEmpty(filePath))
            WhisperManager.Instance.SetFilePath(filePath);
        else
            Debug.LogError("File not Exist");
    }
}
