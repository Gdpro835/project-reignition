using Godot;
using Godot.Collections;
using Project.Core;

namespace Project.Interface.Menus;

public partial class StatusMenu : Menu
{
	[Export] private Array<Rect2> worldIconRegions = [];
	[Export] private Array<NodePath> worldRings = [];
	private Array<Sprite2D> _worldRings = [];

	[Export] private Label levelLabel;
	[Export] private Label ringLabel;
	[Export] private Label skillPointLabel;
	[Export] private Label fireSoulLabel;
	[Export] private Label soulGaugeLabel;
	[Export] private Label goldMedalLabel;
	[Export] private Label expLabel;
	[Export] private Label nextExpLabel;
	[Export] private Label nextMissionLabel;

	protected override void SetUp()
	{
		for (int i = 0; i < worldRings.Count; i++)
			_worldRings.Add(GetNode<Sprite2D>(worldRings[i]));
	}

	public override void ShowMenu()
	{
		levelLabel.Text = $"{SaveManager.ActiveGameData.level}/99";
		expLabel.Text = SaveManager.ActiveGameData.exp.ToString("00000000");

		int nextExp = 0;
		if (SaveManager.ActiveGameData.level < 99)
			nextExp = ExperienceResult.CalculateLevelUpRequirement(SaveManager.ActiveGameData.level + 1);
		nextExpLabel.Text = nextExp.ToString("00000000");
		ringLabel.Text = SaveManager.ActiveGameData.ringCount.ToString("00000000");
		skillPointLabel.Text = $"{SaveManager.ActiveSkillRing.TotalCost}/{SaveManager.ActiveSkillRing.MaxSkillPoints}";
		fireSoulLabel.Text = $"{SaveManager.ActiveGameData.LevelData.FireSoulCount}/{Gameplay.Objects.FireSoul.AchievementFireSoulRequirement}";
		if (TimeAttackManager.Instance.IsRunActive)
			goldMedalLabel.Text = $"{SaveManager.TimeData.GoldMedalCount}/{LevelResult.AchievementGoldTimeAttackRequirement}";
		else
			goldMedalLabel.Text = $"{SaveManager.ActiveGameData.LevelData.GoldMedalCount}/{LevelResult.AchievementGoldRequirement}";
		soulGaugeLabel.Text = SaveManager.ActiveGameData.CalculateMaxSoulPower(false).ToString("000");

		nextMissionLabel.Text = "-";
		if (SaveManager.ActiveGameData.CurrentStoryLevel != null)
			nextMissionLabel.Text = SaveManager.ActiveGameData.CurrentStoryLevel.AreaKey.ToString().ToSnakeCase();


		for (int i = 0; i < _worldRings.Count; i++)
		{
			// Check if world ring is unlocked (+1 because lost prologue doesn't have one.)
			bool isWorldRingObtained = SaveManager.ActiveGameData.IsWorldRingObtained((SaveManager.WorldEnum)i + 1);
			_worldRings[i].GetChild<Sprite2D>(0).Modulate = Colors.White.Lerp(Colors.Transparent, isWorldRingObtained ? 0 : 0.8f);
		}

		base.ShowMenu();
	}

	protected override void Cancel()
	{
		HideMenu();
	}
}
