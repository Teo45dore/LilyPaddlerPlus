using HarmonyLib;
using System.Reflection;

namespace LilyPaddlerPlus.Patches;

[HarmonyPatch(typeof(GroundMotor))]
public static class GroundMotorPatch
{
    public static Vector3 curVel = Vector3.zero;
    public static float friction;
    public static bool wasLastSprinting = false;
    public static FieldInfo moveDirFld = AccessTools.Field(typeof(PlayerMotor), "movementInputDirection");
    private static float sprintBuffer;

    [HarmonyPatch("ApplyInputVelocityChange")]
    [HarmonyPostfix]
    static void ApplyInputVelocityChangePostfix(GroundMotor __instance, ref Vector3 __result) {
        if (!PlayerMotorPatch.ApplySlippery || !__instance.IsGrounded()) {
            return;
        }

        if (__result.magnitude < 0.1f && wasLastSprinting) {
            wasLastSprinting = false;
        }
        if ((Vector3)moveDirFld.GetValue(__instance) != Vector3.zero && !__instance.IsSprinting()) {
            sprintBuffer += Time.deltaTime;
            if (sprintBuffer > 0.2f) {
                sprintBuffer = 0f;
                wasLastSprinting = false;
            }
        } else {
            sprintBuffer = 0f;
        }

        try {
            if (!__instance.IsSprinting()) {
                friction = 14f / HypnosisScreenFXControllerPatch.disorientedIntensity;
            } else {
                wasLastSprinting = true;
                friction = 21f / HypnosisScreenFXControllerPatch.disorientedIntensity;
            }

            if (wasLastSprinting) {
                friction = 21f / HypnosisScreenFXControllerPatch.disorientedIntensity;
            }
        } catch (DivideByZeroException) {
            return;
        }


        __result = Vector3.Lerp(__instance.movement.velocity, __result, friction * Time.deltaTime);
    }
}