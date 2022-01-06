#if ENABLE_UNITY_PURCHASE
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

namespace PluginSet.UnityPurchasingAPI
{
    public class UnityPurchasingAPI : IStoreListener
    {
        public delegate void OnInitializeFailedDelegate(string error);
        public delegate void OnInitializeSuccessDelegate();

        public delegate void OnPurchaseFailedDelegate(string productId, string reason);
        public delegate void OnPurchaseSuccessDelegate(Product product);

        public event OnInitializeFailedDelegate InitializeFailed;
        public event OnInitializeSuccessDelegate InitializeSuccess;
        public event OnPurchaseFailedDelegate PurchaseFailed;
        public event OnPurchaseSuccessDelegate PurchaseSuccess;
        
        private IStoreController m_Controller;
        private IExtensionProvider m_Extensions;

        public List<Product> AllProducts
        {
            get
            {
                var result = new List<Product>();
                if (m_Controller != null)
                {
                    foreach (var product in m_Controller.products.all)
                    {
                        result.Add(new Product(product, m_Extensions));
                    }
                }

                return result;
            }
        }
        
        public void Init()
        {
            var module = StandardPurchasingModule.Instance();
            module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;
            
#if INIT_IAP_WITH_CATALOG
            var builder = ConfigurationBuilder.Instance(module);
            var catalog = ProductCatalog.LoadDefaultCatalog();
            foreach (var product in catalog.allValidProducts)
            {
                if (product.allStoreIDs.Count > 0)
                {
                    var ids = new IDs();
                    foreach (var storeID in product.allStoreIDs)
                    {
                        ids.Add(storeID.id, storeID.store);
                    }
                    builder.AddProduct(product.id, product.type, ids);
                }
                else
                {
                    builder.AddProduct(product.id, product.type);
                }
            }
                
            UnityPurchasing.Initialize(this, builder);
#endif
        }
        

        public void InitWithProducts(Dictionary<string, object> products)
        {
            var module = StandardPurchasingModule.Instance();
            var builder = ConfigurationBuilder.Instance(module);
            foreach (var kv in products)
            {
                builder.AddProduct(kv.Key, (ProductType) kv.Value);
            }
            UnityPurchasing.Initialize(this, builder);
        }

        public Product FindProduct(string productId)
        {
            var product = m_Controller?.products.WithID(productId);
            if (product == null)
                return null;

            return new Product(product, m_Extensions);
        }

        public void BuyProduct(Product product)
        {
            m_Controller.InitiatePurchase(product.UnityProduct);
        }
        
        public void PendProduct(string productId)
        {
            var product = m_Controller?.products.WithID(productId);
            if (product != null)
                m_Controller.ConfirmPendingPurchase(product);
        }
        
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            InitializeFailed?.Invoke(error.ToString());
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            var product = purchaseEvent.purchasedProduct;
#if UNITY_ANDROID
            var validator = new CrossPlatformValidator(GooglePlayTangle.Data(),AppleTangle.Data(), Application.identifier);
            var result = validator.Validate(purchaseEvent.purchasedProduct.receipt);
            foreach (IPurchaseReceipt productReceipt in result)
            {
                GooglePlayReceipt google = productReceipt as GooglePlayReceipt;
                if (null != google)
                {
                    var _result = new Product(product, m_Extensions);
                    _result.SetReceipt(google.purchaseToken);
                    PurchaseSuccess?.Invoke(_result);
                    Debug.Log($"ProcessPurchase ========================== {product.definition.id}:{google.purchaseToken}");
                    break;
                }
            }
#else
            PurchaseSuccess?.Invoke(new Product(product, m_Extensions));
            Debug.Log($"ProcessPurchase ========================== {product.definition.id}:{product.receipt}");
#endif
            return PurchaseProcessingResult.Pending;
        }

        public void OnPurchaseFailed(UnityEngine.Purchasing.Product product, PurchaseFailureReason failureReason)
        {
            PurchaseFailed?.Invoke(product.definition.id, failureReason.ToString());
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            m_Controller = controller;
            m_Extensions = extensions;
            
            var appleExtensions = extensions.GetExtension<IAppleExtensions>();
            appleExtensions.RegisterPurchaseDeferredListener(OnDeferred);
            
            InitializeSuccess?.Invoke();
        }
        
        /// <summary>
        /// iOS Specific.
        /// This is called as part of Apple's 'Ask to buy' functionality,
        /// when a purchase is requested by a minor and referred to a parent
        /// for approval.
        ///
        /// When the purchase is approved or rejected, the normal purchase events
        /// will fire.
        /// </summary>
        /// <param name="item">Item.</param>
        private void OnDeferred(UnityEngine.Purchasing.Product item)
        {
            Debug.Log("Purchase deferred: " + item.definition.id);
        }
    }
}
#endif
