namespace PluginSet.UnityPurchasingAPI
{
#if UNITY_ANDROID
    public struct GooglePayloadJson
    {
        public string orderId;
        public string packageName;
        public string productId;
        public long purchaseTime;
        public int purchaseState;
        public string purchaseToken;
        public int quantity;
        public bool acknowledged;
        public string signature;
    }
    
    public struct GooglePayload
    {
        public string json;
    }
    
    public struct GooglePayloadData
    {
        public string Payload;
        public string Store;
        public string TransactionId;
    }
#endif
}