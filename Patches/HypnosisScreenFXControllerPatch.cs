using HarmonyLib;
using System.Reflection;

namespace LilyPaddlerPlus.Patches;

//This class mostly just provides info to the other patches.

[HarmonyPatch(typeof(HypnosisScreenFXController))]
public static class HypnosisScreenFXControllerPatch
{
    private static FieldInfo intensity = AccessTools.Field(typeof(HypnosisScreenFXController), "intensity");
    private static FieldInfo isSwirlingField = AccessTools.Field(typeof(HypnosisScreenFXController), "isSwirling");

    public static bool isSwirling = false;
    public static float disorientedIntensity = 0f;
    public static float unscaledDisorientedIntensity = 0f;

    public static event Action onStartDisorient;
    public static event Action onStopDisorient;

    public static event Action afterUpdate;

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    private static void UpdatePostfix(HypnosisScreenFXController __instance)
    {
        unscaledDisorientedIntensity = (float)intensity.GetValue(__instance);
        isSwirling = (bool)isSwirlingField.GetValue(__instance);
        disorientedIntensity = Mathf.Max(unscaledDisorientedIntensity * ModConfig.IntensityCfg.Value, 0);
        afterUpdate?.Invoke();
    }

    [HarmonyPatch("StartFx")]
    [HarmonyPostfix]
    private static void StartEvent()
    {
        onStartDisorient?.Invoke();
    }

    [HarmonyPatch("StopFx")]
    [HarmonyPostfix]
    private static void StopEvent()
    {
        onStopDisorient?.Invoke();
    }

    /*
    public static bool DampCamera()
    {
        if (unscaledDisorientedIntensity <= 0) {
            if (QuickSlotsPatch.shuffled) {
                QuickSlotsPatch.UnShuffle();
            }
            return false;
        }
        if (!QuickSlotsPatch.shuffled && ModConfig.RandomizeHotbarCfg.Value) {
            QuickSlotsPatch.RandomizeBinds();
        } else if (QuickSlotsPatch.shuffled && !ModConfig.RandomizeHotbarCfg.Value) {
            QuickSlotsPatch.UnShuffle();
        }
        return ModConfig.DampCameraCfg.Value;
    }
    */
}