namespace CustomAds.Core {
    /// <summary>
    /// Result of an ad operation (load or show)
    /// </summary>
    public class AdResult {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int ErrorCode { get; set; }

        public static AdResult Successful() {
            return new AdResult { Success = true };
        }

        public static AdResult Failed(string errorMessage, int errorCode = 0) {
            return new AdResult {
                Success = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode
            };
        }

        public override string ToString() {
            return Success ? "Success" : $"Failed: {ErrorMessage} (Code: {ErrorCode})";
        }
    }
}
