using Godot;
using System.Collections.Generic;
using Project.Core;
using Project.CustomNodes;

namespace Project.Gameplay;

/// <summary>
/// Responsible for playing sfx/vfx. Controlled from the PlayerAnimator.
/// </summary>
public partial class PlayerEffect : Node3D
{
	private PlayerController Player;
	public void Initialize(PlayerController player)
	{
		Player = player;
		trailFX.Player = Player;

		// Rebuild dialog libraries to account for modded locales
		voiceLibrary?.LocalizeAudioStreams(true);

		SoundManager.instance.CharacterSpeechStarted += OnSpeakerStarted;
		SoundManager.instance.CharacterSpeechFinished += OnSpeakerFinished;

		voiceChannel.Finished += DisableSonicVoiceSfx;

		if (Player.IsDarkspineSonic) // Start darkspine FX
		{
			darkspineAuraSfx.Play();
			darkspineGroup.RestartGroup();
		}

		int augmentIndex = SaveManager.ActiveSkillRing.GetAugmentIndex(SkillKey.Character);
		if (augmentIndex != 0)
		{
			// Override modded voice library
			SkillResource skill = Runtime.Instance.SkillList.GetSkill(SkillKey.Character).GetAugment(augmentIndex);
			if (skill?.VoiceLibraryOverride != null)
				voiceLibrary = skill.VoiceLibraryOverride;
		}
	}

	public override void _PhysicsProcess(double _)
	{
		if (isFadingRailSFX)
			isFadingRailSFX = SoundManager.FadeAudioPlayer(grindrailSfx);
	}

	public readonly string JumpSfx = "jump";
	public readonly string JumpDashSfx = "jump dash";
	public readonly string BrakeSfx = "brake";
	public readonly string SlideSfx = "slide";
	public readonly string SplashSfx = "splash";
	public readonly string SidleSfx = "sidle";
	public readonly string StompSfx = "stomp";
	public readonly string WindSfx = "wind";
	public readonly string FireSfx = "fire";
	public readonly string DarkSfx = "dark";

	#region Actions
	// Actions (Jumping, sliding, etc)
	[ExportGroup("Skill Effects")]
	[Export] private SFXLibraryResource actionSFXLibrary;
	private readonly List<StringName> activeActionChannelKeys = [];
	private readonly List<AudioStreamPlayer> actionChannels = []; // Audio channels for playing action sound effects
	/// <summary> Plays a sound effect given an key (key names are based on actionSFXLibrary). </summary>
	public void PlayActionSFX(StringName key)
	{
		AudioStreamPlayer targetChannel = null;
		AudioStream targetStream = actionSFXLibrary.GetStream(key);

		for (int i = 0; i < actionChannels.Count; i++)
		{
			if (actionChannels[i].Playing &&
				actionChannels[i].Stream != targetStream)
			{
				// Audio channel is already busy playing a different sound effect
				continue;
			}

			targetChannel = actionChannels[i];
			activeActionChannelKeys[i] = key;
		}

		if (targetChannel == null) // Add new target channels as needed
		{
			targetChannel = new()
			{
				VolumeLinear = 0.9f,
				Bus = "GAME SFX"
			};

			GetChild(0).AddChild(targetChannel);
			actionChannels.Add(targetChannel);
			activeActionChannelKeys.Add(key);
		}

		targetChannel.Stream = targetStream;
		targetChannel.Play();
	}

	/// <summary> Stops all action channels with the given key. </summary> 
	public void AbortActionSFX(StringName key)
	{
		while (activeActionChannelKeys.Contains(key))
		{
			int index = activeActionChannelKeys.IndexOf(key);
			if (actionChannels[index].Playing)
				actionChannels[index].Stop();

			activeActionChannelKeys[index] = null;
		}
	}

	[Export] public Trail3D trailFX;
	[Export] public MeshInstance3D spinFX;
	public void UpdateTrailHueShift(float hueShift)
	{
		(trailFX.material as ShaderMaterial).SetShaderParameter("hue_shift", hueShift);
		(spinFX.MaterialOverride as ShaderMaterial).SetShaderParameter("hue_shift", hueShift);
	}

