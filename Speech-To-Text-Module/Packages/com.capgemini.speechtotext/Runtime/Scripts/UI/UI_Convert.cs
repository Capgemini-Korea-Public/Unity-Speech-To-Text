using UnityEngine;
using UnityEngine.UI;

public class UI_Convert : MonoBehaviour
{
    [SerializeField] private Button btn_Convert;

    public void UpdateConvertBtnInteractable()
    {
        btn_Convert.interactable = true ? STTManager.Instance.FilePath != "" : false;
    }
}
