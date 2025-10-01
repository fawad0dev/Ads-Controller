using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Toast : MonoBehaviour
{
    [SerializeField] TMP_Text textComponent;
    [SerializeField] GameObject toastObject;
    
    public void ShowToast(string message)
    {
        textComponent.text = message;
        toastObject.SetActive(true);
    }
}
