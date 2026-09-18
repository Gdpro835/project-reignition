using Godot;

namespace Project.Interface.Menus;

[Tool]
public partial class VideoStreamFileLoadPlayer : VideoStreamPlayer
{
	[Export(PropertyHint.File)]
	private string videoFilePath;

	public void SetVideoFilePath(string path) => videoFilePath = path;

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
			return;

		ReloadVideoPath();
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
