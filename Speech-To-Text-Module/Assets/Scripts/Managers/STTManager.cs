using System;
using System.IO;
using UnityEngine;

public class STTManager : Singleton<STTManager>
{
    [field: Header("File Info")]
    [field: SerializeField] public string FilePath { get; private set; }
    [field: Header("Converted Text")]
    [field: SerializeField] public string ConvertedText { get; private set; }
    [field: Header("Selected Model")]
    [field: SerializeField] public ESTTType STTModel { get; private set; }
    [field: Header("Set Audio Maximum Length")]
    [field: SerializeField, Range(10f, 30)] public int MaximumAudioLength = 20;

    [field: SerializeField] public bool IsTranscribe = false;

    public readonly string AudioProcessings = "AudioProcessings";
    public readonly string Plugins = "Plugins";

    private void Start()
    {
        InitFolder(AudioProcessings);
        InitFolder(Plugins);
    }

    private void InitFolder(string folderName)
    {
        string folderPath = Application.dataPath + $"/{folderName}";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        else
        {
            Debug.Log("Folder Already Exist in " + folderPath);
        }
    }

    public void Reset()
    {
        SetConvertedText("");
        SetFilePath("");
    }

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
