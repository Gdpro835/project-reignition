using Godot;
using Project.Core;

namespace Project.Interface;

public partial class Boot : Node
{
	private bool skipAndroidVideos;

	public override void _Ready()
	{
		TransitionManager.Instance.LoadCommonResources();
		skipAndroidVideos = OS.GetName().Equals("Android", System.StringComparison.OrdinalIgnoreCase);
		if (skipAndroidVideos)
		{
			DisableVideo(GetNodeOrNull<VideoStreamPlayer>("Logo/SEGAVideo"));
			DisableVideo(GetNodeOrNull<VideoStreamPlayer>("Logo/SFFVideo"));
		}
	}

	[Export] private AnimationPlayer animator;

	public override void _Process(double _delta)
	{
		if (skipAndroidVideos)
		{
			skipAndroidVideos = false;
			animator.Stop();
			StartTitleTransition();
			return;
		}

		if (!animator.IsPlaying())
			return;

		if (Input.IsActionJustPressed("sys_select"))
			AdvanceVideo();
	}

	private static void DisableVideo(VideoStreamPlayer player)
	{
		if (player == null)
			return;

		player.Stop();
		player.Stream = null;
		player.Visible = false;
	}

	private void StartTitleTransition()
	{
		TransitionManager.QueueSceneChange("res://interface/menu/Menu.tscn");
		TransitionManager.StartTransition(new()
		{
			inSpeed = .1f,
			outSpeed = .5f,
			color = Colors.Black
		});
	}

	private void AdvanceVideo() => animator.Advance(animator.CurrentAnimationLength);
}
