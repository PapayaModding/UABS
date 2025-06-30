using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;
using UABS.Assets.Script.Writer;
using UnityEngine;

namespace UABS.Assets.Script.DataSource
{
    public class AssetsDataSource : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        private BundleFileInstance _currBunInst;
        private List<AssetDisplayInfo> _assetsDisplayInfo = new();
        private ReadTextInfoFromBundle _readTextInfoFromBundle;
        private WriteTextureAsImage2Path _writeTextureAsImage2Path;
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        public void OnEvent(AppEvent e)
        {
            if (e is BundleRead4DependencyEvent br4d)
            {
                _currBunInst = br4d.Bundle;
                _assetsDisplayInfo = new();
                List<AssetTextInfo> assetsTextInfo = _readTextInfoFromBundle.ReadAllBasicInfo(_currBunInst);
                foreach (AssetTextInfo assetTextInfo in assetsTextInfo)
                {
                    AssetTextInfo newAssetTextInfo = new()
                    {
                        path = string.IsNullOrWhiteSpace(br4d.RealFilPath) ? assetTextInfo.path : br4d.RealFilPath,
                        name = assetTextInfo.name,
                        pathID = assetTextInfo.pathID,
                        fileID = assetTextInfo.fileID,
                        uncompressedSize = assetTextInfo.uncompressedSize,
                        compressedSize = assetTextInfo.compressedSize,
                        type = assetTextInfo.type
                    };
                    _assetsDisplayInfo.Add(new()
                    {
                        assetTextInfo = newAssetTextInfo
                    });
                }
                _appEnvironment.Dispatcher.Dispatch(new AssetsDisplayInfoEvent(_assetsDisplayInfo));
            }
            else if (e is BundleReadEvent bre)
            {
                _currBunInst = bre.Bundle;
                _assetsDisplayInfo = new();
                List<AssetTextInfo> assetsTextInfo = _readTextInfoFromBundle.ReadAllBasicInfo(_currBunInst);
                foreach (AssetTextInfo assetTextInfo in assetsTextInfo)
                {
                    _assetsDisplayInfo.Add(new()
                    {
                        assetTextInfo = assetTextInfo
                    });
                }
                _appEnvironment.Dispatcher.Dispatch(new AssetsDisplayInfoEvent(_assetsDisplayInfo));
            }
            else if (e is SortScrollViewEvent ssve)
            {
                SortByType sortByType = ssve.SortProp.sortByType;
                SortOrder sortOrder = ssve.SortProp.sortOrder;
                _assetsDisplayInfo = SortedAssetsDisplayInfo(sortByType, sortOrder);
                _appEnvironment.Dispatcher.Dispatch(new AssetsDisplayInfoEvent(_assetsDisplayInfo, false));
            }
            else if (e is ExportAssetsEvent eae)
            {
                ExportMethod exportMethod = eae.ExportMethod;
                if (exportMethod.exportType == ExportType.All)
                    _writeTextureAsImage2Path.ExportAllAssetsToPath(exportMethod, _assetsDisplayInfo, _currBunInst);
            }
        }

        private List<AssetDisplayInfo> SortedAssetsDisplayInfo(SortByType sortByType, SortOrder sortOrder)
        {
            if (sortByType == SortByType.Name)
            {
                if (sortOrder == SortOrder.Down)
                {
                    _assetsDisplayInfo.Sort((a, b) => NaturalCompare(a.assetTextInfo.name, b.assetTextInfo.name));
                    return _assetsDisplayInfo;
                }
                else if (sortOrder == SortOrder.Up)
                {
                    _assetsDisplayInfo.Sort((b, a) => NaturalCompare(a.assetTextInfo.name, b.assetTextInfo.name));
                    return _assetsDisplayInfo;
                }
            }
            else if (sortByType == SortByType.Type)
            {
                if (sortOrder == SortOrder.Down)
                {
                    return _assetsDisplayInfo.OrderBy(x => x.assetTextInfo.type).ToList();
                }
                else if (sortOrder == SortOrder.Up)
                {
                    return _assetsDisplayInfo.OrderByDescending(x => x.assetTextInfo.type).ToList();
                }
            }
            else if (sortByType == SortByType.PathID)
            {
                if (sortOrder == SortOrder.Down)
                {
                    return _assetsDisplayInfo.OrderBy(x => x.assetTextInfo.pathID).ToList();
                }
                else if (sortOrder == SortOrder.Up)
                {
                    return _assetsDisplayInfo.OrderByDescending(x => x.assetTextInfo.pathID).ToList();
                }
            }
            return _assetsDisplayInfo;
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
            _readTextInfoFromBundle = new(_appEnvironment.AssetsManager);
            _writeTextureAsImage2Path = new(_appEnvironment.AssetsManager);
        }

        public AssetDisplayInfo GetDisplayInfoAtIndex(int index)
        {
            return _assetsDisplayInfo[index];
        }
    }
}