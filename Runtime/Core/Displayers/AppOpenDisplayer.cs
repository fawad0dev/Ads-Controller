using UnityEngine;
using UnityEngine.Events;
using CustomAds.Core;

namespace CustomAds {
    /// <summary>
    /// Legacy AppOpenDisplayer - Updated to use new AdsManager API
    /// Maintains backward compatibility with existing scenes
    /// </summary>
    public class AppOpenDisplayer : MonoBehaviour {
        [SerializeField] string adName = "Main";
        [SerializeField] UnityEvent<string> onFailed;
        [SerializeField] UnityEvent onOpen;
        [SerializeField] UnityEvent onClose;
        [SerializeField] UnityEvent onClicked;
        [SerializeField] UnityEvent onImpression;
        [SerializeField] UnityEvent<long> onPaid;
        [SerializeField] DisplayBy displayBy = DisplayBy.OnFocus;
        [SerializeField] float cooldownSeconds = 5f;

        static float lastAdShownTime = float.MinValue;

        enum DisplayBy {
            OnFocus,
            OnPause
        }

        bool CanShowAd() {
            return Time.realtimeSinceStartup - lastAdShownTime >= cooldownSeconds;
        }

        void OnApplicationFocus(bool focus) {
            if (displayBy != DisplayBy.OnFocus) return;
            if (focus && CanShowAd()) {
                Show();
            }
        }

        void OnApplicationPause(bool pause) {
            if (displayBy != DisplayBy.OnPause) return;
            if (!pause && CanShowAd()) {
                Show();
            }
        }

        public void Show() {
            var appOpen = AdsManager.GetAppOpen(adName);

            if (appOpen == null) {
                onFailed?.Invoke("App open ad not configured");
                return;
            }

            if (!appOpen.IsReady) {
                return; // Silently skip if not ready
            }

            lastAdShownTime = Time.realtimeSinceStartup;

            // Subscribe to events
            appOpen.OnOpened += () => onOpen?.Invoke();
            appOpen.OnClosed += () => onClose?.Invoke();
            appOpen.OnClicked += () => onClicked?.Invoke();
            appOpen.OnImpression += () => onImpression?.Invoke();
            appOpen.OnPaid += (revenue) => onPaid?.Invoke((long)revenue.Value);

            AdsManager.ShowAppOpen(adName, result => {
                if (!result.Success) {
                    onFailed?.Invoke(result.ErrorMessage);
                }
            });
        }
    }
}
