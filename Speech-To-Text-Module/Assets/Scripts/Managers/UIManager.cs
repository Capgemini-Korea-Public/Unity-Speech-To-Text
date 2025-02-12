using System.IO;
using TMPro;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [field : Header ("Text")]
    [SerializeField] private TextMeshProUGUI TXT_fileName;
    [SerializeField] private TextMeshProUGUI TXT_OutputText;

    public void UpdateFileName(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        TXT_fileName.text = fileName;
    }

    public void UpdateOutputText(string convertedText)
    {
        string output = convertedText;
        TXT_OutputText.text = output;
    }
}
