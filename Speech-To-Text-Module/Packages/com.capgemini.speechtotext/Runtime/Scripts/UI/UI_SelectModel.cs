using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_SelectModel : MonoBehaviour
{
    [SerializeField] private GameObject dropdown;

    [SerializeField] private Button btn_sentis;
    [SerializeField] private Button btn_openai;

    private void Start()
    {
        UIManager.Instance.UpdateModelName(STTManager.Instance.STTModel.ToString());
    }

    public void SelectModel(int index)
    {
        ESTTType modelName = ((ESTTType)index);
        STTManager.Instance.SetModelType(modelName);
        UIManager.Instance.UpdateModelName(modelName.ToString());
        Toggle();
    }

    public void Toggle()
    {
        dropdown.SetActive(!dropdown.activeInHierarchy);
    }
}
