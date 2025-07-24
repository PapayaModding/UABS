using UnityEngine;
using UnityEngine.UI;
using AssetsTools.NET.Extra;
using TMPro;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.Misc.ScriptableObjects;

namespace UABS.Assets.Script.View.BundleView
{
    public class EntryInfoView : MonoBehaviour
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
        private AssetType2IconData _assetType2IconData;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TextMeshProUGUI _name;

        [SerializeField]
        private TextMeshProUGUI _type;

        [SerializeField]
        private TextMeshProUGUI _pathID;

        [SerializeField]
        private RectTransform _rectTransform;

        private ParsedAssetAndEntry _currEntryInfo = null;

        public EventDispatcher dispatcher = null;

        private int _index;
        public int Index => _index;
        private int _totalEntryNum;

        public void TriggerEvent()
        {
            if (dispatcher != null)
            {
                if (_assetType2IconData != null && _currEntryInfo != null)
                {
                    dispatcher.Dispatch(new AssetDisplayInfoEvent(_currEntryInfo));
                    dispatcher.Dispatch(new AssetSelectionEvent(_currEntryInfo.assetEntryInfo.pathID,
                                                                _index,
                                                                _totalEntryNum,
                                                                IsShiftHeld(),
                                                                IsCtrlHeld()));
                }
            }
            else
            {
                throw new System.Exception("Entry Info View missing dispatcher. Please assign first.");
            }
        }

        public void AssignStuff(int index, int totalEntryNum)
        {
            _index = index;
            _totalEntryNum = totalEntryNum;
        }

        public void Render(ParsedAssetAndEntry entryInfo)
        {
            _currEntryInfo = entryInfo;
            AssetEntryInfo assetEntryInfo = entryInfo.assetEntryInfo;

            if (!entryInfo.isHighlighted)
            {
                _bg.color = _index % 2 == 0 ? _alternateColor1 : _alternateColor2;
            }
            else
            {
                _bg.color = _highlightColor;
            }
            AssetClassID assetType = assetEntryInfo.classID;
            long pathID = assetEntryInfo.pathID;
            string name = assetEntryInfo.name;

            _icon.sprite = _assetType2IconData.GetIcon(assetType);
            _name.text = name;

            int classIdInt = (int)assetType;
            string className = "";

            if (System.Enum.IsDefined(typeof(AssetClassID), classIdInt))
            {
                className = ((AssetClassID)classIdInt).ToString();
            }
            else
            {
                Debug.LogWarning($"Unknown AssetClassID: {classIdInt}");
            }

            _type.text = className;
            _pathID.text = pathID.ToString();
        }

        public void Hide()
        {
            _rectTransform.gameObject.SetActive(false);
        }

        public void Show()
        {
            _rectTransform.gameObject.SetActive(true);
        }

        public void SetPosition(Vector2 newPosition)
        {
            _rectTransform.anchoredPosition = newPosition;
        }

        private static bool IsShiftHeld()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }
        
        private static bool IsCtrlHeld()
        {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }
    }
}