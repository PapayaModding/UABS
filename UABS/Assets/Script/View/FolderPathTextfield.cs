using System.IO;
using TMPro;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UnityEngine;

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
            else if (e is FolderRead4DependencyEvent fr4d)
            {
                _pathTextfield.text = $"You are viewing dependencies for [{Path.GetFileName(_lastBundlePath)}].";
                // _pathTextfield.text = fr4d.FolderPath;
            }
            else if (e is BundleReadEvent bre)
            {
                _lastBundlePath = bre.FilePath;
            }
            else if (e is BundleRead4DependencyEvent br4d)
            {
                _lastBundlePath = br4d.FilePath;
            }
        }
    }
}