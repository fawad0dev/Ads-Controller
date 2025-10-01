using System;
using UnityEngine;
using UnityEngine.Events;

public class BannerDisplayer : MonoBehaviour {
    [SerializeField] int index;
    [SerializeField] EnableState enableState;
    [SerializeField] DisableState disableState;
    [SerializeField] UnityEvent<string> displayedFailedEvent;
    private bool wasShown;
    enum EnableState {
        None,
        Show,
        Hide
    }
    enum DisableState {
        None,
        Show,
        Hide,
        Revert
    }

    public void Show() {
        AdsController.ShowBanner(index, displayedFailedEvent.Invoke);
    }
    public void Hide() {
        AdsController.HideBanner(index);
    }
    private void OnEnable() {
        switch (enableState) {
            case EnableState.Show:
                Show();
                wasShown = true;
                break;
            case EnableState.Hide:
                Hide();
                wasShown = false;
                break;
        }
    }

    private void OnDisable() {
        switch (disableState) {
            case DisableState.Show:
                Show();
                break;
            case DisableState.Hide:
                Hide();
                break;
            case DisableState.Revert:
                if (wasShown) {
                    Show();
                } else {
                    Hide();
                }
                break;
        }
    }

}