	[Export] private GroupGpuParticles3D teleportParticle;
	public void StartTeleport()
	{
		teleportParticle.RestartGroup();
		PlayActionSFX("teleport start");
	}
	public void StopTeleport()
	{
		teleportParticle.RestartGroup();
		PlayActionSFX("teleport end");
	}

	/// <summary> VFX for drifting dust. </summary>
	[Export] private GpuParticles3D dustParticle;
	public void StartDust() => dustParticle.Emitting = true;
	public void StopDust() => dustParticle.Emitting = false;

	public void StartTrailFX()
	{
		trailFX.IsEmitting = true;
		if (Player.IsDarkspineSonic)
			darkspineTrailFx.Emitting = true;
	}

	public void StopTrailFX()
	{
		trailFX.IsEmitting = false;
		if (Player.IsDarkspineSonic)
			darkspineTrailFx.Emitting = false;
	}

	public void StartSpinFX() => CreateTween().TweenProperty(spinFX, "transparency", 0.0f, .1f);
	public void StopSpinFX() => CreateTween().TweenProperty(spinFX, "transparency", 1.0f, .1f);
	public void CanelSpinFX() => spinFX.Transparency = 1f;

	[Export] private AnimationPlayer spinFXAnimator;
	public void StartSpinSquashFX()
	{
		spinFXAnimator.Seek(0.0, true);
		spinFXAnimator.Play("squish");
	}

	[Export] public GpuParticles3D doubleJumpFx;
	public void PlayDoubleJumpFX() => doubleJumpFx.Restart();

	[Export] private GpuParticles3D windParticle;
	public void PlayWindFX()
	{
		windParticle.Restart();
		PlayActionSFX(WindSfx);
	}

	[Export] private GpuParticles3D fireParticle;
	public void PlayFireFX()
	{
		fireParticle.Restart();
		PlayActionSFX(FireSfx);
	}

	[Export] private GroupGpuParticles3D stompParticle;
	public void StartStompFX()
	{
		stompParticle.SetEmitting(true);
		PlayActionSFX(StompSfx);
	}
	public void StopStompFX() => stompParticle.SetEmitting(false);

	[Export] private GroupGpuParticles3D splashJumpParticle;
	public void PlaySplashJumpFX() => splashJumpParticle.RestartGroup();

	[Export] private GpuParticles3D quickStepParticle;
	public void PlayQuickStepFX(bool isSteppingRight)
	{
		quickStepParticle.Rotation = isSteppingRight ? Vector3.Zero : Vector3.Up * Mathf.Pi;
		quickStepParticle.Restart();
		PlayActionSFX("quick step");
	}

	[Export] private GpuParticles3D lightDashParticle;
	public void StartLightDashFX()
	{
		lightDashParticle.Emitting = true;
		PlayActionSFX("light dash");
	}

	public void StopLightDashFX() => lightDashParticle.Emitting = false;

	[Export] private GroupGpuParticles3D aegisSlideParticle;
	public void StartAegisFX() => aegisSlideParticle.SetEmitting(true);
	public void StopAegisFX() => aegisSlideParticle.SetEmitting(false);
	[Export] private GroupGpuParticles3D volcanoSlideParticle;
	public void StartVolcanoFX() => volcanoSlideParticle.SetEmitting(true);
	public void StopVolcanoFX() => volcanoSlideParticle.SetEmitting(false);

	[Export] private GroupGpuParticles3D soulSlideParticle;
	public void StartSoulSlideFX() => soulSlideParticle.SetEmitting(true);
	public void StopSoulSlideFX() => soulSlideParticle.SetEmitting(false);

	[Export] private GpuParticles3D darkSpiralParticle;
	public void PlayDarkSpiralFX()
	{
		darkSpiralParticle.Restart();
		PlayActionSFX(DarkSfx);
	}

	[Export] private GroupGpuParticles3D darkCrestParticle;
	public void PlayDarkCrestFX() => darkCrestParticle.RestartGroup();
	[Export] private GroupGpuParticles3D windCrestParticle;
	public void PlayWindCrestFX() => windCrestParticle.RestartGroup();

	[Export] private GroupGpuParticles3D fireCrestParticle;
	public void PlayFireCrestFX() => fireCrestParticle.RestartGroup();

