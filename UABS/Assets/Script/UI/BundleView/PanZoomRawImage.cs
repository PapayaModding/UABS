using UnityEngine;
using UnityEngine.EventSystems;

namespace UABS.Assets.Script.View.BundleView
{
    public class PanZoomRawImage : MonoBehaviour, IPointerDownHandler, IDragHandler, IScrollHandler
    {
        public RectTransform imageRect;
        public RectTransform viewportRect; // The visible mask or panel
        public float zoomSpeed = 0.1f;
        public float minZoom = 0.5f;
        public float maxZoom = 3f;

        private Vector2 lastMousePosition;

        public void OnPointerDown(PointerEventData eventData)
        {
            lastMousePosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 delta = eventData.position - lastMousePosition;
            imageRect.anchoredPosition += delta;
            lastMousePosition = eventData.position;

            ClampPosition();
        }

        public void OnScroll(PointerEventData eventData)
        {
            float scroll = eventData.scrollDelta.y;
            float currentScale = imageRect.localScale.x;
            float newScale = Mathf.Clamp(currentScale + scroll * zoomSpeed, minZoom, maxZoom);
            imageRect.localScale = Vector3.one * newScale;

            ClampPosition();
        }

        void ClampPosition()
        {
            // Get scaled size of image
            Vector2 imageSize = Vector2.Scale(imageRect.rect.size, imageRect.localScale);
            Vector2 viewportSize = viewportRect.rect.size;

            Vector2 maxOffset = (imageSize - viewportSize) / 2f;
            maxOffset.x = Mathf.Max(maxOffset.x, 0);
            maxOffset.y = Mathf.Max(maxOffset.y, 0);

            Vector2 clampedPos = imageRect.anchoredPosition;
            clampedPos.x = Mathf.Clamp(clampedPos.x, -maxOffset.x, maxOffset.x);
            clampedPos.y = Mathf.Clamp(clampedPos.y, -maxOffset.y, maxOffset.y);

            imageRect.anchoredPosition = clampedPos;
        }
    }
}