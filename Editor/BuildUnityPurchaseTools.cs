﻿using PluginSet.Core;
using PluginSet.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace PluginSet.UnityPurchase.Editor
{
    [BuildTools]
    public static class BuildUnityPurchaseTools
    {
        [OnSyncEditorSetting]
        public static void OnSyncEditorSetting(BuildProcessorContext context)
        {
            if (context.BuildTarget != BuildTarget.Android && context.BuildTarget != BuildTarget.iOS)
                return;
                
            var buildParams = context.BuildChannels.Get<BuildUnityPurchaseParams>();
            if (!buildParams.Enable)
                return;

            if (!Global.IncludePackage("com.unity.purchasing"))
                throw new BuildException("Cannot find package com.unity.purchasing! Please import it first!");
            
            context.Symbols.Add("ENABLE_UNITY_PURCHASE");
            
            if (buildParams.InitWithCatalog)
                context.Symbols.Add("INIT_IAP_WITH_CATALOG");
            
            context.AddLinkAssembly("UnityPurchasing.API");
            context.AddLinkAssembly("PluginSet.UnityPurchase");
            
            context.AddLinkAssembly("UnityEngine.Purchasing", "UnityEngine.Purchasing.Extension.IPurchasingBinder");
            context.AddLinkAssembly("Stores", "UnityEngine.Purchasing.CloudCatalogImpl", "UnityEngine.Purchasing.Promo");
            context.AddLinkAssembly("mscorlib", "System.Security.Cryptography.*");
            
            var pluginConfig = context.Get<PluginSetConfig>("pluginsConfig");
            var config = pluginConfig.AddConfig<PluginUnityPurchaseConfig>("UnityPurchase");
            config.GooglePublicKey = buildParams.GooglePublicKey;
            config.AppleRootCert = buildParams.AppleRootCert;
        }
        
    }
}
