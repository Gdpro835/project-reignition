using Godot;
using Godot.Collections;
using Project.Core;

namespace Project.Interface.Menus;

public partial class WorldSelect : Menu
{
	[ExportGroup("Media Settings")]
	[Export] private BGMPlayer retailBgm;
	[Export] private VideoStreamPlayer[] videoPlayers;
	private VideoStreamPlayer ActiveVideoPlayer { get; set; }
	private VideoStreamPlayer PreviousVideoPlayer { get; set; }
	[Export] private AnimationPlayer storyIndicationAnimator;
	[Export] private StatusMenu statusMenu;

	private Color crossfadeColor;
	private float videoFadeFactor;
	private const float VideoCrossfadeSpeed = 5.0f;
	private int mouseSelection;

	[ExportGroup("Selection Settings")]
	[Export]
	private Description description;
	[Export]
	private Array<Rect2> levelSpriteRegions = [];
	[Export]
	private Array<string> levelDescriptionKeys = [];
	[Export]
	private Array<NodePath> levelTextSprites = [];
	[Export]
	private Array<NodePath> levelGlowSprites = [];
	[Export]
	private Array<NodePath> levelNewSprites = [];
	private Array<int> newLevelList = [];
	private readonly Array<Sprite2D> _levelTextSprites = [];
	private readonly Array<Sprite2D> _levelGlowSprites = [];
	private readonly Array<Control> _levelNewSprites = [];

	private int GetMaxWorld()
	{
		if (!SaveManager.Config.areLevelModsEnabled) // Hide Mod world option
			return (int)SaveManager.WorldEnum.Max - 1;

		return (int)SaveManager.WorldEnum.Max;
	}

	protected override void SetUp()
	{
		if (SaveManager.Config.useRetailMenuMusic)
			bgm = retailBgm; // Swap audio track to retail version

		for (int i = 0; i < levelTextSprites.Count; i++)
			_levelTextSprites.Add(GetNode<Sprite2D>(levelTextSprites[i]));

		for (int i = 0; i < levelGlowSprites.Count; i++)
			_levelGlowSprites.Add(GetNode<Sprite2D>(levelGlowSprites[i]));

		for (int i = 0; i < levelNewSprites.Count; i++)
			_levelNewSprites.Add(GetNode<Control>(levelNewSprites[i]));

		VerticalSelection = menuMemory[MemoryKeys.WorldSelect];
		if (videoPlayers != null && videoPlayers.Length > 0)
			ActiveVideoPlayer = videoPlayers[Mathf.Clamp(VerticalSelection, 0, videoPlayers.Length - 1)];

		if (menuMemory[MemoryKeys.ActiveMenu] == (int)MemoryKeys.LevelSelect) // Activate the correct submenu
			LoadIntoLevelSelect();
	}

	public override void ShowMenu()
	{
		newLevelList.Clear();
		int levelIndex = 0;
		foreach (Menu item in _submenus) // Update new level statuses
		{
			if (item is LevelSelect levelSelect && (item as LevelSelect).IsWorldUnlocked())
			{
				if (levelSelect.HasNewLevel())
					newLevelList.Add(levelIndex);

				levelIndex++;
			}
		}

		VerticalSelection = menuMemory[MemoryKeys.WorldSelect];
		menuMemory[MemoryKeys.ActiveMenu] = (int)MemoryKeys.WorldSelect;

		if (videoPlayers != null)
		{
			for (int i = 0; i < videoPlayers.Length; i++)
			{
				if (videoPlayers[i] == null || i == VerticalSelection)
					continue;

				videoPlayers[i].Paused = true;
				videoPlayers[i].Modulate = Colors.Transparent;
			}
		}

		if (animator.AssignedAnimation == "init" || animator.AssignedAnimation == "cancel")
			animator.Play("show-fade-video");
		else
			animator.Play(ShowAnimation);

		PreviousVideoPlayer = null;
		UpdateActiveVideoPlayer();
		UpdateStoryIndicator(false);
	}

	public override void _Process(double _)
	{
		if (ActiveVideoPlayer == null || !ActiveVideoPlayer.IsVisibleInTree())
			return;

		UpdateVideo();

		if (ActiveVideoPlayer.IsPlaying())
			videoFadeFactor = Mathf.MoveToward(videoFadeFactor, 1, VideoCrossfadeSpeed * PhysicsManager.normalDelta);
		else
			ActiveVideoPlayer.CallDeferred(VideoStreamPlayer.MethodName.Play);

		ActiveVideoPlayer.Modulate = Colors.Transparent.Lerp(Colors.White, videoFadeFactor);
		if (PreviousVideoPlayer != null)
			PreviousVideoPlayer.Modulate = crossfadeColor.Lerp(Colors.Transparent, videoFadeFactor);
	}

	protected override void ProcessMenu()
	{
		if (statusMenu != null && statusMenu.IsVisibleInTree())
			return;

		if (Runtime.Instance.MouseScrollInput != 0)
		{
			ScrollSelection(Runtime.Instance.MouseScrollInput);
			return;
		}

		if (Runtime.Instance.IsActionJustPressed("sys_pause", "ui_accept") && !Input.IsActionJustPressed("toggle_fullscreen"))
		{
			statusMenu?.ShowMenu();
			return;
		}

		base.ProcessMenu();
	}

	protected override void UpdateSelection()
	{
		int inputSign = Mathf.Sign(Input.GetAxis("ui_up", "ui_down"));
		if (inputSign != 0)
			ScrollSelection(inputSign);
	}

