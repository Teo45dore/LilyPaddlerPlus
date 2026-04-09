using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace LilyPaddlerPlus.Patches;

[HarmonyPatch(typeof(QuickSlots))]
public static class QuickSlotsPatch
{
    public static List<int> randomizedBinds = new List<int>();
    public static bool shuffled = false;
    public static bool selectedWithHotkey = false;
    static int progress = 0;

    static QuickSlotsPatch() {
        HypnosisScreenFXControllerPatch.onStartDisorient += RandomizeBinds;
        HypnosisScreenFXControllerPatch.onStopDisorient += UnShuffle;
    }

    [HarmonyPatch(typeof(Inventory), "Awake")]
    [HarmonyPostfix]
    private static void Init() {
        for (int i = 0; i < Inventory.main.quickSlots.slotCount; i++) {
            randomizedBinds.Add(i);
        }
    }

    [HarmonyPatch("Select")]
    [HarmonyPrefix]
    private static void SelectPrefix(ref int slotID) {
        if (!ModConfig.RandomizeHotbarCfg.Value || slotID == -1 || !selectedWithHotkey) {
            return;
        }

        selectedWithHotkey = false;
        
        progress = slotID;

        slotID = randomizedBinds[slotID];
    }

    public static void RandomizeBinds() {
        UnShuffle();

        if (!ModConfig.RandomizeHotbarCfg.Value) {
            return;
        }

        while (!CheckShuffle()) {
            Shuffle();
        }

        shuffled = true;

        if (!ModConfig.VerboseLoggingCfg.Value) {
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

    public static void UnShuffle() {
        if (!shuffled) {
            return;
        }

        randomizedBinds.Clear();

        for (int i = 0; i < Inventory.main.quickSlots.slotCount; i++) {
            randomizedBinds.Add(i);
        }

        shuffled = false;
    }

    private static void Shuffle() {
        for (int i = randomizedBinds.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (randomizedBinds[j], randomizedBinds[i]) = (randomizedBinds[i], randomizedBinds[j]);
        }
    }

    private static bool CheckShuffle() {
        for (int i = 0; i < randomizedBinds.Count; i++) {
            if (i == randomizedBinds[i]) {
                return false;
            }
        }

        return true;
    }

    private static void NextSlotPatch() {        
        QuickSlots quickSlots = Inventory.main.quickSlots;

        int index = (progress < 0) ? -1 : progress;
        int slotCount = quickSlots.slotCount;

        for (int i = 0; i < slotCount; i++) {
            index++;

            if (index >= slotCount) {
                progress = -1;
                quickSlots.Deselect();
                return;
            }

            TechType slotType = quickSlots.GetSlotBinding(randomizedBinds[index]);

            if (slotType == TechType.None) {
                continue;
            }

            QuickSlotType quickSlotType = TechData.GetSlotType(slotType);

            if (quickSlotType == QuickSlotType.Selectable || quickSlotType == QuickSlotType.SelectableChargeable) {
                progress = index;
                quickSlots.Select(randomizedBinds[index]);
                return;
            }
        }
    }

    private static void PreviousSlotPatch() {
        QuickSlots quickSlots = Inventory.main.quickSlots;

        int slotCount = quickSlots.slotCount;
        int index = (progress < 0) ? slotCount : progress;

        for (int i = 0; i < slotCount; i++) {
            index--;

            if (index < 0) {
                progress = -1;
                quickSlots.Deselect();
                return;
            }

            TechType slotType = quickSlots.GetSlotBinding(randomizedBinds[index]);
            
            if (slotType == TechType.None) {
                continue;
            }

            QuickSlotType quickSlotType = TechData.GetSlotType(slotType);

            if (quickSlotType == QuickSlotType.Selectable || quickSlotType == QuickSlotType.SelectableChargeable) {
                progress = index;
                quickSlots.Select(randomizedBinds[index]);
                return;
            }
        }
    }

    #region Transpiler Stuff

    public static FieldInfo bindingFld = AccessTools.Field(typeof(QuickSlots), "binding");
    public static FieldInfo desiredSlotFld = AccessTools.Field(typeof(QuickSlots), "desiredSlot");
    public static FieldInfo shuffledFld = AccessTools.Field(typeof(QuickSlotsPatch), "shuffled");
    public static FieldInfo selectedWithHotkeyFld = AccessTools.Field(typeof(QuickSlotsPatch), "selectedWithHotkey");
    public static MethodInfo handlerNxtMth = AccessTools.Method(typeof(QuickSlotsPatch), "NextSlotPatch");
    public static MethodInfo handlerPrvMth = AccessTools.Method(typeof(QuickSlotsPatch), "PreviousSlotPatch");

    [HarmonyPatch("SlotNext")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SlotNextTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

        Label ifEndLabel = default;

        int insertIndex = -1;
        for (int i = 1; i < instructionList.Count; i++) {
            if (instructionList[i - 1].opcode == OpCodes.Ret && instructionList[i].opcode == OpCodes.Ldarg_0 && instructionList[i + 1].OperandIs(AccessTools.Method("QuickSlots:GetActiveSlotID"))) {
                insertIndex = i;
                ifEndLabel = instructionList[i].labels[0];
                instructionList[i].labels.RemoveAt(0);
                if (ModConfig.VerboseLoggingCfg.Value) {
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

        CodeInstruction labledInstruction1 = new CodeInstruction(OpCodes.Ldsfld, shuffledFld);
        labledInstruction1.labels.Add(ifEndLabel);
        CodeInstruction labledInstruction2 = new CodeInstruction(OpCodes.Nop);
        labledInstruction2.labels.Add(endLabel);

        newInstructions.Add(labledInstruction1);
        newInstructions.Add(new CodeInstruction(OpCodes.Brfalse, endLabel));
        newInstructions.Add(new CodeInstruction(OpCodes.Call, handlerNxtMth));
        newInstructions.Add(new CodeInstruction(OpCodes.Ret));
        newInstructions.Add(labledInstruction2);

        instructionList.InsertRange(insertIndex, newInstructions);

        if (ModConfig.VerboseLoggingCfg.Value) {
            Plugin.Logger.LogDebug("SlotNextTranspiler: All done!");
        }

        return instructionList;
    }

    [HarmonyPatch("SlotPrevious")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SlotPreviousTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

        Label ifEndLabel = default;

        int insertIndex = -1;
        for (int i = 1; i < instructionList.Count; i++) {
            if (instructionList[i - 1].opcode == OpCodes.Ret && instructionList[i].opcode == OpCodes.Ldarg_0 && instructionList[i + 1].OperandIs(AccessTools.Method("QuickSlots:GetActiveSlotID"))) {
                insertIndex = i;
                ifEndLabel = instructionList[i].labels[0];
                instructionList[i].labels.RemoveAt(0);
                if (ModConfig.VerboseLoggingCfg.Value) {
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

        CodeInstruction labledInstruction1 = new CodeInstruction(OpCodes.Ldsfld, shuffledFld);
        labledInstruction1.labels.Add(ifEndLabel);
        CodeInstruction labledInstruction2 = new CodeInstruction(OpCodes.Nop);
        labledInstruction2.labels.Add(endLabel);

        newInstructions.Add(labledInstruction1);
        newInstructions.Add(new CodeInstruction(OpCodes.Brfalse, endLabel));
        newInstructions.Add(new CodeInstruction(OpCodes.Call, handlerPrvMth));
        newInstructions.Add(new CodeInstruction(OpCodes.Ret));
        newInstructions.Add(labledInstruction2);

        instructionList.InsertRange(insertIndex, newInstructions);

        if (ModConfig.VerboseLoggingCfg.Value) {
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
                if (ModConfig.VerboseLoggingCfg.Value) {
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
        newInstructions.Add(new CodeInstruction(OpCodes.Stsfld, selectedWithHotkeyFld));

        instructionList.InsertRange(insertIndex, newInstructions);

        if (ModConfig.VerboseLoggingCfg.Value) {
            Plugin.Logger.LogDebug("SlotKeyDownTranspiler: All done!");
        }

        return instructionList;
    }

    #endregion
}