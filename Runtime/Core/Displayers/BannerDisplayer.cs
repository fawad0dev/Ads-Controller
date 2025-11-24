using System;
using UnityEngine;
using UnityEngine.Events;
using CustomAds.Core;

namespace CustomAds {
    /// <summary>
    /// Legacy BannerDisplayer - Updated to use new AdsManager API
    /// Maintains backward compatibility with existing scenes
    /// </summary>
    public class BannerDisplayer : MonoBehaviour {
        [SerializeField] string adName = "Main";
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
            AdsManager.ShowBanner(adName, result => {
                if (!result.Success) {
                    displayedFailedEvent?.Invoke(result.ErrorMessage);
                }
            });
        }

        public void Hide() {
            AdsManager.HideBanner(adName);
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
}
