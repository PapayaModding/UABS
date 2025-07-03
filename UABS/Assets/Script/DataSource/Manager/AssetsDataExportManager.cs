using System;
using System.Collections.Generic;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Writer;

namespace UABS.Assets.Script.DataSource.Manager
{
    public class AssetsDataExportManager: IAppEventListener
    {
        private WriteTextureAsImage2Path _writeTextureAsImage2Path;
        
        public Func<List<ParsedAssetAndEntry>> EntryInfosCallBack;

        private List<ParsedAssetAndEntry> EntryInfos => EntryInfosCallBack != null ? EntryInfosCallBack() : new();

        public AssetsDataExportManager(AppEnvironment appEnvironment)
        {
            _writeTextureAsImage2Path = new(appEnvironment.AssetsManager, appEnvironment.Wrapper.TextureDecoder);
        }

        public void OnEvent(AppEvent e)
        {
            if (e is ExportAssetsEvent eae)
            {
                ExportMethod exportMethod = eae.ExportMethod;
                if (exportMethod.exportType == ExportType.All)
                {
                    _writeTextureAsImage2Path.ExportAllAssetsToPath(exportMethod, EntryInfos);
                }
            }
        }
    }
}