	[Export] private GpuParticles3D chargeParticle;
	[Export] private GpuParticles3D fullChargeParticle;
	public void StartChargeFX()
	{
		chargeParticle.Emitting = true;
		chargeParticle.Visible = true;
	}

	public void StopChargeFX()
	{
		chargeParticle.Emitting = false;
		fullChargeParticle.Emitting = false;
	}

	[Export] private GpuParticles3D grindrailSparkParticle;
	[Export] private GpuParticles3D grindrailBurstParticle;
	[Export] private GpuParticles3D perfectShuffleParticle;
	[Export] private AudioStreamPlayer grindrailSfx;
	private bool isFadingRailSFX;
	public void StartGrindFX(bool resetSFX)
	{
		grindrailSparkParticle.Emitting = true;
		isFadingRailSFX = false;

		if (resetSFX)
		{
			grindrailSfx.VolumeDb = 0f;
			grindrailSfx.Play();
		}
	}

	public void StartFullChargeFX()
	{
		chargeParticle.Emitting = false;
		chargeParticle.Visible = false;
		fullChargeParticle.Emitting = true;
		grindrailBurstParticle.Restart();
	}

	public void StopFullChargeFX()
	{
		if (chargeParticle.Visible && !fullChargeParticle.Emitting)
			return;

		chargeParticle.Restart();
		chargeParticle.Visible = true;
		fullChargeParticle.Emitting = false;
	}

	public void PerfectGrindShuffleFX()
	{
		perfectShuffleParticle.Restart();
		PlayActionSFX("perfect shuffle");
	}

	public void UpdateGrindFX(float speedRatio)
	{
		grindrailSfx.VolumeDb = -9f * Mathf.SmoothStep(0, 1, 1 - speedRatio); // Set sfx volume based on speed
	}

	public void StopGrindFX()
	{
		isFadingRailSFX = true; // Start fading sound effect
		grindrailSparkParticle.Emitting = false;
		StopChargeFX();
	}

	[Export] private GpuParticles3D petrifyParticle;
	public void PetrifyShatterFX()
	{
		petrifyParticle.Restart();
		PlayActionSFX("petrify shatter");
	}

	[Export] private GpuParticles3D darkspineTrailFx;
	[Export] private GroupGpuParticles3D darkspineGroup;
	[Export] private GroupGpuParticles3D darkspineSpinFX;
	[Export] private GpuParticles3D darkspineSpiritBombBurstVfx;
	[Export] private AudioStreamPlayer darkspineAuraSfx;
	[Export] private AudioStreamPlayer darkspineChargeSfx;
	public bool IsDarkspineSpinFxPlaying => darkspineSpinFX.IsGroupEmitting;

	public void StartDarkspineSpinFX(bool disableDarkspineGroupFx)
	{
		darkspineSpinFX.SetEmitting(true);

		if (disableDarkspineGroupFx)
			darkspineGroup.SetEmitting(false);

		darkspineAuraSfx.Stop();
		darkspineChargeSfx.Play();
	}

	public void StopDarkspineSpinFX()
	{
		darkspineSpinFX.SetEmitting(false);
		darkspineGroup.SetEmitting(true);

		darkspineAuraSfx.Play();
		darkspineChargeSfx.Stop();
	}

	public void PlayDarkspineSpiritBombBurst() => darkspineSpiritBombBurstVfx.Restart();

	#endregion

	#region Ground
	[ExportGroup("Material Effects")]
	// SFX for different ground materials (footsteps, landing, etc)
	[Export] private SFXLibraryResource materialSFXLibrary;
	private enum MaterialEnum
	{
		Pavement,
		Sand,
		Grass,
		Wood,
		Snow,
		Metal,
		Water,
		Rock,
		WetWood,
		WetRock,
		DustlessFloor, // Used in the final boss fight
		Count
	}

	[Export] private AudioStreamPlayer3D footstepChannel;
	[Export] private AudioStreamPlayer3D landingChannel;
	/// <summary> Index of the current type of ground the player is walking on. </summary>
	private MaterialEnum groundMaterial;
	private int GroundMaterialIndex => (int)groundMaterial;

