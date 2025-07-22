using System;
using System.Linq;
using AssetsTools.NET.Extra;
using UnityEngine;

namespace UABS.Assets.Script.__Test__
{
    public class AllAssetClassIDs : MonoBehaviour
    {
        private void Start()
        {
            var allAssetClassIDs = Enum.GetValues(typeof(AssetClassID)).Cast<AssetClassID>().ToArray();
            Debug.Log(allAssetClassIDs.Length);
        }
    }
}