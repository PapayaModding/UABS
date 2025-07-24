using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UABS.Assets.Script.UI.BundleView
{
    public class LongPressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float triggeringDelay = 1.0f;
        public float repeatRate = 0.1f;
        public UnityEvent onLongPress;
        public UnityEvent onClick;

        private bool _isPointerDown = false;
        private float _pointerDownTimer = 0f;

        private float _holdTime = 0f;
        private float _nextInvokeTime = 0f;
        private bool _repeating = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPointerDown = true;
            _pointerDownTimer = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isPointerDown && _pointerDownTimer > triggeringDelay)
            {
                onClick?.Invoke(); // If released before duration, treat as normal click
            }
            Reset();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Reset(); // Cancel if pointer leaves button
        }

        private void Update()
        {
            if (!_isPointerDown)
            return;

            _holdTime += Time.deltaTime;
            _pointerDownTimer += Time.deltaTime;

            if (_holdTime >= _nextInvokeTime)
            {
                onLongPress?.Invoke();

                if (!_repeating)
                {
                    // First time triggered
                    _repeating = true;
                    _nextInvokeTime = _holdTime + repeatRate;
                }
                else
                {
                    // Continue repeating
                    _nextInvokeTime += repeatRate;
                }
            }
        }

        private void Reset()
        {
            _isPointerDown = false;
            _pointerDownTimer = 0f;
        }
    }
}