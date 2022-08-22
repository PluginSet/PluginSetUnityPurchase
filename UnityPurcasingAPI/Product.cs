#if ENABLE_UNITY_PURCHASE
using PluginSet.Core;
using UnityEngine.Purchasing;

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
        public string TransactionId => _product.transactionID;

        public override float Price => (float)_product.metadata.localizedPrice * 100f;

        public override string Currency => _product.metadata.isoCurrencyCode;

        public override string PriceString => _product.metadata.localizedPriceString;
        public override string Title => _product.metadata.localizedTitle;
        public override string Description => _product.metadata.localizedDescription;

        /// <summary>
        /// </summary>
        private string customReceipt;

        public string Receipt
        {
            get
            {
                if (!string.IsNullOrEmpty(customReceipt))
                    return customReceipt;
                
#if UNITY_IOS
                var appleExtensions = _extension.GetExtension<IAppleExtensions>();
                return appleExtensions.GetTransactionReceiptForProduct(_product);
#else
                return customReceipt;
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
            customReceipt = receipt;
        }
    }
}
#endif