	private void ScrollSelection(int direction)
	{
		if (!Mathf.IsZeroApprox(cursorSelectionTimer))
			return;

		int max = GetMaxWorld();
		VerticalSelection = WrapSelection(VerticalSelection + direction, max);
		menuMemory[MemoryKeys.WorldSelect] = VerticalSelection;
		menuMemory[MemoryKeys.LevelSelect] = 0; // Reset level selection

		bool isScrollingUp = direction < 0;
		int transitionIndex = WrapSelection(isScrollingUp ? VerticalSelection - 1 : VerticalSelection + 1, max);
		UpdateSpriteRegion(3, transitionIndex); // Update level text

		animator.Play(isScrollingUp ? ScrollUpAnimation : ScrollDownAnimation);
		animator.Seek(0.0, true);
		DisableProcessing();
		UpdateStoryIndicator(false);
	}

	private void UpdateStoryIndicator(bool forceClose)
	{
		if (SaveManager.ActiveGameData.CurrentStoryLevel != null)
		{
			if (!forceClose && (int)SaveManager.ActiveGameData.CurrentStoryLevel.AreaKey == VerticalSelection && !TimeAttackManager.Instance.IsRunActive)
			{
				storyIndicationAnimator.Play("show");
				return;
			}
		}

		storyIndicationAnimator.Play(storyIndicationAnimator.AssignedAnimation.Equals("show") ? "hide" : "RESET");
	}

	protected override void Confirm()
	{
		// World hasn't been unlocked
		if (!SaveManager.ActiveGameData.IsWorldUnlocked((SaveManager.WorldEnum)VerticalSelection) && VerticalSelection != (int)SaveManager.WorldEnum.Mods) return;

		if (isConfirmedWithMouse && Runtime.Instance.IsUsingMouse)
		{
			if (mouseSelection == -100)
				return;

			if (mouseSelection != 0)
			{
				ScrollSelection(mouseSelection);
				return;
			}
		}

		UpdateStoryIndicator(true);
		base.Confirm();
	}

	public override void OpenParentMenu()
	{
		base.OpenParentMenu();

		foreach (VideoStreamPlayer player in videoPlayers)
			player.Stop();

		SaveManager.SaveGameData();

		if (!TimeAttackManager.Instance.IsRunActive)
		{
			GD.Print("Run is not active");
			SaveManager.ActiveSaveSlotIndex = -1;
			UpdateStoryIndicator(true);
		}
	}

	public override void OpenSubmenu()
	{
		SaveManager.ActiveGameData.lastPlayedWorld = (SaveManager.WorldEnum)VerticalSelection;
		_submenus[VerticalSelection].ShowMenu();
	}

	private void UpdateVideo()
	{
		// Don't change video?
		if (ActiveVideoPlayer == videoPlayers[VerticalSelection]) return;
		if (!SaveManager.ActiveGameData.IsWorldUnlocked((SaveManager.WorldEnum)VerticalSelection)) return; // World is locked
		if (!Mathf.IsZeroApprox(Input.GetAxis("ui_up", "ui_down"))) return; // Still scrolling


		if (ActiveVideoPlayer?.IsPlaying() == true)
		{
			videoFadeFactor = 0;
			crossfadeColor = ActiveVideoPlayer.Modulate;
			PreviousVideoPlayer = ActiveVideoPlayer;
			PreviousVideoPlayer.Paused = true;
		}

		UpdateActiveVideoPlayer();

	}

	private void UpdateActiveVideoPlayer()
	{
		if (videoPlayers == null || videoPlayers.Length == 0)
			return;

		if (VerticalSelection == (int)SaveManager.WorldEnum.Mods)
			ActiveVideoPlayer = videoPlayers[0];
		else
			ActiveVideoPlayer = videoPlayers[Mathf.Clamp(VerticalSelection, 0, videoPlayers.Length - 1)];

		if (ActiveVideoPlayer == null)
			return;

		ActiveVideoPlayer.Paused = false;
		if (!ActiveVideoPlayer.IsPlaying())
			ActiveVideoPlayer.Play();
	}

	public void UpdateLevelText()
	{
		UpdateSpriteRegion(0, VerticalSelection - 1); // Top option
		UpdateSpriteRegion(1, VerticalSelection); // Center option
		UpdateSpriteRegion(2, VerticalSelection + 1); // Bottom option

		for (int i = 0; i < _levelGlowSprites.Count; i++) // Sync glow regions
			_levelGlowSprites[i].RegionRect = _levelTextSprites[i].RegionRect;
	}

	private void UpdateSpriteRegion(int spriteIndex, int selectionIndex)
	{
		int max = GetMaxWorld();
		selectionIndex = WrapSelection(selectionIndex, max);
		if (!SaveManager.ActiveGameData.IsWorldUnlocked((SaveManager.WorldEnum)selectionIndex)) // World isn't unlocked.
			selectionIndex = levelSpriteRegions.Count - 1;

		if (!TimeAttackManager.Instance.IsRunActive)
			_levelNewSprites[spriteIndex].Visible = newLevelList.Contains(selectionIndex); // Update new notifications
		else
			_levelNewSprites[spriteIndex].Visible = false;

		_levelTextSprites[spriteIndex].RegionRect = levelSpriteRegions[selectionIndex];

		if (spriteIndex == 1) // Updating primary selection
		{
			description.ShowDescription();
			description.Text = levelDescriptionKeys[selectionIndex];
		}
	}

	private void ReceiveMouseInput(int direction)
	{
		Runtime.Instance.IsUsingMouse = true;
		mouseSelection = direction;
	}

	private void LoadIntoLevelSelect()
	{
		animator.Play("init-level-select");
		animator.Advance(0);
		VerticalSelection = (int)SaveManager.ActiveGameData.lastPlayedWorld;
		OpenSubmenu();
	}
}
