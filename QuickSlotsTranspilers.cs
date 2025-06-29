using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LilyPaddlerPlus.Patches;

namespace LilyPaddlerPlus.Transpilers;

[HarmonyPatch(typeof(QuickSlots))]
public class QuickSlotsTranspilers
{
    public static FieldInfo bindingFld = AccessTools.Field(typeof(QuickSlots), "binding");
    public static FieldInfo desiredSlotFld = AccessTools.Field(typeof(QuickSlots), "desiredSlot");
    public static FieldInfo shuffledFld = AccessTools.Field(typeof(QuickSlotsPatch), "shuffled");
    public static FieldInfo calledByInputFld = AccessTools.Field(typeof(QuickSlotsPatch), "calledByInput");
    public static MethodInfo handlerNxtMth = AccessTools.Method(typeof(QuickSlotsTranspilers), "HandleNextSlot");
    public static MethodInfo handlerPrvMth = AccessTools.Method(typeof(QuickSlotsTranspilers), "HandlePrevSlot");
    private static List<InventoryItem> prevSlots = new List<InventoryItem>();
    public static List<int> usedNextSlots = new List<int>();
    public static List<int> usedPrevSlots = new List<int>();
    private static List<int> availableSlots = new List<int>();
    public static bool usedNormNext = false;
    public static bool usedNormPrev = false;
    public static int? lastIndex = null;

