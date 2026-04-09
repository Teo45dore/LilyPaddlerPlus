using HarmonyLib;
using System.Reflection;

namespace LilyPaddlerPlus.Patches;

[HarmonyPatch(typeof(PlayerMotor))]
public static class PlayerMotorPatch
{
    static FieldInfo directionField = AccessTools.Field(typeof(PlayerMotor), "movementInputDirection");
    public static bool ApplySlippery => HypnosisScreenFXControllerPatch.disorientedIntensity > 0.1f && ModConfig.SlipperyMovementCfg.Value; 

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    private static void UpdatePostfix(PlayerMotor __instance)
    {
        if (!ModConfig.InvertMovementCfg.Value || HypnosisScreenFXControllerPatch.unscaledDisorientedIntensity < 0.2f) {
            return;
        }

        directionField.SetValue(__instance, -(Vector3)directionField.GetValue(__instance));
    }
}
