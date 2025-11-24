namespace CustomAds.Core {
    /// <summary>
    /// Revenue information from an ad
    /// </summary>
    public class AdRevenue {
        public long Value { get; set; }
        public string CurrencyCode { get; set; }
        public int Precision { get; set; }

        public override string ToString() {
            return $"{Value} {CurrencyCode} (Precision: {Precision})";
        }
    }
}
