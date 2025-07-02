using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataSource.Manager;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;
using UnityEngine;

namespace UABS.Assets.Script.DataSource
{
    public class AssetsDataSource : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;
        public List<ParsedAssetAndEntry> _entryInfos;
        public AssetParser _assetParser;
        public AssetsDataExportManager _exportManager;

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
            else if (e is SortScrollViewEvent ssve)
            {
                SortByType sortByType = ssve.SortProp.sortByType;
                SortOrder sortOrder = ssve.SortProp.sortOrder;
                _entryInfos = SortedEntryInfos(sortByType, sortOrder);
                _appEnvironment.Dispatcher.Dispatch(new GoBundleViewEvent(_entryInfos));
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
            AssetReader assetReader = AppEnvironment.AssetReader;
            assetReader.MakeMonoScriptNameTable(fileInst);
            _entryInfos = parsedAssets.Select(x =>
                new ParsedAssetAndEntry()
                {
                    parsedAsset = x,
                    assetEntryInfo = assetReader.ReadEntryInfoFromAsset(x),
                    realBundlePath = brePath
                }
            ).ToList();
            _appEnvironment.Dispatcher.Dispatch(new GoBundleViewEvent(_entryInfos));
        }

        private List<ParsedAssetAndEntry> SortedEntryInfos(SortByType sortByType, SortOrder sortOrder)
        {
            if (sortByType == SortByType.Name)
            {
                if (sortOrder == SortOrder.Down)
                {
                    _entryInfos.Sort((a, b) => NaturalCompare(a.assetEntryInfo.name, b.assetEntryInfo.name));
                    return _entryInfos;
                }
                else if (sortOrder == SortOrder.Up)
                {
                    _entryInfos.Sort((b, a) => NaturalCompare(a.assetEntryInfo.name, b.assetEntryInfo.name));
                    return _entryInfos;
                }
            }
            else if (sortByType == SortByType.Type)
            {
                if (sortOrder == SortOrder.Down)
                {
                    return _entryInfos.OrderBy(x => x.assetEntryInfo.classID).ToList();
                }
                else if (sortOrder == SortOrder.Up)
                {
                    return _entryInfos.OrderByDescending(x => x.assetEntryInfo.classID).ToList();
                }
            }
            else if (sortByType == SortByType.PathID)
            {
                if (sortOrder == SortOrder.Down)
                {
                    return _entryInfos.OrderBy(x => x.assetEntryInfo.pathID).ToList();
                }
                else if (sortOrder == SortOrder.Up)
                {
                    return _entryInfos.OrderByDescending(x => x.assetEntryInfo.pathID).ToList();
                }
            }
            return _entryInfos;
        }

        private static int NaturalCompare(string a, string b)
        {
            var regex = new Regex(@"\d+|\D+");
            var matchesA = regex.Matches(a);
            var matchesB = regex.Matches(b);
            int i = 0;

            while (i < matchesA.Count && i < matchesB.Count)
            {
                var chunkA = matchesA[i].Value;
                var chunkB = matchesB[i].Value;

                if (int.TryParse(chunkA, out int numA) && int.TryParse(chunkB, out int numB))
                {
                    int cmp = numA.CompareTo(numB);
                    if (cmp != 0) return cmp;
                }
                else
                {
                    int cmp = string.Compare(chunkA, chunkB, StringComparison.OrdinalIgnoreCase);
                    if (cmp != 0) return cmp;
                }

                i++;
            }

            return matchesA.Count.CompareTo(matchesB.Count);
        }

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _assetParser = new(_appEnvironment.AssetsManager);
            _exportManager = new(_appEnvironment.AssetsManager)
            {
                EntryInfosCallBack = () => _entryInfos
            };
            _appEnvironment.Dispatcher.Register(_exportManager);
        }
    }
}