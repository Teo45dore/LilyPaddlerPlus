using HarmonyLib;
using UnityEngine.EventSystems;

namespace LilyPaddlerPlus.Patches;

[HarmonyPatch(typeof(RectTransform))]
public static class RectTransformPatch {
    private static Dictionary<RectTransform, WobbleParams> wobbleParams = new Dictionary<RectTransform, WobbleParams>();

    private static float resetSpeed = 1f;
    private static float introSpeed = 0.8f;

    private static bool active;

    static RectTransformPatch() {
        HypnosisScreenFXControllerPatch.onStartDisorient += () => {
            HypnosisScreenFXControllerPatch.afterUpdate += UpdateWobble;
            active = true;
        };

        HypnosisScreenFXControllerPatch.onStopDisorient += () => {
            active = false;
        };
    }

    private static bool dontRecurse = false;
    [HarmonyPatch("anchoredPosition", MethodType.Setter)]
    [HarmonyPostfix]
    private static void SetAnchoredPosPostfix(RectTransform __instance) {
        if (dontRecurse || !wobbleParams.TryGetValue(__instance, out WobbleParams wobble)) return;

        dontRecurse = true;
        __instance.anchoredPosition += wobble.lastOffset - WobbleParams.GetOffsetFromParams(wobble);
        dontRecurse = false;
    }

    private static void UpdateWobble() {
        if (!active) {
            WobbleParams.scale = Mathf.Lerp(WobbleParams.scale, 0, resetSpeed * Time.deltaTime);
            if (WobbleParams.scale < 0.001f) {
                HypnosisScreenFXControllerPatch.afterUpdate -= UpdateWobble;
            }
        }

        if (active && ModConfig.WigglyIconsCfg.Value && WobbleParams.scale != ModConfig.IntensityCfg.Value) {
            WobbleParams.scale = Mathf.Lerp(WobbleParams.scale, ModConfig.IntensityCfg.Value, introSpeed * Time.deltaTime);
            if (Mathf.Abs(ModConfig.IntensityCfg.Value - WobbleParams.scale) < 0.001f) {
                WobbleParams.scale = ModConfig.IntensityCfg.Value;
            }
        }

        foreach (RectTransform wobbleTransform in wobbleParams.Keys) {
            if (wobbleTransform == null) continue;
            wobbleTransform.anchoredPosition = wobbleTransform.anchoredPosition;
        }
    }

    private class WobbleParams(float wiggleScale, float wiggleSpeed, bool wiggleDirection) {
        public float wiggleScale = wiggleScale * 20f;
        public float wiggleSpeed = wiggleSpeed;
        public bool wiggleDirection = wiggleDirection;
        /// <summary>
        /// The last offset that was requested with GetOffsetFromParams.
        /// </summary>
        public Vector2 lastOffset = Vector2.zero;
        private readonly float startTime = Time.time;

        public static float scale = 0;

        public static Vector2 GetOffsetFromParams(WobbleParams instance) {
            if (!active) {
                return instance.lastOffset = Vector2.zero;
            }

            float progress = (Time.time - instance.startTime) * instance.wiggleSpeed;
            float x = Mathf.Sin(progress) * 2 * instance.wiggleScale;
            float y = Mathf.Sin(progress * 2) * instance.wiggleScale;

            return instance.lastOffset = (instance.wiggleDirection == true ? new Vector2(x, y) : -new Vector2(x, y)) * Time.timeScale * scale;
        }

        /// <summary>
        /// Generates a instance of WobbleParams with random values mostly in the 0.5 - 1.3 range.
        /// </summary>
        public static WobbleParams random => new WobbleParams(Random.Range(0.6f, 1.3f), Random.Range(0.5f, 1.6f), Random.Range(0, 2) == 0);
    }

    [HarmonyPatch(typeof(UIBehaviour))]
    private static class UIBehaviourPatch {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void AwakePostfix(UIBehaviour __instance) {
            if (__instance is not uGUI_ItemIcon icon) {
                return;
            }

            wobbleParams.Remove((RectTransform)icon.transform);
            wobbleParams.Add((RectTransform)icon.transform, WobbleParams.random);
        }
    }
}












/*[HarmonyPatch("Init")]
    [HarmonyPostfix]
    private static void InitPostfix(uGUI_ItemIcon __instance) {
        if (__instance.transform.parent.GetComponentsInParent<uGUI_BlueprintsTab>().Length != 0 || __instance.transform.GetComponentsInParent<ItemIconPatchMarker>().Length != 0) {
            return;
        }

        // This replaces the icon's parent without changing the icon's position.
        // This allows us to change the icon's position without worrying about saving the old position.
        GameObject wobblePivot = new GameObject("Wobble Pivot", [typeof(RectTransform), typeof(ItemIconPatchMarker)]) {
            layer = LayerID.UI
        };

        wobblePivot.transform.SetParent(__instance.transform.parent, false);
        __instance.transform.SetParent(wobblePivot.transform, true);
        
        RectTransform wobbleTransform = wobblePivot.GetComponent<RectTransform>();
        RectTransform wobbleParentTransform = wobblePivot.transform.parent.GetComponent<RectTransform>();

        wobbleTransform.offsetMax = wobbleParentTransform.offsetMax;
        wobbleTransform.offsetMin = wobbleParentTransform.offsetMin;
        wobbleTransform.pivot = new Vector2(0.5f, 0.5f);
        wobbleTransform.anchoredPosition = Vector2.zero;

        wobbleParams.Remove(wobbleTransform);
        wobbleParams.Add(wobbleTransform, new WobbleParams(__instance.name == "Equipment Slot Icon" ? Random.Range(0.9f, 1.5f) : Random.Range(0.6f, 1.1f), Random.Range(0.5f, 1.6f), Random.Range(0, 2) == 0));
    }*/