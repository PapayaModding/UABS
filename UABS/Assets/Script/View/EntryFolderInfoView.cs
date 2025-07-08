using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.ScriptableObjects;
using UABS.Assets.Script.Misc;

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

        [SerializeField]
        private TextMeshProUGUI _size;

        [SerializeField]
        private RectTransform _sizeRectTransform;

        private FolderViewInfo _storedFolderViewInfo;

        public EventDispatcher dispatcher = null;

        private int _index;

        public void TriggerEvent()
        {
            if (dispatcher != null)
            {
                if (!_storedFolderViewInfo.IsDeriveFolder)
                {
                    string nextPath = _storedFolderViewInfo.path;
                    dispatcher.Dispatch(new FolderReadEvent(nextPath));
                }
                else
                {
                    string nextPath = _storedFolderViewInfo.path;
                    dispatcher.Dispatch(new FolderRead4DeriveEvent(nextPath, overrideBundlePath: _storedFolderViewInfo.overrideDerivePath));
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
            if (folderViewInfo.size == 0)
            {
                _sizeRectTransform.gameObject.SetActive(false);
            }
            else
            {
                string size = FileSize.FormatFileSize(folderViewInfo.size);
                _size.text = size;
            }

            _icon.sprite = _folderViewType2IconData.GetIcon(folderViewType);
            _name.text = name;
        }
    }
}