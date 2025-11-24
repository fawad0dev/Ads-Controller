using System;

namespace CustomAds.Core {
    /// <summary>
    /// Interface for native ad operations
    /// </summary>
    public interface INativeAd : IAdUnit {
        /// <summary>
        /// Show the native ad
        /// </summary>
        void Show(Action<AdResult> onComplete = null);

        /// <summary>
        /// Hide the native ad
        /// </summary>
        void Hide();

        /// <summary>
        /// Event triggered when ad is clicked
        /// </summary>
        event Action OnClicked;

        /// <summary>
        /// Event triggered when impression is recorded
        /// </summary>
        event Action OnImpression;

        /// <summary>
        /// Event triggered when user earns revenue from this ad
        /// </summary>
        event Action<AdRevenue> OnPaid;
    }
}
