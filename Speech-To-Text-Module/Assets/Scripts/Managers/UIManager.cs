using System.IO;
using TMPro;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [field : Header ("Text")]
    [SerializeField] private TextMeshProUGUI TXT_fileName;
    [SerializeField] private TextMeshProUGUI TXT_OutputText;

    public void UpdateFileName()
    {
        string fileName = Path.GetFileName(WhisperManager.Instance.FilePath);
        TXT_fileName.text = fileName;
    }

    public void UpdateOutputText()
    {
        string output =WhisperManager.Instance.ConvertedText;
        TXT_OutputText.text = output;
    }
}
