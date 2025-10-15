using System;
using UnityEngine;
using UnityEngine.Events;
namespace AdsControllerNS {
    public class NativeDisplayer : MonoBehaviour {
        [SerializeField] int index;
        [SerializeField] EnableState enableState;
        [SerializeField] DisableState disableState;
        [SerializeField] UnityEvent displayedEvent;
        [SerializeField] UnityEvent<string> displayedFailedEvent;
        [SerializeField] UnityEvent collapsedEvent;
        private bool disableCalled;
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
        bool wasShown;
        public void Show() {
            if (!AdsController.IsReady)
                return;
            if (!AdsController.IsNativeReady(index)) {
                AdsController.LoadNative(index,
                    () => {
                        if (disableCalled && (disableState == DisableState.Hide || (disableState == DisableState.Revert && !wasShown)))
                            return;
                        AdsController.RenderNative(index);
                        AdsController.ShowNative(index, displayedEvent.Invoke, displayedFailedEvent.Invoke);
                    },
                    error => {
                        Debug.LogWarning($"Failed to load native ad at index {index}: {error}");
                        displayedFailedEvent.Invoke(error);
                    });
            } else {
                AdsController.RenderNative(index);
                AdsController.ShowNative(index, displayedEvent.Invoke, displayedFailedEvent.Invoke);
            }
        }

        public void Hide() {
            if (!AdsController.IsReady)
                return;
            AdsController.HideNative(index, collapsedEvent.Invoke);
        }

        public void Render() {
            if (!AdsController.IsReady)
                return;
            if (!AdsController.IsNativeReady(index)) {
                Debug.LogWarning($"Native ad at index {index} is not ready to render.");
                return;
            }
            AdsController.RenderNative(index);
        }
        private void OnEnable() {
            disableCalled = false;
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
            disableCalled = true;
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
}