using TMPro;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [Header ("Text")]
    [SerializeField] private TextMeshProUGUI TXT_ModelName;
    [SerializeField] private TextMeshProUGUI TXT_FileName;
    [SerializeField] private TextMeshProUGUI TXT_OutputText;

    public void UpdateModelName(string modelNameText)
    {
        TXT_ModelName.text = modelNameText;
    }

    public void UpdateFileName(string fileName)
    {
        TXT_FileName.text = fileName;      
    }

    public void UpdateOutputText(string convertedText)
    {
        TXT_OutputText.text = convertedText;
    }
}