	[Export] private GpuParticles3D[] landingParticles;
	/// <summary>
	/// Plays landing sfx and vfx based on the current groundKeyIndex.
	/// </summary>
	public void PlayLandingFX()
	{
		if (groundMaterial == MaterialEnum.DustlessFloor)
			return;

		if (groundMaterial == MaterialEnum.Water) // Water is a special case because it can be called from a Water DeathTrigger
		{
			PlayLandingWaterFX();
			return;
		}

		// Play landing sfx
		landingChannel.Stream = materialSFXLibrary.GetStream(materialSFXLibrary.GetKeyByIndex(GroundMaterialIndex), 1);
		landingChannel.Play();

		if (landingParticles.Length - 1 < currentStepEmitter || landingParticles[GroundMaterialIndex] == null) // Unimplemented VFX
			return;

		if (landingParticles[GroundMaterialIndex] is GroupGpuParticles3D)
			(landingParticles[GroundMaterialIndex] as GroupGpuParticles3D).RestartGroup();
		else
			landingParticles[GroundMaterialIndex].Restart();
	}

	/// <summary> Special method to play water splash fx. Also used by Water DeathTriggers. </summary>
	public void PlayLandingWaterFX(float heightOffset = 0)
	{
		PlayActionSFX(SplashSfx);
		GroupGpuParticles3D waterFx = landingParticles[(int)MaterialEnum.Water] as GroupGpuParticles3D;
		waterFx.Position = Vector3.Up * heightOffset;
		waterFx.RestartGroup();
	}

	/// <summary> Emitters responsible for dust when moving on the ground. </summary>
	[Export] private GpuParticles3D[] stepEmitters;
	/// <summary> Index of the current step emitter. </summary>
	private int currentStepEmitter = -1;
	/// <summary> Is step dust be emitted? </summary>
	public bool IsEmittingStepDust
	{
		get => currentStepEmitter != -1;
		set
		{
			if ((value && currentStepEmitter == GroundMaterialIndex) || (!value && !IsEmittingStepDust)) // Unnecessary assignment; return early
				return;

			// Start by disabling any current emission (if applicable)
			if (IsEmittingStepDust && stepEmitters.Length > currentStepEmitter && stepEmitters[currentStepEmitter] != null)
			{
				if (stepEmitters[currentStepEmitter] is GroupGpuParticles3D)
					(stepEmitters[currentStepEmitter] as GroupGpuParticles3D).SetEmitting(false);
				else
					stepEmitters[currentStepEmitter].Emitting = false;
			}

			if (!value) // Disabling emitters, return early
			{
				currentStepEmitter = -1;
				return;
			}

			currentStepEmitter = GroundMaterialIndex; // Update current step emitter based on current ground type

			if (stepEmitters.Length - 1 < currentStepEmitter || stepEmitters[currentStepEmitter] == null) // Validate that step emitter exists
				return;

			// Start the emitter
			if (stepEmitters[currentStepEmitter] is GroupGpuParticles3D)
				(stepEmitters[currentStepEmitter] as GroupGpuParticles3D).SetEmitting(true);
			else
				stepEmitters[currentStepEmitter].Emitting = true;
		}
	}

	/// <summary> Plays FXs that occur the moment a foot strikes the ground (i.e. SFX, Footprints, etc.). </summary>
	public void PlayFootstepFX(bool isRightFoot)
	{
		if (Player.IsDarkspineSonic && groundMaterial != MaterialEnum.Water) // No footsteps with Darkspine
			return;

		if (Mathf.IsZeroApprox(Player.MoveSpeed)) // Probably called during a blend to idle state; Ignore.
			return;

		// Update step emission speed and amount
		if (currentStepEmitter != -1 && stepEmitters[currentStepEmitter] != null)
		{
			float ratio = Player.Stats.GroundSettings.GetSpeedRatioClamped(Player.MoveSpeed);
			stepEmitters[currentStepEmitter].SpeedScale = ratio;
			stepEmitters[currentStepEmitter].AmountRatio = ratio;
		}

		footstepChannel.Stream = materialSFXLibrary.GetStream(materialSFXLibrary.GetKeyByIndex(GroundMaterialIndex), 0);
		footstepChannel.Play();

		Transform3D spawnTransform = isRightFoot ? Player.Animator.RightFoot.GlobalTransform : Player.Animator.LeftFoot.GlobalTransform;
		spawnTransform.Basis = GlobalTransform.Basis;

		switch (groundMaterial)
		{
			case MaterialEnum.Sand:
				CreateSandFootFX(spawnTransform); // Create a footprint
				break;
			case MaterialEnum.Water:
				CreateSplashFootFX(isRightFoot); // Create a ripple at the player's foot
				break;
		}
	}

