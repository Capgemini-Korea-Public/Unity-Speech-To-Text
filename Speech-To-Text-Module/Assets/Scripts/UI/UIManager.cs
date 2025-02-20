using TMPro;
using UnityEngine;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [field: Header("File")]
    [field: SerializeField] public UI_Convert UI_Convert { get; private set; }

    [Header ("Text")]
    [SerializeField] private TextMeshProUGUI TXT_ModelName;
    [SerializeField] private TextMeshProUGUI TXT_FileName;
    [SerializeField] private TextMeshProUGUI TXT_Output;
    [SerializeField] private TextMeshProUGUI TXT_Warning;

    private Coroutine warningCor;
    private readonly float warningDuration = 2f;

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
        TXT_Output.text = convertedText;
    }

    public void Warning(string message)
    {
        TXT_Warning.text = message;
        TXT_Warning.gameObject.SetActive(true);

        if(warningCor != null) 
            StopCoroutine(warningCor);
        warningCor = StartCoroutine(HideWarningAfterDelay());
    }

    private IEnumerator HideWarningAfterDelay()
    {
        yield return new WaitForSeconds(warningDuration);
        TXT_Warning.gameObject.SetActive(false);
    }
}
