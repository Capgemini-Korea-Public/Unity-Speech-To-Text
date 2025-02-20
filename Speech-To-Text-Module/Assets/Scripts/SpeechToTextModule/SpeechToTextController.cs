using System;
using System.IO;
using UnityEngine;
using SpeechToTextUnity;

public class SpeechToTextController : MonoBehaviour
{
    [field: Header("Selected Model")]
    [field: SerializeField] public ESTTModelType STTModelType { get; private set; }

    [field: Header("File Info")]
    [field: SerializeField] public string FilePath { get; private set; }
    [field: Header("Converted Text")]
    [field: SerializeField] public string ConvertedText { get; private set; }

    [field: Header("Set Audio Maximum Length")]
    [field: SerializeField, Range(10f, 30)] public int MaximumAudioLength = 20;

    private void Start()
    {
        SpeechToTextUnityModule.InitializeSentisModel();
        InitFolder("AudioProcessings");
        InitFolder("Plugins");
    }

    private void OnDestroy()
    {
        SpeechToTextUnityModule.OnDestroy();
    }

    public void FileSelect()
    {
        FilePath = FileSelector.FileSelect();
    }

    public async void Convert()
    {
        ConvertedText = await AudioConvertor.ConvertAudioToText(FilePath, STTModelType, MaximumAudioLength);
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

    public void SetModelType(ESTTModelType sttModel)
    {
        STTModelType = sttModel;
    }

    public void SetFilePath(string filePath)
    {
        FilePath = filePath;
    }

    public void SetConvertedText(string text)
    {
        ConvertedText = text;        
    }
}
