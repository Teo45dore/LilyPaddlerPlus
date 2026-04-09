using System.Linq;
using HarmonyLib;

namespace LilyPaddlerPlus.Patches;

[HarmonyPatch(typeof(MiniWorld))]
public static class MiniWorldPatch {
    private static List<Transform> allAffected = new List<Transform>();
    private static float startTime = 0;
    private static float spinSpeed = 80f;

    static MiniWorldPatch() {
        HypnosisScreenFXControllerPatch.onStartDisorient += () => {
            startTime = Time.time;
            HypnosisScreenFXControllerPatch.afterUpdate += DoSpin;
        };

        HypnosisScreenFXControllerPatch.onStopDisorient += () => {
            HypnosisScreenFXControllerPatch.afterUpdate -= DoSpin;
            HypnosisScreenFXControllerPatch.afterUpdate += StopSpin;
        };

        ModConfig.ShowFalseValuesCfg.SettingChanged += (sender, e) => {
            if (startTime == -1) return;

            if (!ModConfig.ShowFalseValuesCfg.Value) {
                HypnosisScreenFXControllerPatch.afterUpdate -= DoSpin;
                HypnosisScreenFXControllerPatch.afterUpdate += StopSpin;
            }
        };
    }

    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    private static void StartPostfix(MiniWorld __instance) {
        if (__instance.GetComponentInParent<Seaglide>()) {
            allAffected.Add(__instance.transform.Find("HologramHolder").Find("MiniWorld"));
        }
    }

    private static void DoSpin() {
        allAffected = allAffected.Where(t => t != null).ToList();

        if (!HypnosisScreenFXControllerPatch.isSwirling && ModConfig.ShowFalseValuesCfg.Value) {
            if (startTime == -1) {
                startTime = Time.time;
            }
            foreach (Transform hologram in allAffected) {
                hologram.transform.localEulerAngles = -Vector3.up * ((Time.time - startTime) * spinSpeed);
            }
        }
    }

    private static void StopSpin() {
        allAffected = allAffected.Where(t => t != null).ToList();
        bool done = true;

        foreach (Transform hologram in allAffected) {
            if (hologram.transform.localEulerAngles.y < -360) hologram.transform.localEulerAngles = new Vector3(0, hologram.transform.localEulerAngles.y % 360, 0);
            
            if (Mathf.Abs(hologram.transform.localEulerAngles.y) > 0.1f && Mathf.Abs(hologram.transform.localEulerAngles.y - 360f) > 0.1f) {
                done = false;
            } else {
                hologram.transform.localEulerAngles = Vector3.zero;
                continue;
            }

            hologram.transform.localEulerAngles = Vector3.MoveTowards(hologram.transform.localEulerAngles, Vector3.zero, Mathf.Min(Mathf.Pow(hologram.transform.localEulerAngles.y / 360, 0.25f), spinSpeed * Time.time));
        }

        if (done) {
            HypnosisScreenFXControllerPatch.afterUpdate -= StopSpin;
            startTime = -1;
        }
    }
}