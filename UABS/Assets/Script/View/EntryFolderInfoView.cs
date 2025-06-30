using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UABS.Assets.Script.ScriptableObjects;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.View
{
    public class EntryFolderInfoView : MonoBehaviour
    {
        [SerializeField]
        private Image _bg;

        [SerializeField]
        private Color _highlightColor;
        [SerializeField]
        private Color _alternateColor1;
        [SerializeField]
        private Color _alternateColor2;

        [SerializeField]
        private FolderViewType2IconData _folderViewType2IconData;  // Folder

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TextMeshProUGUI _name;

        private FolderViewInfo _storedFolderViewInfo;

        public EventDispatcher dispatcher = null;

        private int _index;

        public void TriggerEvent()
        {
            if (dispatcher != null)
            {
                if (!_storedFolderViewInfo.IsDependencyFolder)
                {
                    string nextPath = _storedFolderViewInfo.path;
                    dispatcher.Dispatch(new FolderReadEvent(nextPath));
                }
                else
                {
                    string nextPath = _storedFolderViewInfo.path;
                    dispatcher.Dispatch(new FolderRead4DependencyEvent(nextPath, overrideBundlePath: _storedFolderViewInfo.RealPath));
                }
            }
            else
            {
                throw new System.Exception("Entry Info View missing dispatcher. Please assign first.");
            }
        }

        public void AssignStuff(int index)
        {
            _index = index;
        }

        public void Render(FolderViewInfo folderViewInfo)
        {
            _storedFolderViewInfo = folderViewInfo;
            _bg.color = _index % 2 == 0 ? _alternateColor1 : _alternateColor2;
            FolderViewType folderViewType = folderViewInfo.type;
            string name = folderViewInfo.name;

            _icon.sprite = _folderViewType2IconData.GetIcon(folderViewType);
            _name.text = name;
        }
    }
}