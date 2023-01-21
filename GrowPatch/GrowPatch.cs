using BepInEx; // requires BepInEx.dll and BepInEx.Harmony.dll
using UnityEngine; // requires UnityEngine.dll, UnityEngine.CoreModule.dll
using HarmonyLib; // requires 0Harmony.dll
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

// requires Assembly-CSharp.dll
namespace GrowPatch
{
    [BepInPlugin(ModId, ModName, "0.0.0")]
    [BepInProcess("Rounds.exe")]
    public class GrowPatch : BaseUnityPlugin
    {
        private const string ModId = "pykess.rounds.plugins.growpatch";
        private const string ModName = "Grow Patch";

        private void Awake()
        {
            new Harmony(ModId).PatchAll();
        }
    }
    
    [HarmonyPatch(typeof(TrickShot))]
    class TrickShotPatchAwake
    {
        [HarmonyPrefix]
        [HarmonyPatch("Awake")]
        private static void AwakePrefix(TrickShot __instance)
        {
            var data = __instance.gameObject.AddComponent<TrickShotData>();
        }

        [HarmonyTranspiler]
        [HarmonyPatch("Update")]
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            FieldInfo projectileHitField = AccessTools.Field(typeof(TrickShot), "projectileHit");
            FieldInfo projectileHitShakeField = AccessTools.Field(typeof(ProjectileHit), "shake");

            int startIndex = -1;
            int endIndex = -1;
            for (int i = 0; i < code.Count; i++)
            {
                var currentInstruction = code[i];

                // /* 0x0002CF8E */ IL_0082: ldfld     class ProjectileHit TrickShot::projectileHit	// Finds the value of a field in the object whose reference is currently on the evaluation stack.
                if (startIndex < 0 && currentInstruction.opcode == OpCodes.Ldfld && currentInstruction.LoadsField(projectileHitField))
                    startIndex = i;

                // /* 0x0002CFAE */ IL_00A2: stfld     float32 ProjectileHit::shake	// Replaces the value stored in the field of an object reference or pointer with a new value.
                if (endIndex < 0 && currentInstruction.opcode == OpCodes.Stfld && currentInstruction.StoresField(projectileHitShakeField))
                    endIndex = i;
            }

            if (startIndex < 0 || endIndex < 0)
            {
                UnityEngine.Debug.LogError($"[TrickShot] Update transpiler unable to find code block to replace");
                return code;
            }

            code.RemoveRange(startIndex, (endIndex - startIndex) + 1);
            code.InsertRange(startIndex, new List<CodeInstruction>
            {
                // TrickShotData.ApplyScaling(base.gameObject);
                CodeInstruction.Call(typeof(UnityEngine.Component), "get_gameObject"),
                CodeInstruction.Call(typeof(TrickShotData), nameof(TrickShotData.ApplyScaling), parameters: new []{ typeof(GameObject) })
            });

            return code;
        }
    }

    class TrickShotData : MonoBehaviour
    {
        public float baseDamage, baseShake, timeCounter, distanceCounter;
        
        private ProjectileHit projectileHit;
        private ScaleTrailFromDamage trail;

        private Vector3 lastPos;

        void Awake()
        {
            projectileHit = GetComponentInParent<ProjectileHit>();
            trail = transform.root.GetComponentInChildren<ScaleTrailFromDamage>();
        }

        void Start()
        {
            baseDamage = projectileHit.damage;
            baseShake = projectileHit.shake;
            timeCounter = 0;
            distanceCounter = 0;
            lastPos = transform.position;
        }

        void Update()
        {
            timeCounter += TimeHandler.deltaTime;
            distanceCounter += Vector3.Distance(transform.position, lastPos);
            lastPos = transform.position;
        }

        private void Apply()
        {
            //var factor = 1/Mathf.Pow(0.1f, timeCounter) + 1/Mathf.Pow(0.9f, distanceCounter);
            //projectileHit.damage = (baseDamage + factor) * (1 + (factor / 55f));
            ///projectileHit.shake = baseShake + factor;

            var factor = Mathf.Sqrt(timeCounter) * 25f + Mathf.Sqrt(distanceCounter) * 10f;
            projectileHit.damage = baseDamage + (baseDamage * (factor / 55f));

            UnityEngine.Debug.Log($"Time: {timeCounter}\nDistance: {distanceCounter}\nFactor: {factor}\n\n");
        }

        public static void ApplyScaling(GameObject projectile)
        {
            projectile.GetComponent<TrickShotData>().Apply();
        }
    }
}
