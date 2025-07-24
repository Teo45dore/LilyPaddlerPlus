using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LilyPaddlerPlus.Patches;
using UnityEngine;
using UWEXR;

namespace LilyPaddlerPlus.Transpilers;

[HarmonyPatch(typeof(MainCameraControl))]
internal class CameraControlTranspiler
{
    private static Vector2 currentLookVelocity;
    private static float maxRotationSpeed = 1.6f;

    [HarmonyPatch("OnUpdate")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> OnUpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

        int insertIndex = -1;
        for (int i = 0; i < instructionList.Count; i++) {
            if (instructionList[i].opcode == OpCodes.Call && (MethodInfo)instructionList[i].operand == AccessTools.Method(typeof(GameInput), "GetLookDelta") && instructionList[i + 2].opcode == OpCodes.Call && (MethodInfo)instructionList[i + 2].operand == AccessTools.PropertyGetter(typeof(XRSettings), "enabled")) {
                insertIndex = i + 1;
                if (Config.ModConfig.Instance.MoreDebugCfg.Value) {
                    Plugin.Logger.LogDebug("OnUpdateTranspiler: Found inject point...");
                }
                break;
            }
        }

        if (insertIndex == -1) {
            Plugin.Logger.LogError("OnUpdateTranspiler: Couldn't find first inject point!");
            return instructionList;
        }

        instructionList.Insert(insertIndex, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CameraControlTranspiler), "DampDelta")));

        if (Config.ModConfig.Instance.MoreDebugCfg.Value) {
            Plugin.Logger.LogDebug("OnUpdateTranspiler: All done!");
        }

        return instructionList;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified", Justification = "Because harmony thinks this is a patch method so every line gets a warning.")]
    public static Vector2 DampDelta(Vector2 vector)
    {
        if (HypnosisScreenFXControllerPatch.DampCamera() && Config.ModConfig.Instance.IntensityCfg.Value != 0f) {
            vector *= Mathf.Max(60 * HypnosisScreenFXControllerPatch.disorientedIntensity, 1);
            vector = Vector2.SmoothDamp(Vector2.zero, vector, ref currentLookVelocity, 1f * HypnosisScreenFXControllerPatch.disorientedIntensity);

            vector.x = Mathf.Clamp(vector.x, -maxRotationSpeed, maxRotationSpeed);
            vector.y = Mathf.Clamp(vector.y, -maxRotationSpeed, maxRotationSpeed);
        }

        if (Config.ModConfig.Instance.InvertCameraCfg.Value && HypnosisScreenFXControllerPatch.unscaledDisorientedIntensity > 0) {
            vector = -vector;
        }

        return vector;
    }
}
