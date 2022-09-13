#if ENABLE_UNITY_PURCHASE
using System;
using System.Collections.Generic;
using PluginSet.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

namespace PluginSet.UnityPurchasingAPI
{
    public class UnityPurchasingAPI : IStoreListener
    {
        [Serializable]
        private struct ProductSerialize
        {
            [SerializeField]
            public bool availableToPurchase;
            [SerializeField]
            public string productId;
            [SerializeField]
            public float price;
            [SerializeField]
            public string currency;
            [SerializeField]
            public string priceString;
            [SerializeField]
            public string title;
            [SerializeField]
            public string description;
        }
        
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

        private byte[] _googlePublicKey;
        private byte[] _appleRootCert;
        
        public string SerializeProducts(List<Product> allProducts)
        {
            List<ProductSerialize> list = new List<ProductSerialize>();
            foreach (var product in allProducts)
            {
                list.Add(new ProductSerialize
                {
                    availableToPurchase = product.AvailableToPurchase,
                    productId = product.ProductId,
                    price = product.Price,
                    priceString = product.PriceString,
                    currency = product.Currency,
                    title = product.Title,
                    description = product.Description,
                });
            }

            return JsonUtil.ToJson(list);
        }

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
        
        public void Init(byte[] googlePublicKey, byte[] appleRootCert)
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
            _googlePublicKey = googlePublicKey;
            _appleRootCert = appleRootCert;
        }
        

        public void InitWithProducts(Dictionary<string, int> products)
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
            CrossPlatformValidator validator = null;
#if UNITY_ANDROID
            if (_googlePublicKey != null)
                validator = new CrossPlatformValidator(_googlePublicKey, _appleRootCert, Application.identifier);
//#elif UNITY_IOS
//            if (_appleRootCert != null)
//                validator = new CrossPlatformValidator(_googlePublicKey, _appleRootCert, Application.identifier);
#endif
            var invoked = false;
            if (validator != null)
            {
                var result = validator.Validate(purchaseEvent.purchasedProduct.receipt);
                foreach (IPurchaseReceipt productReceipt in result)
                {
#if UNITY_ANDROID
                    GooglePlayReceipt google = productReceipt as GooglePlayReceipt;
                    string receipt = google?.purchaseToken;
#else
                    string receipt = null;
#endif
                    if (!string.IsNullOrEmpty(receipt))
                    {
                        var pro = new Product(product, m_Extensions);
                        pro.SetReceipt(receipt);
                        PurchaseSuccess?.Invoke(pro);
                        invoked = true;
                        break;
                    }
                }
            }
            
            if (!invoked)
                PurchaseSuccess?.Invoke(new Product(product, m_Extensions));
            
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
