#if ENABLE_UNITY_PURCHASE
using PluginSet.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using ProductType = PluginSet.Core.ProductType;

namespace PluginSet.UnityPurchasingAPI
{
    public class Product: IPaymentProduct
    {
        private UnityEngine.Purchasing.Product _product;
#if UNITY_IOS
        private IExtensionProvider _extension;
#endif
        
        internal UnityEngine.Purchasing.Product UnityProduct => _product;

        public override bool AvailableToPurchase => _product.availableToPurchase;
        public override string ProductId => _product.definition.id;
        
        public override ProductType Type => (ProductType)((int)_product.definition.type);
        public string TransactionId => _product.transactionID;

        public override int Price => (int)(_product.metadata.localizedPrice * 100);

        public override string Currency => _product.metadata.isoCurrencyCode;

        public override string PriceString => _product.metadata.localizedPriceString;
        public override string Title => _product.metadata.localizedTitle;
        public override string Description => _product.metadata.localizedDescription;
        public override string Payload => _product.receipt;

        /// <summary>
        /// </summary>
        private string _customReceipt;

        public override string Receipt
        {
            get
            {
#if UNITY_IOS
                var appleExtensions = _extension.GetExtension<IAppleExtensions>();
                return appleExtensions.GetTransactionReceiptForProduct(_product);
#else
                if (!string.IsNullOrEmpty(_customReceipt))
                {
                    return _customReceipt;
                }
                
#if UNITY_ANDROID
                var data = JsonUtil.FromJson<GooglePayloadData>(Payload);
                var payload = JsonUtil.FromJson<GooglePayload>(data.Payload);
                var json = JsonUtil.FromJson<GooglePayloadJson>(payload.json);
                _customReceipt = json.purchaseToken;
#endif
                
                return _customReceipt;
#endif
            }
        }
        
        public Product(UnityEngine.Purchasing.Product product, IExtensionProvider extension)
        {
            _product = product;
#if UNITY_IOS
            _extension = extension;
#endif
        }

        public void SetReceipt(string receipt)
        {
            _customReceipt = receipt;
        }
    }
}
#endif