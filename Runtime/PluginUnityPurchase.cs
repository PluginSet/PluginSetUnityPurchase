#if ENABLE_UNITY_PURCHASE
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using PluginSet.Core;
using PluginSet.UnityPurchasingAPI;
using UnityEngine;
using Logger = PluginSet.Core.Logger;

namespace PluginSet.UnityPurchase
{
    [PluginRegister]
    public class PluginUnityPurchase : PluginBase, IStartPlugin, IIAPurchasePlugin
    {
        [Serializable]
        private class PurchasingOrderInfo
        {
            [SerializeField]
            public string productId;
            [SerializeField]
            public string transactionId;
            [SerializeField]
            public float price;
            [SerializeField]
            public string currency;
            [SerializeField]
            public string receipt;
        }
        
        public override string Name => "UnityPurchase";

        private static readonly Logger Logger = LoggerManager.GetLogger("UnityPurchase");

        public int StartOrder => 0;
        
        public bool IsRunning { get; private set; }
        
        public bool IsEnablePayment { get; private set; }
        
        private List<Action<Result>> _payCallbacks = new List<Action<Result>>();
        private Result? _paymentResult = null;
        private string _payingProductId = null;
        private readonly List<Result> _lostPaymenets = new List<Result>();
        
        private readonly Dictionary<string, string> transactionProducts = new Dictionary<string, string>();
        
        private readonly PurchasingOrderInfo tempOrderInfo = new PurchasingOrderInfo();

        private UnityPurchasingAPI.UnityPurchasingAPI Api;

        protected override void Init(PluginSetConfig config)
        {
            var cfg = config.Get<PluginUnityPurchaseConfig>("UnityPurchase");
            
            
            Api = new UnityPurchasingAPI.UnityPurchasingAPI();
            Api.InitializeSuccess += OnInitialized;
            Api.InitializeFailed += OnInitializeFailed;
            Api.PurchaseFailed += OnPurchaseFailed;
            Api.PurchaseSuccess += OnProcessPurchase;

            byte[] gp = null;
            byte[] ac = null;
            if (!string.IsNullOrEmpty(cfg.GooglePublicKey))
                gp = Convert.FromBase64String(cfg.GooglePublicKey);
            if (!string.IsNullOrEmpty(cfg.AppleRootCert))
                ac = Convert.FromBase64String(cfg.AppleRootCert);
            Api.Init(gp, ac);
        }

        public IEnumerator StartPlugin()
        {
            IsRunning = true;
            IsEnablePayment = false;
            
            yield break;
        }

        public void DisposePlugin(bool isAppQuit = false)
        {
            IsRunning = false;
            IsEnablePayment = false;
            transactionProducts.Clear();
        }

        public void InitWithProducts(Dictionary<string, int> products)
        {
            Api.InitWithProducts(products);
        }

        public void PaymentComplete(string transactionId)
        {
            if (transactionProducts.TryGetValue(transactionId, out var productId))
                Api.PendProduct(productId);
            else
                Logger.Warn($"Cannot find product with transactionId:{transactionId}");
        }
        
        public void Pay(string productId, Action<Result> callback = null, string jsonData = null)
        {
            if (!IsRunning || !IsEnablePayment)
            {
                callback?.Invoke(new Result
                {
                    Success = false,
                    PluginName = Name,
                    Code = PluginConstants.FailDefaultCode,
                    Data = OnProductIdToJson(productId),
                    Error = "IAP not inited"
                });
                return;
            }

            if (!string.IsNullOrEmpty(_payingProductId))
            {
                callback?.Invoke(new Result
                {
                    Success = false,
                    PluginName = Name,
                    Code = PluginConstants.FailDefaultCode,
                    Data = OnProductIdToJson(productId),
                    Error = "Is paying",
                });
                return;
            }

            var product = Api.FindProduct(productId);
            if (product == null || !product.AvailableToPurchase)
            {
                callback?.Invoke(new Result
                {
                    Success = false,
                    PluginName = Name,
                    Code = PluginConstants.FailDefaultCode,
                    Data = OnProductIdToJson(productId),
                    Error = "Invalid product"
                });
                return;
            }

            _payingProductId = productId;
            Api.BuyProduct(product);
            
            if (callback != null)
                _payCallbacks.Add(callback);
        }

        private void OnInitialized()
        {
            Logger.Debug("PurchaseManager OnInitialized");

            IsEnablePayment = true;
            
            SendNotification(PluginConstants.IAP_ON_INIT_SUCCESS, Api.AllProducts);
            OnCheckLostPayments();
        }

        private void OnInitializeFailed(string error)
        {
            SendNotification(PluginConstants.IAP_ON_INIT_FAILED, error);
        }

        private void OnProcessPurchase(Product product)
        {
            var productId = product.ProductId;
            tempOrderInfo.productId = productId;
            tempOrderInfo.transactionId = product.TransactionId;
            tempOrderInfo.price = product.Price;
            tempOrderInfo.currency = product.Currency;
            tempOrderInfo.receipt = product.Receipt;

            transactionProducts[product.TransactionId] = productId;

            var result = new Result
            {
                Success = true,
                PluginName = Name,
                Code = PluginConstants.SuccessCode,
                Data = JsonUtil.ToJson(tempOrderInfo),
            };

            if (productId.Equals(_payingProductId))
            {
                _paymentResult = result;
            }
            else
            {
                if (!SendNotification(PluginConstants.IAP_CALLBACK_LOST_PAYMENTS, result))
                    _lostPaymenets.Add(result);
            }
        }

        private string OnProductIdToJson(string productId)
        {
            return $"{{\"productId\":\"{productId}\"}}";
        }

        /// <summary>
        /// A purchase failed with specified reason.
        /// </summary>
        /// <param name="productId">The product that was attempted to be purchased. </param>
        /// <param name="failureReason">The failure reason.</param>
        private void OnPurchaseFailed(string productId, string failureReason)
        {
            if (!productId.Equals(_payingProductId))
                return;
            
            _payingProductId = null;
            
            var result = new Result
            {
                Success = false,
                PluginName = Name,
                Code = PluginConstants.FailDefaultCode,
                Data = OnProductIdToJson(productId),
                Error = failureReason,
            };
            
            foreach (var callback in _payCallbacks)
            {
                callback.Invoke(result);
            }

            _payCallbacks.Clear();
        }

        private void Update()
        {
            CheckPurchaseSuccess();
        }

        private void CheckPurchaseSuccess()
        {
            if (_paymentResult == null)
                return;

            var result = _paymentResult.Value;
            _paymentResult = null;
            _payingProductId = null;
            
            foreach (var callback in _payCallbacks)
            {
                callback.Invoke(result);
            }

            _payCallbacks.Clear();
        }

        private void OnCheckLostPayments()
        {
            foreach (var result in _lostPaymenets)
            {
                SendNotification(PluginConstants.IAP_CALLBACK_LOST_PAYMENTS, result);
            }
            _lostPaymenets.Clear();
        }

        
    }
}
#endif
