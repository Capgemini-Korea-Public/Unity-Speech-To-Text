using UnityEngine;

public class STTManager : Singleton<STTManager>
{
    [field: Header("File Info")]
    [field: SerializeField] public string FilePath { get; private set; }
    [field: Header("Converted Text")]
    [field: SerializeField] public string ConvertedText { get; private set; }

    public void SetFilePath(string filePath)
    {
        FilePath = filePath;
        UIManager.Instance.UpdateFileName(FilePath);
    }

    public void SetConvertedText(string text)
    {
        ConvertedText = text;
        UIManager.Instance.UpdateOutputText(ConvertedText);
    }
}
