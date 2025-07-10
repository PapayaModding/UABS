using UnityEngine;
using TMPro;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using System.IO;
using UABS.Assets.Script.Misc;

namespace UABS.Assets.Script.Debugger
{
    public class AppStateTracker : MonoBehaviour, IAppEventListener
    {
        private enum AppState
        {
            FolderView,
            BundleView,
            BundleViewDerive,
            Dependency,
            Search
        }

        [SerializeField]
        private TextMeshProUGUI _appStateText;

        private void ChangeState(AppState appState)
        {
            if (_appStateText.gameObject.activeSelf)
            {
                _appStateText.text = appState.ToString();
            }
        }

        public void OnEvent(AppEvent e)
        {
            if (e is BundleReadEvent bre)
            {
                if (Directory.Exists(bre.FilePath))
                {
                    TreatAsPath(bre.FilePath);
                }
                else
                {
                    ChangeState(AppState.BundleView);
                }
            }
            else if (e is BundleRead4DeriveEvent br4d)
            {
                if (Directory.Exists(br4d.FilePath))
                {
                    TreatAsPath(br4d.FilePath);
                }
                else
                {
                    ChangeState(AppState.BundleViewDerive);
                }
            }
            else if (e is FolderReadEvent fre)
            {
                if (Directory.Exists(fre.FolderPath))
                {
                    TreatAsPath(fre.FolderPath);
                }
                else
                {
                    ChangeState(AppState.BundleView);
                }
            }
            else if (e is FolderRead4DeriveEvent fr4d)
            {
                if (Directory.Exists(fr4d.FolderPath))
                {
                    TreatAsPath(fr4d.FolderPath);
                }
                else
                {
                    ChangeState(AppState.BundleViewDerive);
                }
            }
        }

        private void TreatAsPath(string path)
        {
            if (PathUtils.PathStartsWith(path, PredefinedPaths.ExternalSystemDependenceCache))
            {
                ChangeState(AppState.Dependency);
            }
            else if (PathUtils.PathStartsWith(path, PredefinedPaths.ExternalSystemSearchCache))
            {
                ChangeState(AppState.Search);
            }
            else
            {
                ChangeState(AppState.FolderView);
            }
        }
    }
}