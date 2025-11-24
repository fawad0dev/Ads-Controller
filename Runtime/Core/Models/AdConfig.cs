using UnityEngine;

namespace CustomAds.Core {
    /// <summary>
    /// Configuration for an ad unit
    /// </summary>
    [System.Serializable]
    public class AdConfig {
        [Header("Ad Unit IDs")]
        public string androidAdUnitId;
        public string iosAdUnitId;

        [Header("Test Mode")]
        public bool useTestIds = false;

        [Header("Loading")]
        public float loadCooldown = 5f;

        [Header("Debug")]
        public bool debugLogs = false;

        public string GetAdUnitId() {
#if UNITY_ANDROID
            return androidAdUnitId;
#elif UNITY_IOS
            return iosAdUnitId;
#else
            return null;
#endif
        }
    }
}
