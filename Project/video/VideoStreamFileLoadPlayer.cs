using Godot;

namespace Project.Interface.Menus;

[Tool]
public partial class VideoStreamFileLoadPlayer : VideoStreamPlayer
{
	[Export(PropertyHint.File)]
	private string videoFilePath;
	private bool androidVideoFallback;
	public void SetVideoFilePath(string path) => videoFilePath = path;

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
			return;

		// The repository does not ship the FFmpeg Android ARM64 binaries yet. Keep
		// the surrounding animation/audio timeline usable instead of trying to load
		// an unavailable MP4 decoder on Android.
		androidVideoFallback = OS.HasFeature("android");
		if (androidVideoFallback)
		{
			Stream = null;
			Visible = false;
			return;
		}

		ReloadVideoPath();
	}

	public override void _Process(double _delta)
	{
		if (androidVideoFallback)
			Visible = false;
	}

	public void ReloadVideoPath()
	{
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