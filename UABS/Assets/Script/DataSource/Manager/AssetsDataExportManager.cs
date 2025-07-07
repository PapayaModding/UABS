using System;
using System.Collections.Generic;
using System.Linq;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Writer;

namespace UABS.Assets.Script.DataSource.Manager
{
    public class AssetsDataExportManager : IAppEventListener
    {
        private readonly WriteTextureAsImage2Path _writeTextureAsImage2Path;

        public Func<List<ParsedAssetAndEntry>> EntryInfosCallBack;

        private List<ParsedAssetAndEntry> EntryInfos => EntryInfosCallBack != null ? EntryInfosCallBack() : new();

        private Dictionary<AssetClassID, bool> _isClassIDFiltered = new();

        public AssetsDataExportManager(AppEnvironment appEnvironment)
        {
            _writeTextureAsImage2Path = new(appEnvironment.AssetsManager, appEnvironment.Wrapper.TextureDecoder);
        }

        public void OnEvent(AppEvent e)
        {
            if (e is ExportAssetsEvent eae)
            {
                ExportMethod exportMethod = eae.ExportMethod;
                if (exportMethod.exportKind == ExportKind.Asset && exportMethod.exportType == ExportType.All)
                {
                    _writeTextureAsImage2Path.ExportAssetsToPath(exportMethod, EntryInfos);
                }
                else if (exportMethod.exportKind == ExportKind.Asset && exportMethod.exportType == ExportType.FilterByType)
                {
                    _writeTextureAsImage2Path.ExportAssetsToPath(exportMethod, FilterEntryInfoByType(EntryInfos));
                }
                else if (exportMethod.exportKind == ExportKind.Asset && exportMethod.exportType == ExportType.Selected)
                {
                    _writeTextureAsImage2Path.ExportAssetsToPath(exportMethod, FilterEntryInfoByHighlight(EntryInfos));
                }
            }
            else if (e is FilterTypeEvent fte)
            {
                _isClassIDFiltered = fte.IsClassIDFiltered;
            }
        }

        private List<ParsedAssetAndEntry> FilterEntryInfoByType(List<ParsedAssetAndEntry> entryInfos)
        {
            return entryInfos.Where(x => !_isClassIDFiltered.ContainsKey(x.assetEntryInfo.classID) ||
                                            (_isClassIDFiltered.ContainsKey(x.assetEntryInfo.classID) &&
                                            !_isClassIDFiltered[x.assetEntryInfo.classID])).ToList();
        }

        private List<ParsedAssetAndEntry> FilterEntryInfoByHighlight(List<ParsedAssetAndEntry> entryInfos)
        {
            return entryInfos.Where(x => x.isHighlighted).ToList();
        }
    }
}