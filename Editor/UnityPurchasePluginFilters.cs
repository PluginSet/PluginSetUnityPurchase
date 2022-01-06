using System.Collections;
using System.Collections.Generic;
using PluginSet.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace PluginSet.UnityPurchase.Editor
{
    [InitializeOnLoad]
    public static class UnityPurchasePluginFilters
    {
        private static bool FilterPlugins(string s, BuildProcessorContext context)
        {
            var buildParams = context.BuildChannels.Get<BuildUnityPurchaseParams>("UnityPurchase");
            if (!buildParams.Enable)
            {
                Debug.Log("Filter lib file :::::::  " + s);
            }

            return !buildParams.Enable;
        }
        
        static UnityPurchasePluginFilters()
        {
            PluginFilter.RegisterFilter("com.unity.purchasing/Plugins/UnityPurchasing", FilterPlugins);
            PluginFilter.RegisterFilter("com.unity.purchasing/Plugins/UnityPurchasing/iOS", FilterPlugins);
            PluginFilter.RegisterFilter("com.unity.purchasing/Plugins/UnityPurchasing/Android", FilterPlugins);
        }
    }

}
