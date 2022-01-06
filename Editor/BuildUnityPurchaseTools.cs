using PluginSet.Core.Editor;
using UnityEngine;

namespace PluginSet.UnityPurchase.Editor
{
    [BuildTools]
    public static class BuildUnityPurchaseTools
    {
        [OnSyncEditorSetting]
        public static void OnSyncEditorSetting(BuildProcessorContext context)
        {
            var buildParams = context.BuildChannels.Get<BuildUnityPurchaseParams>("UnityPurchase");
            if (!buildParams.Enable)
                return;
            
            context.Symbols.Add("ENABLE_UNITY_PURCHASE");
            
            if (buildParams.InitWithCatalog)
                context.Symbols.Add("INIT_IAP_WITH_CATALOG");
            
            context.AddLinkAssembly("UnityPurchasing.API");
            context.AddLinkAssembly("PluginSet.UnityPurchase");
            
            context.AddLinkAssembly("UnityEngine.Purchasing", "UnityEngine.Purchasing.Extension.IPurchasingBinder");
            context.AddLinkAssembly("Stores", "UnityEngine.Purchasing.CloudCatalogImpl", "UnityEngine.Purchasing.Promo");
            context.AddLinkAssembly("mscorlib", "System.Security.Cryptography.*");
            
        }
        
    }
}
