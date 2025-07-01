using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.ScriptableObjects;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.EventListener;

namespace UABS.Assets.Script.View
{
    public class EntryInfoView : MonoBehaviour, IAppEventListener
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

        private AssetTextInfo _storedAssetInfo;

        public EventDispatcher dispatcher = null;
        private Scrollbar _scrollbarRef;

        private int _index;
        private int _totalEntryNum;

        public void TriggerEvent()
        {
            if (dispatcher != null)
            {
                if (_assetType2IconData != null)
                {
                    dispatcher.Dispatch(new AssetTextInfoEvent(_storedAssetInfo));
                    dispatcher.Dispatch(new AssetSelectionEvent(_storedAssetInfo.pathID, _index, _totalEntryNum, isHoldingShift: IsShiftHeld()));
                }
            }
            else
            {
                throw new System.Exception("Entry Info View missing dispatcher. Please assign first.");
            }
        }

        public void AssignStuff(int index, int totalEntryNum, Scrollbar scrollbar)
        {
            _index = index;
            _totalEntryNum = totalEntryNum;
            _scrollbarRef = scrollbar;
        }

        public void Render(AssetTextInfo assetTextInfo, bool isHighlighted=false)
        {
            _storedAssetInfo = assetTextInfo;
            if (!isHighlighted)
            {
                _bg.color = _index % 2 == 0 ? _alternateColor1 : _alternateColor2;
            }
            else
            {
                _bg.color = _highlightColor;
            }
            AssetClassID assetType = assetTextInfo.type;
            long pathID = assetTextInfo.pathID;
            string name = assetTextInfo.name;

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

        public void OnEvent(AppEvent e)
        {
            if (e is AssetSelectionEvent ase)
            {
                if (_storedAssetInfo.pathID == ase.AssetSelectionInfo.pathID)
                {
                    if (ase.UseJump)
                        Jump2Me();
                    dispatcher.Dispatch(new AssetTextInfoEvent(_storedAssetInfo));
                }
            }
        }

        private void Jump2Me()
        {
            float newScrollbarValue = 1 - _index / (float)_totalEntryNum;
            if (_index == _totalEntryNum - 1)
                newScrollbarValue = 0;
            _scrollbarRef.value = newScrollbarValue;
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
    }
}