using System.IO;
using UnityEngine;
using TMPro;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Writer;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.LocalController
{
    public class RunInheritMemoButton : MonoBehaviour, IAppEnvironment, IAppEventListener
    {
        [SerializeField]
        private TMP_InputField _fromPackagePathField;

        [SerializeField]
        private TMP_InputField _toPackagePathField;

        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        private InheritMemoWriter _inheritMemoWriter;

        private MemoInheritMode _memoInheritMode = MemoInheritMode.Safe;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _inheritMemoWriter = new(appEnvironment);
        }

        public void ClickButton()
        {
            string from = _fromPackagePathField.text;
            string to = _toPackagePathField.text;

            if (string.IsNullOrEmpty(from))
            {
                Debug.LogWarning("[FROM] path is empty");
                return;
            }

            if (string.IsNullOrEmpty(to))
            {
                Debug.LogWarning("[TO] path is empty");
                return;
            }

            if (!Directory.Exists(from))
            {
                Debug.LogWarning($"[FROM]: {from} doesn't exist");
            }

            if (!Directory.Exists(to))
            {
                Debug.LogWarning($"[TO]: {to} doesn't exist");
            }

            _inheritMemoWriter.InheritMemoPackage(from, to, _memoInheritMode);
        }

        public void OnEvent(AppEvent e)
        {
            if (e is MemoInheritEvent mie)
            {
                _memoInheritMode = mie.MemoInheritMode;
            }
        }
    }
}