using System;

namespace CustomAds.Core {
    /// <summary>
    /// Interface for banner ad operations
    /// </summary>
    public interface IBannerAd : IAdUnit {
        /// <summary>
        /// Whether the banner is currently visible
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Show the banner ad
        /// </summary>
        void Show(Action<AdResult> onComplete = null);

        /// <summary>
        /// Hide the banner ad
        /// </summary>
        void Hide();

        /// <summary>
        /// Event triggered when banner is clicked
        /// </summary>
        event Action OnClicked;

        /// <summary>
        /// Event triggered when banner impression is recorded
        /// </summary>
        event Action OnImpression;

        /// <summary>
        /// Event triggered when user earns revenue from this ad
        /// </summary>
        event Action<AdRevenue> OnPaid;
    }
}
