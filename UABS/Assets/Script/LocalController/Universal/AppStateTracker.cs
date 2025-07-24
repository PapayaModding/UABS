using System.IO;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc.Paths;

namespace UABS.Assets.Script.LocalController.Universal
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

        [SerializeField]
        private UnityEvent _onAppStart;

        [SerializeField]
        private UnityEvent _onBundleView;

        [SerializeField]
        private UnityEvent _onFolderView;

        [SerializeField]
        private UnityEvent _onDependencyView;

        [SerializeField]
        private UnityEvent _onSearchView;

        [SerializeField]
        private UnityEvent _disableSearchCacheGoBack;

        [SerializeField]
        private UnityEvent _enableSearchCacheGoBack;

        private void Start()
        {
            _onAppStart.Invoke();
        }

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
                    _onBundleView.Invoke();
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
                    _onBundleView.Invoke();
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
                    _onBundleView.Invoke();
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
                    _onBundleView.Invoke();
                }
            }
            else if (e is ControlSearchGoBackEvent csc)
            {
                if (!csc.Enable)
                    _disableSearchCacheGoBack.Invoke();
                else
                    _enableSearchCacheGoBack.Invoke();
            }
        }

        private void TreatAsPath(string path)
        {
            if (PathUtils.PathStartsWith(path, PredefinedPaths.ExternalSystemDependencyCache))
            {
                ChangeState(AppState.Dependency);
                _onDependencyView.Invoke();
            }
            else if (PathUtils.PathStartsWith(path, PredefinedPaths.ExternalSystemSearchCache))
            {
                ChangeState(AppState.Search);
                _onSearchView.Invoke();
            }
            else
            {
                ChangeState(AppState.FolderView);
                _onFolderView.Invoke();
            }
        }
    }
}