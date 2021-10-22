using BepInEx; // requires BepInEx.dll and BepInEx.Harmony.dll
using UnityEngine; // requires UnityEngine.dll, UnityEngine.CoreModule.dll
using HarmonyLib; // requires 0Harmony.dll
using System;
using Sonigon.Internal;
using Sonigon;

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
        private void Start()
        {
        }
    }
    
    [Serializable]
    [HarmonyPatch(typeof(TrickShot), "Awake")]
    class TrickShotPatchAwake
    {
		
        private static bool Prefix(TrickShot __instance)
        {
			// replace this component with a fixed one
			__instance.gameObject.AddComponent<FixedTrickShot>();
			UnityEngine.GameObject.Destroy(__instance);

			return false;
        }
	}

	public class FixedTrickShot : MonoBehaviour
	{
		private void Awake()
		{
			this.trail = base.transform.root.GetComponentInChildren<ScaleTrailFromDamage>();
		}

		private void Start()
		{
			this.projectileHit = base.GetComponentInParent<ProjectileHit>();
			this.move = base.GetComponentInParent<MoveTransform>();
			if (this.projectileHit != null)
			{
				if (this.soundGrowExplosion != null)
				{
					this.projectileHit.AddHitActionWithData(new Action<HitInfo>(this.SoundPlayGrowExplosion));
				}
				if (this.soundGrowWail != null)
				{
					this.soundGrowWailPlayed = true;
					SoundManager.Instance.Play(this.soundGrowWail, this.projectileHit.ownPlayer.transform);
				}
			}
		}

		public void SoundPlayGrowExplosion(HitInfo hit)
		{
			if (!this.soundGrowExplosionPlayed)
			{
				this.soundGrowExplosionPlayed = true;
				if (this.soundGrowExplosion != null)
				{
					SoundManager.Instance.PlayAtPosition(this.soundGrowExplosion, this.projectileHit.ownPlayer.transform, hit.point, new SoundParameterBase[]
					{
					this.soundIntensity
					});
				}
				if (this.soundGrowWailPlayed)
				{
					SoundManager.Instance.Stop(this.soundGrowWail, this.projectileHit.ownPlayer.transform, true);
				}
			}
		}
		// replaces Update from the vanilla game with FixedUpdate
		private void FixedUpdate()
		{
			if (this.move.distanceTravelled > this.removeAt)
			{
				UnityEngine.GameObject.Destroy(this);
				return;
			}
			this.soundIntensity.intensity = this.move.distanceTravelled / this.removeAt;
			float num = this.move.distanceTravelled - this.lastDistanceTravelled;
			this.lastDistanceTravelled = this.move.distanceTravelled;
			// replaces TimeHandler.deltaTime with TimeHandler.fixedDeltaTime
			float num2 = 1f + num * TimeHandler.fixedDeltaTime * base.transform.localScale.x * this.muiltiplier;
			this.projectileHit.damage *= num2;
			this.projectileHit.shake *= num2;
			if (this.trail)
			{
				this.trail.Rescale();
			}
		}

		[Header("Sound")]
		public SoundEvent soundGrowExplosion;

		public SoundEvent soundGrowWail;

		private bool soundGrowExplosionPlayed;

		private bool soundGrowWailPlayed;

		private SoundParameterIntensity soundIntensity = new SoundParameterIntensity(0f, UpdateMode.Once);

		[Header("Settings")]
		public float muiltiplier = 1f;

		public float removeAt = 30f;

		private ProjectileHit projectileHit;

		private MoveTransform move;

		private ScaleTrailFromDamage trail;

		private float lastDistanceTravelled;
	}

}