    [HarmonyPatch("SlotNext")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SlotNextTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

        int insertIndex = -1;
        for (int i = 1; i < instructionList.Count; i++) {
            if (instructionList[i - 1].opcode == OpCodes.Stloc_2 && instructionList[i].opcode == OpCodes.Ldloc_2 && instructionList[i + 1].opcode == OpCodes.Ldloc_1) {
                insertIndex = i;
                if (Config.ModConfig.Instance.MoreDebugCfg.Value) {
                    Plugin.Logger.LogDebug("SlotNextTranspiler: Found inject point!");
                }
                break;
            }
        }

        if (insertIndex == -1) {
            Plugin.Logger.LogError("SlotNextTranspiler: Cannot find target inject point");
            return instructionList;
        }

        List<CodeInstruction> newInstructions = new List<CodeInstruction>();

        Label endLabel = il.DefineLabel();

        CodeInstruction labledInstruction = new CodeInstruction(OpCodes.Nop);
        labledInstruction.labels.Add(endLabel);

        newInstructions.Add(new CodeInstruction(OpCodes.Ldsfld, shuffledFld));
        newInstructions.Add(new CodeInstruction(OpCodes.Brfalse, endLabel));
        newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_2));
        newInstructions.Add(new CodeInstruction(OpCodes.Call, handlerNxtMth));
        newInstructions.Add(new CodeInstruction(OpCodes.Stloc_2));

        newInstructions.Add(labledInstruction);

        instructionList.InsertRange(insertIndex, newInstructions);

        if (Config.ModConfig.Instance.MoreDebugCfg.Value) {
            Plugin.Logger.LogDebug("SlotNextTranspiler: All done!");
        }

        return instructionList;
    }

    [HarmonyPatch("SlotPrevious")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SlotPreviousTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

        int insertIndex = -1;
        for (int i = 1; i < instructionList.Count; i++) {
            if (instructionList[i - 1].opcode == OpCodes.Stloc_2 && instructionList[i].opcode == OpCodes.Ldloc_2 && instructionList[i + 1].opcode == OpCodes.Ldc_I4_0) {
                insertIndex = i;
                if (Config.ModConfig.Instance.MoreDebugCfg.Value) {
                    Plugin.Logger.LogDebug("SlotPreviousTranspiler: Found inject point!");
                }
                break;
            }
        }

        if (insertIndex == -1) {
            Plugin.Logger.LogError("SlotPreviousTranspiler: Cannot find target inject point");
            return instructionList;
        }

        List<CodeInstruction> newInstructions = new List<CodeInstruction>();

        Label endLabel = il.DefineLabel();

        CodeInstruction labledInstruction = new CodeInstruction(OpCodes.Nop);
        labledInstruction.labels.Add(endLabel);

        newInstructions.Add(new CodeInstruction(OpCodes.Ldsfld, shuffledFld));
        newInstructions.Add(new CodeInstruction(OpCodes.Brfalse, endLabel));
        newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_2));
        newInstructions.Add(new CodeInstruction(OpCodes.Call, handlerPrvMth));
        newInstructions.Add(new CodeInstruction(OpCodes.Stloc_2));
        newInstructions.Add(labledInstruction);

        instructionList.InsertRange(insertIndex, newInstructions);

        if (Config.ModConfig.Instance.MoreDebugCfg.Value) {
            Plugin.Logger.LogDebug("SlotPreviousTranspiler: All done!");
        }

        return instructionList;
    }

    [HarmonyPatch("SlotKeyDown")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SlotKeyDownTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

        int insertIndex = -1;
        for (int i = 2; i < instructionList.Count; i++) {
            if (instructionList[i - 2].opcode == OpCodes.Ldfld && (FieldInfo)instructionList[i - 2].operand == AccessTools.Field(typeof(QuickSlots), "ignoreHotkeyInput") && instructionList[i - 1].opcode == OpCodes.Brtrue && instructionList[i].opcode == OpCodes.Ldarg_0) {
                insertIndex = i;
                if (Config.ModConfig.Instance.MoreDebugCfg.Value) {
                    Plugin.Logger.LogDebug("SlotKeyDownTranspiler: Found inject point!");
                }
                break;
            }
        }

        if (insertIndex == -1) {
            Plugin.Logger.LogError("SlotKeyDownTranspiler: Cannot find target inject point");
            return instructionList;
        }

        List<CodeInstruction> newInstructions = new List<CodeInstruction>();

        newInstructions.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
        newInstructions.Add(new CodeInstruction(OpCodes.Stsfld, calledByInputFld));

        instructionList.InsertRange(insertIndex, newInstructions);

        if (Config.ModConfig.Instance.MoreDebugCfg.Value) {
            Plugin.Logger.LogDebug("SlotKeyDownTranspiler: All done!");
        }

        return instructionList;
    }

    private static int HandleNextSlot(int num)
    {
        QuickSlots quickSlots = Inventory.main.quickSlots;

        InventoryItem[] bindingsArray = (InventoryItem[])bindingFld.GetValue(quickSlots);
        List<InventoryItem> bindings = bindingsArray.ToList();

        if (!prevSlots.Equals(bindings)) {
            prevSlots = bindings;
            availableSlots.Clear();

            prevSlots.ForEach(slot => {
                if (slot != null && slot.techType != TechType.None) {
                    availableSlots.Add(quickSlots.GetSlotByItem(slot));
                }
            });
        }

        if (availableSlots.Count == 1 || availableSlots.Count == 0) {
            return num;
        }

        int rand = availableSlots[UnityEngine.Random.Range(0, availableSlots.Count)];
        int retrys = -1;

        if (usedNextSlots.Count >= availableSlots.Count) {
            usedNormNext = false;
            lastIndex = usedNextSlots[usedNextSlots.Count - 1];
            usedNextSlots.Clear();
            usedPrevSlots.Clear();
            return quickSlots.slotCount;
        }

        int desiredSlot = (int)desiredSlotFld.GetValue(quickSlots);

        bool lastItem = availableSlots.Count - usedNextSlots.Count == 1;
        bool otherChecks = rand == desiredSlot + 1 && usedNormNext || rand == lastIndex;

        if (usedNextSlots.Count != 0) {
            do {
                if (retrys > 99) {
                    Plugin.Logger.LogWarning("Number of retrys to find a valid random slot in SlotNext has exceeded 99! This might cause unexpected behavior.");
                    break;
                }

                rand = availableSlots[UnityEngine.Random.Range(0, availableSlots.Count)];

                otherChecks = rand == desiredSlot + 1 && usedNormNext || rand == lastIndex;

                if (lastItem) {
                    otherChecks = false;
                }

                if (rand == desiredSlot + 1 && !usedNormNext) {
                    usedNormNext = true;
                }

                retrys++;
            } while (usedNextSlots.Contains(rand) || otherChecks);
        } else {
            do {
                retrys++;
                rand = availableSlots[UnityEngine.Random.Range(0, availableSlots.Count)];
            } while (rand == lastIndex && availableSlots.Count > 2);
        }

        if (usedPrevSlots.Count > 0) {
            usedPrevSlots.RemoveAt(usedPrevSlots.Count - 1);
        }

        usedNextSlots.Add(rand);

        lastIndex = rand;

        return rand;
    }

    private static int HandlePrevSlot(int num)
    {
        QuickSlots quickSlots = Inventory.main.quickSlots;

        InventoryItem[] bindingsArray = (InventoryItem[])bindingFld.GetValue(quickSlots);
        List<InventoryItem> bindings = bindingsArray.ToList();

        if (!prevSlots.Equals(bindings)) {
            prevSlots = bindings;
            availableSlots.Clear();

            prevSlots.ForEach(slot => {
                if (slot != null && slot.techType != TechType.None) {
                    availableSlots.Add(quickSlots.GetSlotByItem(slot));
                }
            });
        }

        if (availableSlots.Count == 1 || availableSlots.Count == 0) {
            return num;
        }

        int rand = availableSlots[UnityEngine.Random.Range(0, availableSlots.Count)];
        int retrys = -1;

        if (usedPrevSlots.Count >= availableSlots.Count) {
            usedNormPrev = false;
            lastIndex = usedPrevSlots[usedPrevSlots.Count - 1];
            usedPrevSlots.Clear();
            usedNextSlots.Clear();
            return -1;
        }

        int desiredSlot = (int)desiredSlotFld.GetValue(quickSlots);

        bool lastItem = availableSlots.Count - usedPrevSlots.Count == 1;
        bool otherChecks = rand == desiredSlot - 1 && usedNormPrev || rand == lastIndex;

        if (usedPrevSlots.Count != 0) {
            do {
                if (retrys > 99) {
                    Plugin.Logger.LogWarning("Number of retrys to find a valid random slot in SlotPrevious has exceeded 99! This might cause unexpected behavior.");
                    break;
                }

                rand = availableSlots[UnityEngine.Random.Range(0, availableSlots.Count)];

                otherChecks = rand == desiredSlot - 1 && usedNormPrev || rand == lastIndex;

                if (lastItem) {
                    otherChecks = false;
                }

                if (rand == desiredSlot - 1 && !usedNormPrev) {
                    usedNormPrev = true;
                }

                retrys++;
            } while (usedPrevSlots.Contains(rand) || otherChecks);
        } else {
            do {
                retrys++;
                rand = availableSlots[UnityEngine.Random.Range(0, availableSlots.Count)];
            } while (rand == lastIndex && availableSlots.Count > 2);
        }

        if (usedNextSlots.Count > 0) {
            usedNextSlots.RemoveAt(usedNextSlots.Count - 1);
        }

        usedPrevSlots.Add(rand);

        lastIndex = rand;

        return rand;
    }
}
