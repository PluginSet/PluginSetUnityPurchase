using System.Collections;
using System.Collections.Generic;
using PluginSet.Core.Editor;
using UnityEngine;

namespace PluginSet.UnityPurchase.Editor
{
    [BuildChannelsParams("UnityPurchase","Unity In App Purchasing配置")]
    public class BuildUnityPurchaseParams : ScriptableObject
    {
        [Tooltip("是否启动Unity充值API")]
        public bool Enable = true;

        [Tooltip("通过ProductCatalog初始化（自动初始化），未勾选时需要通过事件来触发初始化")]
        public bool InitWithCatalog;
    }
}
