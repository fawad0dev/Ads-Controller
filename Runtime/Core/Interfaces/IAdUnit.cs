using System;

namespace CustomAds.Core {
    /// <summary>
    /// Base interface for all ad units
    /// </summary>
    public interface IAdUnit {
        /// <summary>
        /// Unique identifier for this ad unit
        /// </summary>
        string AdUnitId { get; }

        /// <summary>
        /// Whether the ad is currently ready to be shown
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Whether the ad is currently loading
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// Initialize the ad unit
        /// </summary>
        void Initialize();

        /// <summary>
        /// Load the ad
        /// </summary>
        void Load(Action<AdResult> onComplete);

        /// <summary>
        /// Destroy the ad and clean up resources
        /// </summary>
        void Destroy();

        /// <summary>
        /// Get response info for debugging
        /// </summary>
        void LogResponseInfo();
    }
}
