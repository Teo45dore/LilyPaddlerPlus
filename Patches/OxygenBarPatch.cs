using HarmonyLib;
using System.Reflection.Emit;

namespace LilyPaddlerPlus.Patches;

[HarmonyPatch(typeof(uGUI_OxygenBar))]
public static class OxygenBarPatch {

    static OxygenBarPatch() {
        HypnosisScreenFXControllerPatch.onStartDisorient += OnDisorientStart;
        HypnosisScreenFXControllerPatch.onStopDisorient += OnDisorientStop;

        ModConfig.ShowFalseValuesCfg.SettingChanged += (sender, e) => {
            if (HypnosisScreenFXControllerPatch.unscaledDisorientedIntensity <= 0) return;
            
            if (ModConfig.ShowFalseValuesCfg.Value) {
                OnDisorientStart();
            }
        };
    }

    static float offsetAmount = 0;

    static float swirlSpeed = 3;
    static float swirlProgress = 0;

    static bool intro = false;
    static bool outro = false;

    static float outroSpeed = 0.25f;

    static float lastO2Capacity = 0;

    private static void OnDisorientStart() {
        if (!ModConfig.ShowFalseValuesCfg.Value) {
            return;
        }

        float capacity = Player.main.GetOxygenCapacity();
        float available = Player.main.GetOxygenAvailable();

        lastO2Capacity = capacity;

        //Set the offset:
        do  {
            if (capacity > 45) {
                offsetAmount = capacity * Random.Range(-0.7f, 0.7f);
            } else {
                offsetAmount = capacity * Random.Range(-0.4f, 0.7f);
            }
        } while (available + offsetAmount > capacity || available + offsetAmount <= 0 || ((available + offsetAmount) / capacity < 0.2f && (available +  offsetAmount) / capacity > -0.2f));

        //Start the start position for the spinning of the O2 bar where the O2 bar already is.
        swirlProgress = Mathf.Asin(available / capacity);

        intro = true;
    }

    private static void OnDisorientStop() {
        intro = false;
        outro = true;
    }

    private static float GetOxygenAvailablePatched(Player player) {
        float capacity = player.GetOxygenCapacity();

        if (lastO2Capacity != capacity && HypnosisScreenFXControllerPatch.disorientedIntensity > 0) {
            OnDisorientStart();
        }
        
        if (ModConfig.ShowFalseValuesCfg.Value && (intro || HypnosisScreenFXControllerPatch.isSwirling)) {
            swirlProgress += Time.deltaTime * swirlSpeed;

            //Only stop spinning the O2 bar when the player has stopped spinning around, and when the O2 bar is around where the offset would place it.
            if (!HypnosisScreenFXControllerPatch.isSwirling && Mathf.Abs(((Mathf.Sin(swirlProgress) + 1) / 2 * capacity) - (player.GetOxygenAvailable() + offsetAmount)) < 3f) {
                intro = false;
            }

            return (Mathf.Sin(swirlProgress) + 1) / 2 * capacity;
        } else if (outro) {
            offsetAmount = Mathf.MoveTowards(offsetAmount, 0, Time.deltaTime * capacity * outroSpeed);
            if (offsetAmount == 0) {
                outro = false;
            }
        }
        
        return Mathf.Clamp(player.GetOxygenAvailable() + offsetAmount, 0, capacity);
    }

    [HarmonyPatch("LateUpdate")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> LateUpdateTranspiler(IEnumerable<CodeInstruction> rawInstructions) {
        List<CodeInstruction> instructions = new List<CodeInstruction>(rawInstructions);

        bool foundTarget = false;

        for (int i = 0; i < instructions.Count; i++) {
            if (instructions[i].OperandIs(AccessTools.Method("Player:GetOxygenAvailable")) && instructions[i + 1].opcode == OpCodes.Stloc_1) {
                foundTarget = true;
                instructions.Insert(i, new CodeInstruction(OpCodes.Call, AccessTools.Method("OxygenBarPatch:GetOxygenAvailablePatched")));
                instructions.RemoveAt(i + 1);
                break;
            }
        }

        if (!foundTarget) {
            Plugin.Logger.LogError("Couldn't find target instruction in Player:GetOxygenAvailable!");
            return rawInstructions;
        }

        return instructions;
    }
}