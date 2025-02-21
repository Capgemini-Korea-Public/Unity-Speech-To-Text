using System;
using System.IO;
using UnityEngine;
using SpeechToTextUnity;

public class SpeechToTextController : MonoBehaviour
{
    private static SpeechToTextController instance;
    public static SpeechToTextController Instance => instance;

    [field: Header("Selected Model")]
    [field: SerializeField] public ESTTModelType STTModelType { get; private set; }

    [field: Header("File Info")]
    [field: SerializeField] public string FilePath { get; private set; }
    [field: Header("Converted Text")]
    [field: SerializeField] public string ConvertedText { get; private set; }

    [field: Header("Set Audio Maximum Length")]
    [field: SerializeField, Range(10f, 30)] public int MaximumAudioLength = 25;

    public event Action<string> OnFileSelected;
    public event Action OnConvertBtnClicked;
    public event Action<string> OnOutputTextChanged;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SpeechToTextUnityModule.InitializeSentisModel();
        InitFolder("AudioProcessings");
        InitFolder("Resources");
        InitFolder("Plugins");
    }

    private void OnDestroy()
    {
        SpeechToTextUnityModule.OnDestroy();
    }

    public void FileSelect()
    {
        FilePath = FileSelector.FileSelect();
        OnFileSelected?.Invoke(FilePath);
    }

    public async void Convert()
    {
        OnConvertBtnClicked?.Invoke();
        ConvertedText = await AudioConvertor.ConvertAudioToText(FilePath, STTModelType, MaximumAudioLength);
        OnOutputTextChanged?.Invoke(ConvertedText);
    }

    private void InitFolder(string folderName)
    {
        string folderPath = Application.dataPath + $"/{folderName}";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
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
