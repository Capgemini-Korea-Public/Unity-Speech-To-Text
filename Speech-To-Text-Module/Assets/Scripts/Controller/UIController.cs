using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using SpeechToTextUnity;

public class UIController : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button btn_FileSelect;
    [SerializeField] private Button btn_Convert;

    [Header ("Text")]
    [SerializeField] private TextMeshProUGUI txt_ModelNameDesc;
    [SerializeField] private TextMeshProUGUI txt_FileNameDesc;
    [SerializeField] private TextMeshProUGUI txt_OutputDesc;
    [SerializeField] private TextMeshProUGUI txt_Warning;

    [Header("Select Model")]
    [SerializeField] private GameObject dropdown;
    [SerializeField] private Button btn_ModelSelect;
    [SerializeField] private Button btn_Sentis;
    [SerializeField] private Button btn_Openai;

    private void Start()
    {
        btn_FileSelect.onClick.AddListener(() => SpeechToTextController.Instance.FileSelect());
        btn_Convert.onClick.AddListener(() => SpeechToTextController.Instance.Convert());
        btn_ModelSelect.onClick.AddListener(() => OnClickModelSelectBtn());
        btn_Sentis.onClick.AddListener(() => SelectModel((int)ESTTModelType.SentisWhisper));
        btn_Openai.onClick.AddListener(() => SelectModel((int)ESTTModelType.OpenAIWhisper));

        SpeechToTextController.Instance.OnFileSelected += ResetUI;
        SpeechToTextController.Instance.OnFileSelected += UpdateFileName;
        SpeechToTextController.Instance.OnConvertBtnClicked += OnClickConvertBtn;
        SpeechToTextController.Instance.OnOutputTextChanged += UpdateOutputText;

        UpdateModelName(SpeechToTextController.Instance.STTModelType.ToString());
    }

    private void UpdateModelName(string modelNameText)
    {
        txt_ModelNameDesc.text = modelNameText;
    }

    private void UpdateFileName(string fileName)
    {
        txt_FileNameDesc.text = fileName;      
    }

    private void UpdateOutputText(string convertedText)
    {
        txt_OutputDesc.text = convertedText;
    }

    private void OnClickConvertBtn()
    {
        btn_Convert.interactable = false;
    }

    private void CheckConvertBtnStatus()
    {
        btn_Convert.interactable = true ? SpeechToTextController.Instance.FilePath != "" : false;
    }

    private void ResetUI(string str)
    {
        UpdateFileName("");
        UpdateOutputText("");

        CheckConvertBtnStatus();
    }

    #region UI Model Select
    private void SelectModel(int index)
    {
        ESTTModelType modelName = ((ESTTModelType)index);
        SpeechToTextController.Instance.SetModelType(modelName);
        UpdateModelName(modelName.ToString());
        Toggle();
    }

    private void OnClickModelSelectBtn()
    {
        Toggle();
    }

    private void Toggle()
    {
        dropdown.SetActive(!dropdown.activeInHierarchy);
    }
    #endregion

    #region UI Warning
    private Coroutine warningCor;
    private readonly float warningDuration = 2f;

    private void Warning(string message)
    {
        txt_Warning.text = message;
        txt_Warning.gameObject.SetActive(true);

        if(warningCor != null) 
            StopCoroutine(warningCor);
        warningCor = StartCoroutine(HideWarningAfterDelay());
    }

    private IEnumerator HideWarningAfterDelay()
    {
        yield return new WaitForSeconds(warningDuration);
        txt_Warning.gameObject.SetActive(false);
    }
    #endregion
}
