using System;
using System.Collections.Generic;
using System.Linq;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;
using UnityEngine;

namespace UABS.Assets.Script.DataSource.Manager
{
    public class AssetsOpenBundleManager : IAppEventListener
    {
        private readonly AssetParser _assetParser;

        public Func<List<ParsedAssetAndEntry>> GetEntryInfosCallBack;

        private List<ParsedAssetAndEntry> EntryInfos => GetEntryInfosCallBack != null ? GetEntryInfosCallBack() : new();

        public Action<List<ParsedAssetAndEntry>> SetEntryInfosCallBack;

        private AppEnvironment _appEnvironment;

        public AssetsOpenBundleManager(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _assetParser = new(appEnvironment.AssetsManager);
        }

        public void OnEvent(AppEvent e)
        {
            if (e is BundleRead4DependencyEvent br4d)
            {
                OpenBundle(br4d.Bundle, br4d.RealFilPath);
            }
            else if (e is BundleReadEvent bre)
            {
                OpenBundle(bre.Bundle, "");
            }
        }
        
        public void OpenBundle(BundleFileInstance bunInst, string brePath)
        {
            (List<ParsedAsset> parsedAssets, AssetsFileInstance fileInst) = _assetParser.ReadAssetOnly(bunInst);
            if (fileInst == null)
            {
                Debug.LogError($"Cannot open bundle in {brePath}");
                return;
            }
            AssetReader assetReader = _appEnvironment.AssetReader;
            assetReader.MakeMonoScriptNameTable(fileInst);
            SetEntryInfosCallBack(parsedAssets.Select(x =>
                new ParsedAssetAndEntry()
                {
                    parsedAsset = x,
                    assetEntryInfo = assetReader.ReadEntryInfoFromAsset(x),
                    realBundlePath = brePath
                }
            ).ToList());
            _appEnvironment.Dispatcher.Dispatch(new GoBundleViewEvent(EntryInfos));
        }
    }
}