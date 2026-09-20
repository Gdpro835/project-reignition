using Godot;
using System;

namespace Project.Interface.Menus;

public partial class JukeboxOption : Control
{

	///<summary> Reference to the target BGM Resource. </summary>
	[Export] public BGMResource Bgm { get; private set; }

	[Export] private Label name;
	[Export] private AnimationPlayer animator;

	public bool Equipped { get; private set; }

	public void SetBgmResource(BGMResource resource)
	{
		Bgm = resource;
		if (Bgm == null)
		{
			if (name != null)
				name.Text = string.Empty;
			return;
		}

		if (name == null)
			return;

		name.Text = string.IsNullOrEmpty(Bgm.SongName) ? string.Empty : Bgm.SongName.GetBaseName();
	}

	public void Equip()
	{
		animator.Play("equip");
		Equipped = true;
	}

	public void Unequip()
	{
		animator.Play("unequip");
		Equipped = false;
	}
}
