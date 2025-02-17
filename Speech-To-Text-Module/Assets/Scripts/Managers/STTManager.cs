using UnityEngine;

public class STTManager : Singleton<STTManager>
{
    [field: Header("File Info")]
    [field: SerializeField] public string FilePath { get; private set; }
    [field: Header("Converted Text")]
    [field: SerializeField] public string ConvertedText { get; private set; }
    [field: Header("Selected Model")]
    [field: SerializeField] public ESTTType STTModel { get; private set; }

    [field: SerializeField] public bool IsTranscribe = false;
    [field: SerializeField, Range(0f, 30)] public int MaximumAudioLength = 10;

    public void SetModelType(ESTTType sttModel)
    {
        STTModel = sttModel;
    }

    public void SetFilePath(string filePath)
    {
        FilePath = filePath;
    }

    public void SetConvertedText(string text)
    {
        ConvertedText = text;        
    }

    public void SetTranscribeStatus(bool isTranscribe)
    {
        IsTranscribe = isTranscribe;
    }

    public bool IsTranscribing()
    {
        return IsTranscribe;
    }

}
