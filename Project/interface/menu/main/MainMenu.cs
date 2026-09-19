using Godot;
using Project.Core;

namespace Project.Interface.Menus;

public partial class MainMenu : Menu
{
	[Export] private Description description;
	[Export] private Control[] menuItemAnchorPoints;
	[Export] private Node2D cursor;
	[Export] private AnimationPlayer cursorAnimator;
	private Vector2 cursorVelocity;
	private const float CursorSmoothing = .08f;

	private int currentSelection;
	private bool isNothingSelected;
	private bool isMenuInitialized;

	private bool IsTimeAttackUnlocked => SaveManager.SharedData.IsTimeAttackUnlocked;
	private bool IsPartyModeUnlocked => false; // TODO Enable this after party mode is complete

	public override void ShowMenu()
	{
		if (!IsTimeAttackUnlocked)
		{
			animator.Play("disable-ta");
			animator.Advance(0);
		}

		if (!IsPartyModeUnlocked)
		{
			animator.Play("disable-party");
			animator.Advance(0);
		}

		base.ShowMenu();

		if (Runtime.Instance.IsUsingMouse)
			isNothingSelected = true;
		cursorVelocity = Vector2.Zero;
		if (cursor != null && menuItemAnchorPoints != null && menuItemAnchorPoints.Length > 0)
		{
			currentSelection = Mathf.Clamp(currentSelection, 0, menuItemAnchorPoints.Length - 1);
			if (menuItemAnchorPoints[currentSelection] != null)
				cursor.Position = menuItemAnchorPoints[currentSelection].Position;
		}
		menuMemory[MemoryKeys.ActiveMenu] = (int)MemoryKeys.MainMenu;
	}

	protected override void SetUp()
	{
		currentSelection = menuMemory[MemoryKeys.MainMenu];
		UpdateSelectionValues();
		if (menuMemory[MemoryKeys.ActiveMenu] == (int)MemoryKeys.MainMenu)
			CallDeferred(MethodName.ShowMenu);
	}

