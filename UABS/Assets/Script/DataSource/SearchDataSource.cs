using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Reader.Search;
using UABS.Assets.Script.Writer.SystemCache;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Misc.Paths;

namespace UABS.Assets.Script.DataSource
{
    public class SearchDataSource : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;
        private ReadSearchInfo _readSearchInfo;
        private CopyDeriveToSysFolder _copyDeriveToSysFolder = new();

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _readSearchInfo = new(appEnvironment.Wrapper.JsonSerializer);
        }

        public void OnEvent(AppEvent e)
        {
            if (e is RequestSearchEvent rse)
            {
                List<DeriveInfo> searchInfos = _readSearchInfo.ReadInfoFor(rse.ReadFromCachePath, rse.SearchKeywords, rse.ExcludeKeywords, rse.ExactMatch, rse.SearchMemo);
                Debug.Log("SEARCH FOUND:");
                foreach (DeriveInfo searchInfo in searchInfos)
                {
                    Debug.Log($"{searchInfo.name}, {searchInfo.cabCode}, {searchInfo.path}");
                }

                List<string> searchPaths = searchInfos.Select(x => x.path).ToList();
                string previewFolderPath = _copyDeriveToSysFolder.CopyFromPaths(searchPaths, PredefinedPaths.ExternalSystemSearchCache);

                AppEnvironment.Dispatcher.Dispatch(new FolderRead4DeriveEvent(previewFolderPath, searchInfos, "")
                {
                    from="SearchDataSource"
                });
            }
        }
    }
}