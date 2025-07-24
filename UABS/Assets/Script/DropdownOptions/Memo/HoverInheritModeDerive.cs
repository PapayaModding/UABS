using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.LocalController;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.UI.OptionPanel;

namespace UABS.Assets.Script.DropdownOptions.Memo
{
    public class HoverInheritModeDerive : HoverArea, IAppEnvironment, IAppEventListener
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        [SerializeField]
        private GameObject _entryPrefab;

        [SerializeField]
        private RectTransform _content;

        [SerializeField]
        private Button _button;

        private List<IMemoInheritModeEntry> _memoInheritEntries = new();

        private MemoInheritMode _currSelectedMode = MemoInheritMode.Safe;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            foreach (MemoInheritMode mode in Enum.GetValues(typeof(MemoInheritMode)))
            {
                GameObject entry = CreateScrollEntry(mode, mode == MemoInheritMode.Safe);
                entry.GetComponent<RectTransform>().SetParent(_content.transform, worldPositionStays: false);
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            _button.targetGraphic.color = _button.colors.selectedColor;
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            _button.targetGraphic.color = _button.colors.normalColor;
        }

        private GameObject CreateScrollEntry(MemoInheritMode mode, bool interactable)
        {
            GameObject entry = Instantiate(_entryPrefab);
            IMemoInheritModeEntry menuScrollEntry = entry.GetComponentsInChildren<MonoBehaviour>(true)
                                                .OfType<IMemoInheritModeEntry>()
                                                .FirstOrDefault();
            menuScrollEntry.MemoInheritMode = mode;
            menuScrollEntry.AssignDispatcher(AppEnvironment.Dispatcher);
            menuScrollEntry.IsSelected = menuScrollEntry.MemoInheritMode == _currSelectedMode;
            _appEnvironment.Dispatcher.Register((MemoInheritButton) menuScrollEntry);
            _memoInheritEntries.Add(menuScrollEntry);

            return entry;
        }

        public void OnEvent(AppEvent e)
        {
            if (e is ClickInheritMemoEvent cmie)
            {
                _currSelectedMode = cmie.MemoInheritMode;

                foreach (IMemoInheritModeEntry entry in _memoInheritEntries)
                {
                    if (entry.MemoInheritMode != _currSelectedMode)
                        entry.IsSelected = false;
                }
                _appEnvironment.Dispatcher.Dispatch(new MemoInheritEvent(_currSelectedMode));
            }
        }
    }
}