	[Export] private PackedScene footprintDecal;
	private readonly List<Node3D> footprintDecalList = [];
	private void CreateSandFootFX(Transform3D spawnTransform)
	{
		Node3D activeFootprintDecal = null;
		for (int i = 0; i < footprintDecalList.Count; i++)
		{
			if (footprintDecalList[i].Visible) // Footprint is already active
				continue;

			activeFootprintDecal = footprintDecalList[i]; // Try to reuse decals if possible
		}

		if (activeFootprintDecal == null) // Create new footprint decal
		{
			activeFootprintDecal = footprintDecal.Instantiate<Node3D>();
			footprintDecalList.Add(activeFootprintDecal);
			StageSettings.Instance.AddChild(activeFootprintDecal);
		}

		// Reset fading animation
		AnimationPlayer animator = activeFootprintDecal.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		if (animator != null)
		{
			animator.Seek(0.0);
			animator.Play(animator.Autoplay);
		}
		activeFootprintDecal.GlobalTransform = spawnTransform;
		activeFootprintDecal.ResetPhysicsInterpolation();
	}

	[Export] private GpuParticles3D waterStep;
	private void CreateSplashFootFX(bool isRightFoot)
	{
		if (Player.IsDarkspineSonic)
			waterStep.GlobalPosition = GlobalPosition;
		else
			waterStep.GlobalPosition = isRightFoot ? Player.Animator.RightFoot.GlobalPosition : Player.Animator.LeftFoot.GlobalPosition;

		waterStep.ResetPhysicsInterpolation();

		const uint flags = (uint)GpuParticles3D.EmitFlags.Position + (uint)GpuParticles3D.EmitFlags.Velocity;
		waterStep.EmitParticle(waterStep.GlobalTransform, Player.Velocity * .2f, Colors.White, Colors.White, flags);
	}

	public void UpdateGroundType(Node collision)
	{
		// Loop through material keys and see if anything matches
		for (int i = 0; i < materialSFXLibrary.KeyCount; i++)
		{
			if (!collision.IsInGroup(materialSFXLibrary.GetKeyByIndex(i))) continue;

			groundMaterial = (MaterialEnum)i;
			return;
		}

		if (groundMaterial != MaterialEnum.Pavement) // Avoid being spammed with warnings
			groundMaterial = MaterialEnum.Pavement; // Default to pavement
	}
	#endregion

	[ExportGroup("Voices")]
	[Export] public SFXLibraryResource voiceLibrary;
	[Export] private AudioStreamPlayer voiceChannel;

	public void PlayVoice(StringName key, int sfxIndex = -1, bool forcePlay = false)
	{
		// Don't play anything if someone is already talking
		if (!forcePlay && (SoundManager.instance.IsDialogActive || voiceChannel.Playing))
			return;

		SoundManager.instance.IsSonicSfxVoiceChannelActive = true;
		voiceChannel.Stream = voiceLibrary.GetStream(key, SaveManager.GetCurrentVoiceLocaleIndex(), sfxIndex);
		voiceChannel.Play();
	}

	private void DisableSonicVoiceSfx() => SoundManager.instance.IsSonicSfxVoiceChannelActive = false;

	/// <summary> Stops any currently active voice clip and mutes the voice channel. </summary>
	private void OnSpeakerStarted(SoundManager.SpeakerEnum speaker)
	{
		if (speaker != SoundManager.SpeakerEnum.Sonic || !IsInstanceValid(voiceChannel))
			return;

		voiceChannel.Stop();
		voiceChannel.VolumeDb = -80f;
	}

	/// <summary> Stops any currently active voice clip and resets channel volume. </summary>
	private void OnSpeakerFinished(SoundManager.SpeakerEnum speaker)
	{
		if (speaker != SoundManager.SpeakerEnum.Sonic || !IsInstanceValid(voiceChannel))
			return;

		voiceChannel.Stop();
		voiceChannel.VolumeDb = 0f;
	}
}
