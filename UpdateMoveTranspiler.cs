using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LilyPaddlerPlus.Patches;
using UnityEngine;

namespace LilyPaddlerPlus.Transpilers;

[HarmonyPatch(typeof(UnderwaterMotor))]
internal class UnderWaterMotorTranspilers
{
    private static bool logged = false;

    [HarmonyPatch("UpdateMove")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> UpdateMoveTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
    {
        if (!Config.ModConfig.Instance.MoreDebugCfg.Value) {
            logged = true;
        }

        List<CodeInstruction> newInstructions1 = new List<CodeInstruction>();
        List<CodeInstruction> instructionsList = new List<CodeInstruction>(instructions);

        LocalBuilder reduceDrag = ilGen.DeclareLocal(typeof(bool));

        int insertIndex = -1;

        Label overrideLabel1 = ilGen.DefineLabel();
        Label ifEnd1 = ilGen.DefineLabel();
        Label ifEnd2 = ilGen.DefineLabel();

        CodeInstruction extractedFalseBranch = null;
        CodeInstruction extractedUnConBranch = null;

        for (int i = 0; i < instructionsList.Count - 1; i++) {
            if (CheckInstructions(instructionsList, i, 0)) {
                insertIndex = i;

                for (int j = i - 1; j >= 0; j--) {
                    if (CheckInstructions(instructionsList, j, 1)) {
                        extractedFalseBranch = instructionsList[j];
                        break;
                    }
                }

                for (int j = i - 1; j >= 0; j--) {
                    if (CheckInstructions(instructionsList, j, 2)) {
                        extractedUnConBranch = instructionsList[j];
                        break;
                    }
                }

                break;
            }
        }

        if (insertIndex == -1) {
            Plugin.Logger.LogError("UpdateMoveTranspiler: Couldn't find first inject point!");
            return instructionsList;
        }

        if (extractedFalseBranch == null) {
            Plugin.Logger.LogError("UpdateMoveTranspiler: Couldn't find first FalseBranch");
            return instructionsList;
        }

        if (extractedUnConBranch == null) {
            Plugin.Logger.LogError("UpdateMoveTranspiler: Couldn't find first UnConBranch)");
            return instructionsList;
        }

        extractedUnConBranch.operand = overrideLabel1;
        extractedFalseBranch.operand = overrideLabel1;

        CodeInstruction labeledInstuction1 = new CodeInstruction(OpCodes.Nop);
        labeledInstuction1.labels.Add(overrideLabel1);
        CodeInstruction labeledInstuction2 = new CodeInstruction(OpCodes.Nop);
        labeledInstuction2.labels.Add(ifEnd1);

        newInstructions1.Add(labeledInstuction1);
        newInstructions1.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patches.HypnosisScreenFXControllerPatch), "Slippery")));
        newInstructions1.Add(new CodeInstruction(OpCodes.Stloc_S, reduceDrag));
        newInstructions1.Add(new CodeInstruction(OpCodes.Ldloc_S, reduceDrag));
        newInstructions1.Add(new CodeInstruction(OpCodes.Brfalse_S, ifEnd1));
        newInstructions1.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Player), "main")));
        newInstructions1.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Player), "motorMode")));
        newInstructions1.Add(new CodeInstruction(OpCodes.Ldc_I4_2));
        newInstructions1.Add(new CodeInstruction(OpCodes.Beq_S, ifEnd1));
        newInstructions1.Add(new CodeInstruction(OpCodes.Ldloc_S, (sbyte)7));
        newInstructions1.Add(new CodeInstruction(OpCodes.Ldc_R4, 1.075f));
        newInstructions1.Add(new CodeInstruction(OpCodes.Div));
        newInstructions1.Add(new CodeInstruction(OpCodes.Stloc_S, (sbyte)7));
        newInstructions1.Add(new CodeInstruction(labeledInstuction2));

        instructionsList.InsertRange(insertIndex, newInstructions1);

        List<CodeInstruction> newInstructions2 = new List<CodeInstruction>();
        insertIndex = -1;
        for (int i = 0; i < instructionsList.Count - 1; i++) {
            if (CheckInstructions(instructionsList, i, 3)) {
                insertIndex = i + 1;
                break;
            }
        }

        newInstructions2.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
        newInstructions2.Add(new CodeInstruction(OpCodes.Stloc_S, reduceDrag.LocalIndex));

        if (insertIndex == -1) {
            Plugin.Logger.LogError("UpdateMoveTranspiler: Couldn't find second inject point!");
            return instructionsList;
        }

        instructionsList.InsertRange(insertIndex, newInstructions2);

        List<CodeInstruction> newInstructions3 = new List<CodeInstruction>();

        insertIndex = -1;
        for (int i = 0; i < instructionsList.Count - 1; i++) {
            if (CheckInstructions(instructionsList, i, 4)) {
                insertIndex = i;
                break;
            }
        }

        if (insertIndex == -1) {
            Plugin.Logger.LogError("UpdateMoveTranspiler: Couldn't find third inject point!");
            return instructionsList;
        }

        CodeInstruction labeledInstuction3 = new CodeInstruction(OpCodes.Nop);
        labeledInstuction3.labels.Add(ifEnd2);

        newInstructions3.Add(new CodeInstruction(OpCodes.Ldloc_S, reduceDrag.LocalIndex));
        newInstructions3.Add(new CodeInstruction(OpCodes.Brfalse_S, ifEnd2));
        newInstructions3.Add(new CodeInstruction(OpCodes.Ldloc_S, (sbyte)10));
        newInstructions3.Add(new CodeInstruction(OpCodes.Ldloc_S, (sbyte)10));
        newInstructions3.Add(new CodeInstruction(OpCodes.Ldc_R4, 2.9f));
        newInstructions3.Add(new CodeInstruction(OpCodes.Div, (sbyte)10));
        newInstructions3.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(HypnosisScreenFXControllerPatch), "disorientedIntensity")));
        newInstructions3.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), "Lerp")));
        newInstructions3.Add(new CodeInstruction(OpCodes.Stloc_S, (sbyte)10));
        newInstructions3.Add(new CodeInstruction(OpCodes.Ldloc_0));
        newInstructions3.Add(new CodeInstruction(OpCodes.Ldloc_S, (sbyte)10));
        newInstructions3.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(Rigidbody), "drag")));
        newInstructions3.Add(labeledInstuction3);

        instructionsList.InsertRange(insertIndex, newInstructions3);

        if (!logged) {
            Plugin.Logger.LogDebug("UpdateMoveTranspiler: All done!");

            logged = true;
        }

        return instructionsList;
    }

    public static bool CheckInstructions(List<CodeInstruction> list, int index, int check)
    {
        bool retVal = false;
        if (check == 0) {
            retVal = list[index].opcode == OpCodes.Ldloc_S && list[index + 1].opcode == OpCodes.Mul && list[index + 2].opcode == OpCodes.Call && list[index].operand is LocalBuilder var1 && var1.LocalIndex == 7;
            if (retVal && !logged) {
                Plugin.Logger.LogDebug("UpdateMoveTranspiler: Found first inject point...");
            }
        }
        if (check == 1) {
            retVal = list[index].opcode == OpCodes.Brfalse && list[index - 1].opcode == OpCodes.Ldfld && Equals(list[index - 1].operand, AccessTools.Field(typeof(PlayerMotor), "underWater"));
            if (retVal && !logged) {
                Plugin.Logger.LogDebug("UpdateMoveTranspiler: Found target brfalse instruction...");
            }
        }
        if (check == 2) {
            retVal = list[index].opcode == OpCodes.Br && list[index - 1].opcode == OpCodes.Stloc_S && list[index - 1].operand is LocalBuilder var1 && var1.LocalIndex == 7 && list[index - 2].opcode == OpCodes.Ldfld;
            if (retVal && !logged) {
                Plugin.Logger.LogDebug("UpdateMoveTranspiler: Found target br instruction...");
            }
        }
        if (check == 3) {
            retVal = list[index].opcode == OpCodes.Ble_Un && list[index + 1].opcode == OpCodes.Ldloc_1 && list[index + 2].opcode == OpCodes.Ldloc_2;
            if (retVal && !logged) {
                Plugin.Logger.LogDebug("UpdateMoveTranspiler: Found target ble.un instruction...");
            }
        }
        if (check == 4) {
            try {
                retVal = list[index].opcode == OpCodes.Ldarg_0 && list[index - 1].opcode == OpCodes.Callvirt && (MethodInfo)list[index - 1].operand == AccessTools.PropertySetter(typeof(Rigidbody), "drag") && list[index + 1].opcode == OpCodes.Ldfld && (FieldInfo)list[index + 1].operand == AccessTools.Field(typeof(UnderwaterMotor), "fastSwimMode");
            } catch (ArgumentOutOfRangeException) {
                retVal = false;
            }

            if (retVal && !logged) {
                Plugin.Logger.LogDebug("UpdateMoveTranspiler: Found second inject point...");
            }
        }

        return retVal;
    }
}
