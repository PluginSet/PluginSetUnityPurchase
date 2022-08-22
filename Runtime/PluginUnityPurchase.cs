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
        public override string Name => "UnityPurchase";

        private static readonly Logger Logger = LoggerManager.GetLogger("UnityPurchase");

        public int StartOrder => 0;
        
        public bool IsRunning { get; private set; }
        
        public bool IsEnablePayment { get; private set; }
        
        private List<Action<Result>> _payCallbacks = new List<Action<Result>>();
        private Result? _paymentResult = null;
        private string _payingProductId = null;
        private readonly List<Result> _lostPaymenets = new List<Result>();

        private UnityPurchasingAPI.UnityPurchasingAPI Api;

        protected override void Init(PluginSetConfig config)
        {
            AddEventListener(PluginConstants.IAP_PURCHASED_PRODUCT_PENDED, OnProductPend);
            AddEventListener(PluginConstants.IAP_CHECK_LOST_PAYMENTS, OnCheckLostPayments);
        }

        public IEnumerator StartPlugin()
        {
            IsRunning = true;
            IsEnablePayment = false;
            
            Api = new UnityPurchasingAPI.UnityPurchasingAPI();
            Api.InitializeSuccess += OnInitialized;
            Api.InitializeFailed += OnInitializeFailed;
            Api.PurchaseFailed += OnPurchaseFailed;
            Api.PurchaseSuccess += OnProcessPurchase;
            
            Api.Init();
            
            AddEventListener(PluginConstants.IAP_INIT_WITH_PRODUCTS, InitWithProducts);
            yield break;
        }

        public void DisposePlugin(bool isAppQuit = false)
        {
            RemoveEventListener(PluginConstants.IAP_INIT_WITH_PRODUCTS, InitWithProducts);
            IsRunning = false;
            IsEnablePayment = false;
        }
        
        public void Pay(string productId, Action<Result> callback = null, string jsonData = null)
        {
            if (!IsRunning || !IsEnablePayment)
            {
                callback?.Invoke(new Result
                {
                    Success = false,
                    PluginName = Name,
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

        private void InitWithProducts(PluginsEventContext context)
        {
            Logger.Debug("PurchaseManager InitWithProducts ");
            var dict = (Dictionary<string, object>) context.Data;
            if (IsEnablePayment && (dict == null || dict.Count <= 0))
            {
                SendNotification(PluginConstants.IAP_ON_INIT_SUCCESS, Api.AllProducts);
                return;
            }
            
            InitWithProducts(dict);
        }

        private void InitWithProducts(Dictionary<string, object> products)
        {
            IsEnablePayment = false;
            Api.InitWithProducts(products);
        }

        private void OnInitialized()
        {
            Logger.Debug("PurchaseManager OnInitialized");

            IsEnablePayment = true;
            
            SendNotification(PluginConstants.IAP_ON_INIT_SUCCESS, Api.AllProducts);
        }

        private void OnInitializeFailed(string error)
        {
            SendNotification(PluginConstants.IAP_ON_INIT_FAILED, error);
        }

        private void OnProcessPurchase(Product product)
        {
            var productId = product.ProductId;

            Dictionary<string, object> _data = new Dictionary<string, object>();
            _data.Add("productId", productId);
            _data.Add("transactionId", product.TransactionId);
            _data.Add("price", product.Price);
            _data.Add("currency", product.Currency);
            _data.Add("extra", product.Receipt);
            string jsonStr = JsonConvert.SerializeObject(_data);

            var result = new Result
            {
                Success = true,
                PluginName = Name,
                Data = jsonStr,
            };

            Debug.Log($"ProcessPurchase ========================== {result}:{product.Receipt}");

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
                Data = OnProductIdToJson(productId),
                Error = failureReason,
            };
            
            foreach (var callback in _payCallbacks)
            {
                callback.Invoke(result);
            }
            _payCallbacks.Clear();
            
            //SendNotification(PluginConstants.NOTIFY_PAY_FAIL, result);
        }

        private void OnProductPend(PluginsEventContext context)
        {
            var productId = (string) context.Data;
            if (string.IsNullOrEmpty(productId))
                return;

            Api.PendProduct(productId);
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
            
            //SendNotification(PluginConstants.NOTIFY_PAY_SUCCESS, result);
        }

        private void OnCheckLostPayments()
        {
            foreach (var result in _lostPaymenets)
            {
                SendNotification(PluginConstants.IAP_CALLBACK_LOST_PAYMENTS, result);
            }
            _lostPaymenets.Clear();
        }

        public void QueryMissingOrders(Action<Result> callback = null)
        {

        }

        public void CompleteMissingOrder(string transactionId)
        {

        }

        public void InitWithProducts(Dictionary<string, int> products)
        {
            throw new NotImplementedException();
        }

        public void PaymentComplete(string transactionId)
        {

        }
    }
}
#endif