	private void UpdateSelectionValues()
	{
		if (currentSelection <= 1)
		{
			HorizontalSelection = currentSelection;
			VerticalSelection = 0;
		}
		else if (currentSelection == 2)
		{
			VerticalSelection = 1;
		}
		else
		{
			HorizontalSelection = currentSelection - 3;
			VerticalSelection = 2;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (cursor != null && menuItemAnchorPoints != null &&
			currentSelection >= 0 && currentSelection < menuItemAnchorPoints.Length &&
			menuItemAnchorPoints[currentSelection] != null)
		{
			cursor.GlobalPosition = cursor.GlobalPosition.SmoothDamp(
				menuItemAnchorPoints[currentSelection].GlobalPosition,
				ref cursorVelocity, CursorSmoothing);
		}
		base._PhysicsProcess(delta);
	}

	public override void EnableProcessing()
	{
		base.EnableProcessing();
		GD.Print("Processing!");
	}

	public override void DisableProcessing()
	{
		base.DisableProcessing();
		GD.Print("Not Processing!");
	}

	private void FinishShowingMenu()
	{
		isMenuInitialized = true;

		if (isQuitMenuActive)
			return;

		if (isNothingSelected)
		{
			EnableProcessing();
			return;
		}

		animator.Play($"select-{currentSelection}");
		cursorAnimator.Play("show");
	}

	protected override void ProcessMenu()
	{
		if (Runtime.Instance.IsActionJustPressed("sys_pause", "ui_accept", "escape") && !Input.IsActionJustPressed("toggle_fullscreen"))
		{
			if (isQuitMenuActive)
				CancelQuitMenu();
			else
				ShowQuitMenu();

			return;
		}

		base.ProcessMenu();
	}

	private void ShowQuitMenu()
	{
		isQuitMenuActive = true;
		if (Runtime.Instance.IsUsingMouse)
		{
			quitAnimator.Play("select-none");
			quitSelection = -1;
		}
		else
		{
			quitAnimator.Play("select-no");
			quitSelection = 1;
		}
		quitAnimator.Advance(0.0);

		quitAnimator.Play("quit-text");
		quitAnimator.Advance(0.0);

		quitAnimator.Play("show");
	}

	[Export] private AnimationPlayer quitAnimator;
	private bool isQuitMenuActive;
	private int quitSelection;
	private void CancelQuitMenu()
	{
		quitSelection = 1;
		quitAnimator.Play("select-no");
		quitAnimator.Advance(0.0);

		isQuitMenuActive = false;
		quitAnimator.Play("hide");
	}

	private void UpdateQuitMenuVisuals()
	{
		if (quitSelection == -1)
		{
			quitAnimator.Play("select-none");
			return;
		}

		quitAnimator.Play(quitSelection == 0 ? "select-yes" : "select-no");
	}

	protected override void UpdateSelection()
	{
		Runtime.Instance.IsUsingMouse = false;
		if (isQuitMenuActive)
		{
			int input = Mathf.Sign(Input.GetAxis("ui_left", "ui_right"));
			if ((input > 0 && quitSelection != 1) || (input < 0 && quitSelection != 0))
			{
				quitSelection = input > 0 ? 1 : 0;
				UpdateQuitMenuVisuals();
			}

			return;
		}

		if (isNothingSelected)
		{
			ChangeSelection(currentSelection);
			StartSelectionTimer();
			return;
		}

		VerticalSelection = Mathf.Clamp(VerticalSelection + Mathf.Sign(Input.GetAxis("ui_up", "ui_down")), 0, 2);
		if (VerticalSelection != 1)
			HorizontalSelection = Mathf.Clamp(HorizontalSelection + Mathf.Sign(Input.GetAxis("ui_left", "ui_right")), 0, 1);

		int targetSelection;
		if (VerticalSelection == 0)
			targetSelection = HorizontalSelection;
		else if (VerticalSelection == 1)
			targetSelection = 2;
		else
			targetSelection = 3 + HorizontalSelection;

		if (targetSelection != currentSelection)
		{
			ChangeSelection(targetSelection);
			StartSelectionTimer();
		}
	}

	private void ChangeSelection(int newSelection)
	{
		isNothingSelected = false;
		currentSelection = newSelection;
		description.ShowDescription();
		menuMemory[MemoryKeys.MainMenu] = currentSelection;
		animator.Play("select");
		animator.Advance(0.0);
		animator.Play($"select-{currentSelection}");
	}

	protected override void Confirm()
	{
		if (isQuitMenuActive)
		{
			if (quitSelection == -1)
				return;

			if (quitSelection == 0)
			{
				quitAnimator.Play("confirm");
			}
			else
				CancelQuitMenu();

			return;
		}

		if (isNothingSelected)
			return;

		GD.PrintT(currentSelection, IsTimeAttackUnlocked, IsPartyModeUnlocked);
		if (currentSelection == 1 && !IsTimeAttackUnlocked)
			return;

		if (currentSelection == 2 && !IsPartyModeUnlocked)
			return;


		animator.Play("confirm");
	}

	protected override void Cancel()
	{
		if (isQuitMenuActive)
		{
			CancelQuitMenu();
			return;
		}

		animator.Play("cancel");
	}

	public override void OpenSubmenu()
	{
		if (currentSelection == 0)
		{
			_submenus[currentSelection].ShowMenu();
			return;
		}

		if (currentSelection == 1)
		{
			TransitionManager.QueueSceneChange(TransitionManager.TimeAttackScenePath);
			TransitionManager.StartTransition(new()
			{
				color = Colors.Black,
				inSpeed = .5f,
			});
		}

		if (currentSelection < 2)
			return;

		FadeBgm(.5f);
		menuMemory[MemoryKeys.MainMenu] = currentSelection;

		switch (currentSelection)
		{
			case 2:
				TransitionManager.QueueSceneChange(TransitionManager.PartyScenePath);
				break;
			case 3:
				TransitionManager.QueueSceneChange(TransitionManager.SpecialBookScenePath);
				break;
			default:
				TransitionManager.QueueSceneChange(TransitionManager.OptionsScenePath);
				break;
		}

		TransitionManager.StartTransition(new()
		{
			color = Colors.Black,
			inSpeed = .5f,
		});
	}

	private void StartQuitTransition()
	{
		TransitionManager.Instance.Connect(TransitionManager.SignalName.TransitionProcess, new(this, MethodName.QuitGame));
		TransitionManager.StartTransition(new()
		{
			color = Colors.Black,
			inSpeed = 1f,
		});
	}
	private void QuitGame() => GetTree().Quit();

	private void ReceiveMouseInput(int index)
	{
		if (!isProcessing || !isMenuInitialized)
			return;

		Runtime.Instance.IsUsingMouse = true;

		if (isQuitMenuActive)
		{
			quitSelection = index;
			UpdateQuitMenuVisuals();
			return;
		}

		if (index < 0)
		{
			isNothingSelected = true;
			animator.Play("select-none");
			return;
		}

		ChangeSelection(index);
	}
}
