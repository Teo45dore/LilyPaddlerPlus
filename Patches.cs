using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using LilyPaddlerPlus.Config;
using LilyPaddlerPlus.Transpilers;
using UnityEngine;

namespace LilyPaddlerPlus.Patches;

[HarmonyPatch(typeof(HypnosisScreenFXController))]
public class HypnosisScreenFXControllerPatch : MonoBehaviour
{
    public static FieldInfo intensity = AccessTools.Field(typeof(HypnosisScreenFXController), "intensity");

    public static float disorientedIntensity = 0f;
    public static float unscaledDisorientedIntensity = 0f;

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    private static void UpdatePostfix(HypnosisScreenFXController __instance)
    {
        unscaledDisorientedIntensity = (float)intensity.GetValue(__instance);
        disorientedIntensity = Mathf.Max(unscaledDisorientedIntensity * ModConfig.Instance.IntensityCfg.Value, 0);
    }

    public static bool DampCamera()
    {
        if (unscaledDisorientedIntensity <= 0) {
            if (QuickSlotsPatch.shuffled) {
                QuickSlotsPatch.UnShuffle();
            }
            return false;
        }
        if (!QuickSlotsPatch.shuffled && ModConfig.Instance.RandomizeHotbarCfg.Value) {
            QuickSlotsPatch.RandomizeBinds();
        } else if (QuickSlotsPatch.shuffled && !ModConfig.Instance.RandomizeHotbarCfg.Value) {
            QuickSlotsPatch.UnShuffle();
        }
        return ModConfig.Instance.DampCameraCfg.Value;
    }

    public static bool Slippery()
    {
        return disorientedIntensity > 0.1f && ModConfig.Instance.SlipperyMovementCfg.Value;
    }
}

[HarmonyPatch(typeof(QuickSlots))]
public class QuickSlotsPatch
{
    public static List<int> randomizedBinds = new List<int>();
    public static bool shuffled = false;
    public static bool calledByInput = false;

    [HarmonyPatch(typeof(Inventory), "Awake")]
    [HarmonyPostfix]
    private static void AwakePostfix()
    {
        UnShuffle();
    }

    [HarmonyPatch("Select")]
    [HarmonyPrefix]
    private static void SelectPrefix(ref int slotID)
    {
        if (!ModConfig.Instance.RandomizeHotbarCfg.Value || slotID == -1 || !calledByInput) {
            return;
        }

        calledByInput = false;

        slotID = randomizedBinds[slotID];

        QuickSlotsTranspilers.usedNextSlots.Clear();
        QuickSlotsTranspilers.usedPrevSlots.Clear();

        QuickSlotsTranspilers.usedNormPrev = false;
        QuickSlotsTranspilers.usedNormNext = false;

        if (slotID != -1) {
            QuickSlotsTranspilers.usedNextSlots.Add(slotID);
            QuickSlotsTranspilers.usedPrevSlots.Add(slotID);
        }
    }

    public static void RandomizeBinds()
    {
        UnShuffle();

        while (!Check()) {
            Shuffle();
        }

        shuffled = true;

        int desiredSlot = (int)QuickSlotsTranspilers.desiredSlotFld.GetValue(Inventory.main.quickSlots);

        if (desiredSlot != -1) {
            QuickSlotsTranspilers.usedNextSlots.Add(desiredSlot);
            QuickSlotsTranspilers.usedPrevSlots.Add(desiredSlot);
        }

        if (!ModConfig.Instance.MoreDebugCfg.Value) {
            return;
        }

        StringBuilder sb = new StringBuilder();

        sb.Append("Generated random hotkeys: {");
        foreach (int i in randomizedBinds) {
            sb.Append(" " + i + ",");
        }
        sb.Remove(sb.Length - 1, 1);
        sb.Append(" }");

        Plugin.Logger.LogDebug(sb.ToString());
    }

    public static void UnShuffle()
    {
        randomizedBinds.Clear();

        for (int i = 0; i < Inventory.main.quickSlots.slotCount; i++) {
            randomizedBinds.Add(i);
        }

        shuffled = false;

        QuickSlotsTranspilers.usedNextSlots.Clear();
        QuickSlotsTranspilers.usedPrevSlots.Clear();

        QuickSlotsTranspilers.usedNormPrev = false;
        QuickSlotsTranspilers.usedNormNext = false;
    }

    private static void Shuffle()
    {
        for (int i = randomizedBinds.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (randomizedBinds[j], randomizedBinds[i]) = (randomizedBinds[i], randomizedBinds[j]);
        }
    }

    private static bool Check()
    {
        for (int i = 0; i < randomizedBinds.Count; i++) {
            if (i == randomizedBinds[i]) {
                return false;
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(GroundMotor))]
public class GroundMotorPatch
{
    public static Vector3 curVel = Vector3.zero;
    public static float friction;
    public static bool wasLastSprinting = false;
    public static FieldInfo moveDirFld = AccessTools.Field(typeof(PlayerMotor), "movementInputDirection");
    private static float sprintBuffer;

    [HarmonyPatch("ApplyInputVelocityChange")]
    [HarmonyPostfix]
    static void ApplyInputVelocityChangePostfix(GroundMotor __instance, ref Vector3 __result)
    {
        if (!HypnosisScreenFXControllerPatch.Slippery() || !__instance.IsGrounded()) {
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
        } catch (System.DivideByZeroException) {
            return;
        }


        __result = Vector3.Lerp(__instance.movement.velocity, __result, friction * Time.deltaTime);
    }
}

[HarmonyPatch(typeof(PlayerMotor))]
public class PlayerMotorPatch
{
    static FieldInfo directionField = AccessTools.Field(typeof(PlayerMotor), "movementInputDirection");

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    private static void UpdatePostfix(PlayerMotor __instance)
    {
        if (!ModConfig.Instance.InvertMovementCfg.Value || HypnosisScreenFXControllerPatch.unscaledDisorientedIntensity < 0.2f) {
            return;
        }

        directionField.SetValue(__instance, -(Vector3)directionField.GetValue(__instance));
    }
}