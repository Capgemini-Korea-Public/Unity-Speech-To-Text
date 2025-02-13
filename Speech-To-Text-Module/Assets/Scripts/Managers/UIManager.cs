using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [Header ("Text")]
    [SerializeField] private TextMeshProUGUI TXT_fileName;
    [SerializeField] private TextMeshProUGUI TXT_OutputText;

    [Header("Button")]
    [SerializeField] private Button Btn_Convert;

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
