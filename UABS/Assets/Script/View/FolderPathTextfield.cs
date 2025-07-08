using System.IO;
using UnityEngine;
using TMPro;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;

namespace UABS.Assets.Script.View
{
    public class FolderPathTextfield : MonoBehaviour, IAppEventListener
    {
        [SerializeField]
        private TMP_InputField _pathTextfield;

        private string _lastBundlePath;

        private void Start()
        {
            _pathTextfield.readOnly = true;
        }

        public void OnEvent(AppEvent e)
        {
            if (e is FolderReadEvent fre)
            {
                _pathTextfield.text = fre.FolderPath;
            }
            else if (e is BundleReadEvent bre)
            {
                _lastBundlePath = bre.FilePath;
            }
            else if (e is BundleRead4DeriveEvent br4d)
            {
                _lastBundlePath = br4d.FilePath;
            }
            else if (e is RequestDependencyEvent)
            {
                _pathTextfield.text = $"You are viewing dependencies for [{Path.GetFileName(_lastBundlePath)}].";
            }
            else if (e is RequestSearchEvent rse)
            {
                _pathTextfield.text = $"You are viewing search results for [{rse.SearchKeywords}], excluding [{rse.ExcludeKeywords}].";
            }
        }
    }
}