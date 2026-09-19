using Godot;

namespace Project.Interface.Menus;

[Tool]
public partial class VideoStreamFileLoadPlayer : VideoStreamPlayer
{
	[Export(PropertyHint.File)]
	private string videoFilePath;
	private bool loadAttempted;

	public void SetVideoFilePath(string path) => videoFilePath = path;

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
			return;

		// Do not initialize hidden video players. On Android the FFmpeg backend
		// allocates a decoder even when a VideoStreamPlayer is invisible.
		if (Visible)
			ReloadVideoPath();
	}

	public override void _Process(double _delta)
	{
		if (!Engine.IsEditorHint() && Visible && Stream == null && !loadAttempted)
			ReloadVideoPath();
	}

	public void ReloadVideoPath()
	{
		loadAttempted = true;
		if (string.IsNullOrEmpty(videoFilePath))
			return;

		if (!ResourceLoader.Exists(videoFilePath, "VideoStream"))
		{
			GD.PushWarning($"Couldn't load video file {videoFilePath}!");
			return;
		}

		Stream = ResourceLoader.Load<VideoStream>(videoFilePath, "VideoStream");
	}
}
