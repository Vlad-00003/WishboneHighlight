using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

namespace WishboneHighlight
{
    internal class WishboneHighlightIndicator : MonoBehaviour
    {
        public static Beacon CurrentBeacon;

        private Texture2D _arrowTexture;
        private GUIStyle _labelStyle;
        private Beacon _cachedBeacon;
        private string _cachedBeaconName;

        private void OnGUI()
        {
            if (!WishboneHighlight.Instance || !WishboneHighlight.Instance.IsEnabled)
                return;
            if (!CurrentBeacon)
            {
                _cachedBeacon = null;
                _cachedBeaconName = null;
                return;
            }
            if (Event.current.type != EventType.Repaint)
                return;

            if (_cachedBeacon != CurrentBeacon)
            {
                var hoverable = CurrentBeacon.GetComponentInParent<Hoverable>();
                if (hoverable != null)
                {
                    var hoverName = hoverable.GetHoverName();
                    _cachedBeaconName = hoverName.StartsWith("$")
                        ? Localization.instance.Localize(hoverName)
                        : hoverName;
                }
                else
                    _cachedBeaconName = "Unknown";

                _cachedBeacon = CurrentBeacon;
            }

            Color previousColor = GUI.color;
            Matrix4x4 previousMatrix = GUI.matrix;
            try
            {
                GUI.matrix = Matrix4x4.identity;
                DrawIndicator(_cachedBeaconName);
            }
            finally
            {
                GUI.color = previousColor;
                GUI.matrix = previousMatrix;
            }
        }

        private void DrawIndicator(string beaconName)
        {
            var player = Player.m_localPlayer;
            if (player == null)
                return;
            Camera camera = Camera.main;
            if (!camera)
                return;

            float distance = Utils.DistanceXZ(player.transform.position, CurrentBeacon.transform.position);
            string distanceText = distance.ToString("0.0") + " m";
            float signalStrength = CurrentBeacon.m_range > 0f
                ? Mathf.Clamp01(distance / CurrentBeacon.m_range)
                : 0f;
            Vector3 screenPosition = camera.WorldToScreenPoint(CurrentBeacon.transform.position);
            float markerSize = Mathf.Max(0f, WishboneHighlight.Instance.WorldMarkerSize)
                * Mathf.Lerp(0.7f, 1f, signalStrength);
            Rect markerRect = new Rect(screenPosition.x - markerSize * 0.5f,
                Screen.height - screenPosition.y - markerSize * 0.5f, markerSize, markerSize);
            bool markerOnScreen = screenPosition.z >= camera.nearClipPlane
                && new Rect(0f, 0f, Screen.width, Screen.height).Overlaps(markerRect);

            if (markerOnScreen && markerSize > 0f)
            {
                GUI.color = WishboneHighlight.Instance.WorldMarkerColor;
                GUI.DrawTexture(markerRect, Texture2D.whiteTexture);
                DrawIndicatorLabels(markerRect, beaconName, distanceText);
            }

            DrawHud(camera, markerOnScreen, beaconName, distanceText);
        }

        private void DrawHud(Camera camera, bool markerOnScreen, string beaconName, string distanceText)
        {
            if (!WishboneHighlight.Instance.ArrowAlwaysVisible && markerOnScreen)
                return;

            float size = Mathf.Max(0f, WishboneHighlight.Instance.ArrowSize);
            if (size <= 0f)
                return;
            EnsureArrowTexture();

            Vector3 localDirection = camera.transform.InverseTransformDirection(
                CurrentBeacon.transform.position - camera.transform.position);
            Vector2 direction = new Vector2(localDirection.x, localDirection.y);
            // A target directly ahead or behind has no direction in the camera's image plane.
            if (direction.sqrMagnitude < 0.0001f)
                direction = localDirection.z >= 0f ? Vector2.up : Vector2.down;

            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            // Reserve the square's diagonal so rotation cannot move the pivot or labels.
            float extent = size * Mathf.Sqrt(2f);
            Vector2 pivot = new Vector2(Screen.width * 0.5f,
                Mathf.Max(0f, WishboneHighlight.Instance.ArrowOffset) + extent * 0.5f);
            GUI.color = WishboneHighlight.Instance.ArrowColor;
            Matrix4x4 previousMatrix = GUI.matrix;
            try
            {
                GUIUtility.RotateAroundPivot(angle, pivot);
                GUI.DrawTexture(new Rect(pivot.x - size * 0.5f, pivot.y - size * 0.5f, size, size),
                    _arrowTexture);
            }
            finally
            {
                GUI.matrix = previousMatrix;
            }
            DrawIndicatorLabels(new Rect(pivot.x - extent * 0.5f, pivot.y - extent * 0.5f,
                extent, extent), beaconName, distanceText);
        }

        private void DrawIndicatorLabels(Rect indicatorRect, string beaconName, string distanceText)
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    richText = false,
                    wordWrap = true,
                    padding = new RectOffset(0, 0, 0, 0)
                };
                _labelStyle.normal.textColor = Color.white;
            }

            const float gap = 4f;
            DrawLabel(beaconName, indicatorRect.center.x, indicatorRect.yMin - gap, true);
            DrawLabel(distanceText, indicatorRect.center.x, indicatorRect.yMax + gap, false);
        }

        private void DrawLabel(string text, float centerX, float edgeY, bool above)
        {
            GUIContent content = new GUIContent(text);
            float maxWidth = Mathf.Max(1f, Screen.width - 8f);
            float width = Mathf.Clamp(_labelStyle.CalcSize(content).x + 4f, 1f, maxWidth);
            float height = _labelStyle.CalcHeight(content, width);
            Rect rect = new Rect(
                Mathf.Clamp(centerX - width * 0.5f, 4f, Screen.width - width - 4f),
                Mathf.Clamp(above ? edgeY - height : edgeY, 0f, Mathf.Max(0f, Screen.height - height)),
                width, height);
            Color previousColor = GUI.color;
            GUI.color = Color.black;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), content, _labelStyle);
            GUI.color = Color.white;
            GUI.Label(rect, content, _labelStyle);
            GUI.color = previousColor;
        }

        private void EnsureArrowTexture()
        {
            if (_arrowTexture)
                return;

            const int resolution = 32;
            _arrowTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color[] pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float centerDistance = Mathf.Abs(x - (resolution - 1) * 0.5f);
                    bool head = y >= resolution / 2 && centerDistance <= resolution - 1 - y;
                    bool shaft = y < resolution / 2 && centerDistance < resolution / 8f;
                    pixels[y * resolution + x] = head || shaft ? Color.white : Color.clear;
                }
            }
            _arrowTexture.SetPixels(pixels);
            _arrowTexture.Apply(false, true);
        }

        private void OnDestroy()
        {
            if (_arrowTexture)
                Destroy(_arrowTexture);
        }
    }
}
