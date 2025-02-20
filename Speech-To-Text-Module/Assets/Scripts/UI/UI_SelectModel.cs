//using System;
//using UnityEngine;
//using UnityEngine.UI;

//public class UI_SelectModel : MonoBehaviour
//{
//    [SerializeField] private GameObject dropdown;

//    [SerializeField] private Button btn_sentis;
//    [SerializeField] private Button btn_openai;

//    private void Start()
//    {
//        UIManager.Instance.UpdateModelName(SpeechToTextController.Instance.STTModelType.ToString());
//    }

//    public void SelectModel(int index)
//    {
//        ESTTModelType modelName = ((ESTTModelType)index);
//        SpeechToTextController.Instance.SetModelType(modelName);
//        UIManager.Instance.UpdateModelName(modelName.ToString());
//        Toggle();
//    }

//    public void Toggle()
//    {
//        dropdown.SetActive(!dropdown.activeInHierarchy);
//    }
//}
