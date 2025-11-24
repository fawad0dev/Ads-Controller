namespace CustomAds.Core {
    /// <summary>
    /// Reward information from a rewarded ad
    /// </summary>
    public class AdReward {
        public string Type { get; set; }
        public double Amount { get; set; }

        public override string ToString() {
            return $"{Amount} {Type}";
        }
    }
}
