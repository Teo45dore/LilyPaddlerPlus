using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine.PostProcessing;

namespace LilyPaddlerPlus.Patches;

[HarmonyPatch(typeof(UwePostProcessingManager))]
public static class UwePostProcessingPatch {
    private static float multiplier = 1;
    private static float resetSpeed = 3;

    private static float progress = 0;

    #pragma warning disable CS0414 //Value is read by reflection.
    private static bool applyMultiplier = false;
    #pragma warning restore CS0414

    [HarmonyPatch("UpdateDof")]
    [HarmonyTranspiler]
    [HarmonyDebug]
    private static IEnumerable<CodeInstruction> UpdateDofTranspiler(IEnumerable<CodeInstruction> rawInstructions) {
        List<CodeInstruction> instructions = new List<CodeInstruction>(rawInstructions);
        
        int insert1 = -1;
        for (int i = 0; i < instructions.Count; i++) {
            if (instructions[i].opcode == OpCodes.Stfld && instructions[i].OperandIs(AccessTools.Field(typeof(DepthOfFieldModel.Settings), "focusDistance"))) {
                insert1 = i;
            }
        }
        
        if (insert1 == -1) {
            Plugin.Logger.LogError("First inject point not found in UwePostProcessingManager::UpdateDof");
            return instructions;
        }

        instructions.InsertRange(insert1, new List<CodeInstruction>() {
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(UwePostProcessingPatch), "multiplier")),
            new CodeInstruction(OpCodes.Mul)
        });

        int insert2 = -1;
        for (int i = 3; i < instructions.Count; i++) {
            if (instructions[i].opcode == OpCodes.Stloc_1 && instructions[i - 3].OperandIs(AccessTools.Method("Mathf:Approximately"))) {
                insert2 = i;
            }
        }

        if (insert2 == -1) {
            Plugin.Logger.LogError("Second inject point not found in UwePostProcessingManager::UpdateDof");
            return instructions;
        }

        instructions.InsertRange(insert2, new List<CodeInstruction>() {
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(UwePostProcessingPatch), "applyMultiplier")),
            new CodeInstruction(OpCodes.Or),
        });

        return instructions;
    }

    [HarmonyPatch("UpdateDof")]
    [HarmonyPrefix]
    private static void Update(ref PostProcessingProfile currentProfile, float ___dofFocusDistance) {        
        if (!Player.main || (multiplier == 1 && (!ModConfig.DOFTomfooleryCfg.Value || !Player.main.lilyPaddlerHypnosis.IsHypnotized()))) {
            if (applyMultiplier && currentProfile.depthOfField.settings.focusDistance == ___dofFocusDistance) {
                applyMultiplier = false;
            }
            return;
        }
        
        applyMultiplier = true;
        
        float deltaTime = Time.deltaTime;

        if (!Player.main.lilyPaddlerHypnosis.IsHypnotized() || !ModConfig.DOFTomfooleryCfg.Value) {
            multiplier = Mathf.Lerp(multiplier, 1, resetSpeed * deltaTime);
            if (1 - multiplier < 0.01) {
                multiplier = 1;
                progress = 0;
            }
            return;
        }

        progress += deltaTime;

        // Maps to a 0.3 - 1.3 range but capped at 1 so the blur happens in waves.
        multiplier = Mathf.Min(Mathf.PingPong(progress * 1.1f, 1) + 0.3f, 0.9f);
    }
}