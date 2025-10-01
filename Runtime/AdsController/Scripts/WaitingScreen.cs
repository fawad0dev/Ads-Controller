using TMPro;
using UnityEngine;

public class WaitingScreen : MonoBehaviour
{
    [SerializeField] private GameObject waitingScreen;
    [SerializeField] TMP_Text waitingText;

    public void Show(bool show, string text)
    {
        waitingScreen.SetActive(show);
        if (string.IsNullOrEmpty(text))
            waitingText.gameObject.SetActive(false);
        else
        {
            waitingText.gameObject.SetActive(true);
            waitingText.text = text;
        }
    }
}
