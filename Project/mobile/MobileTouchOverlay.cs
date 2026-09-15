using Godot;
using System.Collections.Generic;

namespace Project.Mobile;

/// <summary>Native C# touch overlay for Android builds.</summary>
public partial class MobileTouchOverlay : Control
{
    private const float JoystickRadius = 150f;
    private const float ButtonRadius = 54f;
    private const float EdgeMargin = 44f;

    private Vector2 joystickCenter;
    private Vector2 joystickPosition;
    private int joystickTouch = -1;
    private readonly Dictionary<int, string> activeButtons = new();
    private readonly Dictionary<string, Vector2> buttonCenters = new();

    private static readonly Dictionary<string, StringName> ButtonActions = new()
    {
        ["jump"] = "button_jump", ["action"] = "button_action",
        ["attack"] = "button_attack", ["brake"] = "button_brake",
        ["time"] = "button_timebreak", ["speed"] = "button_speedbreak",
        ["pause"] = "sys_pause"
    };

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ZIndex = 100;
        MouseFilter = MouseFilterEnum.Ignore;
        SetProcessInput(true);
        UpdateLayout();
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        bool mobile = OS.GetName().ToLowerInvariant() == "android" || OS.HasFeature("mobile");
        bool preview = (bool)ProjectSettings.GetSetting("mobile/touch_controls_preview", false);
        Visible = mobile || preview;
        if (Visible)
        {
            UpdateLayout();
            QueueRedraw();
        }
    }

    private void UpdateLayout()
    {
        Vector2 size = GetViewportRect().Size;
        joystickCenter = new Vector2(EdgeMargin + JoystickRadius, size.Y - EdgeMargin - JoystickRadius);
        if (joystickTouch < 0) joystickPosition = joystickCenter;
        buttonCenters.Clear();
        buttonCenters["jump"] = new Vector2(size.X - 150, size.Y - 150);
        buttonCenters["action"] = new Vector2(size.X - 280, size.Y - 245);
        buttonCenters["attack"] = new Vector2(size.X - 290, size.Y - 95);
        buttonCenters["brake"] = new Vector2(size.X - 430, size.Y - 120);
        buttonCenters["time"] = new Vector2(size.X - 425, size.Y - 255);
        buttonCenters["speed"] = new Vector2(size.X - 555, size.Y - 190);
        buttonCenters["pause"] = new Vector2(size.X - 70, 70);
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;
        if (@event is InputEventScreenTouch touch)
        {
            if (touch.Pressed) PressAt(touch.Index, touch.Position);
            else ReleaseTouch(touch.Index);
        }
        else if (@event is InputEventScreenDrag drag && drag.Index == joystickTouch)
            UpdateJoystick(drag.Position);
    }

    private void PressAt(int index, Vector2 position)
    {
        if (position.DistanceTo(joystickCenter) <= JoystickRadius * 1.35f && joystickTouch < 0)
        {
            joystickTouch = index;
            UpdateJoystick(position);
            return;
        }
        foreach (var button in buttonCenters)
        {
            if (position.DistanceTo(button.Value) <= ButtonRadius * 1.35f)
            {
                activeButtons[index] = button.Key;
                SetButtonActions(button.Key, true);
                QueueRedraw();
                return;
            }
        }
    }

    private void ReleaseTouch(int index)
    {
        if (index == joystickTouch)
        {
            joystickTouch = -1;
            joystickPosition = joystickCenter;
            SetJoystickActions(Vector2.Zero);
        }
        else if (activeButtons.Remove(index, out string key)) SetButtonActions(key, false);
        QueueRedraw();
    }

    private void SetButtonActions(string key, bool pressed)
    {
        if (!ButtonActions.TryGetValue(key, out StringName action)) return;
        if (pressed) Input.ActionPress(action); else Input.ActionRelease(action);
        if (key == "jump")
        {
            if (pressed) { Input.ActionPress("ui_accept"); Input.ActionPress("sys_select"); }
            else { Input.ActionRelease("ui_accept"); Input.ActionRelease("sys_select"); }
        }
        if (key == "action")
        {
            if (pressed) { Input.ActionPress("ui_select"); Input.ActionPress("sys_select"); }
            else { Input.ActionRelease("ui_select"); Input.ActionRelease("sys_select"); }
        }
        if (key == "pause") { if (pressed) Input.ActionPress("ui_cancel"); else Input.ActionRelease("ui_cancel"); }
    }

    private void UpdateJoystick(Vector2 position)
    {
        Vector2 offset = position - joystickCenter;
        joystickPosition = joystickCenter + offset.LimitLength(JoystickRadius);
        SetJoystickActions(offset / JoystickRadius);
        QueueRedraw();
    }

    private void SetJoystickActions(Vector2 axis)
    {
        float left = Mathf.Abs(axis.X) > .18f ? Mathf.Max(-axis.X, 0) : 0;
        float right = Mathf.Abs(axis.X) > .18f ? Mathf.Max(axis.X, 0) : 0;
        float up = Mathf.Abs(axis.Y) > .18f ? Mathf.Max(-axis.Y, 0) : 0;
        float down = Mathf.Abs(axis.Y) > .18f ? Mathf.Max(axis.Y, 0) : 0;
        SetAction("move_left", left); SetAction("move_right", right);
        SetAction("move_up", up); SetAction("move_down", down);
        SetAction("ui_left", left); SetAction("ui_right", right);
        SetAction("ui_up", up); SetAction("ui_down", down);
    }

    private static void SetAction(StringName action, float strength)
    {
        if (strength > 0) Input.ActionPress(action, strength); else Input.ActionRelease(action);
    }

    public override void _Draw()
    {
        if (!Visible) return;
        DrawCircle(joystickCenter, JoystickRadius, new Color(0.08f, 0.1f, 0.14f, .55f));
        DrawArc(joystickCenter, JoystickRadius, 0, Mathf.Tau, 48, new Color(.75f, .85f, 1, .8f), 4);
        DrawCircle(joystickPosition, 58, new Color(.3f, .58f, .95f, .8f));
        var labels = new Dictionary<string, string>
        {
            ["jump"] = "J", ["action"] = "A", ["attack"] = "AT",
            ["brake"] = "B", ["time"] = "T", ["speed"] = "S", ["pause"] = "P"
        };
        Font font = ThemeDB.FallbackFont;
        foreach (var button in buttonCenters)
        {
            bool pressed = false;
            foreach (string active in activeButtons.Values) if (active == button.Key) pressed = true;
            DrawCircle(button.Value, ButtonRadius, pressed ? new Color(.35f, .65f, 1, .92f) : new Color(.08f, .1f, .14f, .65f));
            DrawArc(button.Value, ButtonRadius, 0, Mathf.Tau, 32, new Color(.8f, .9f, 1, .85f), 3);
            string label = labels[button.Key];
            Vector2 textSize = font.GetStringSize(label, HorizontalAlignment.Left, -1, 26);
            DrawString(font, button.Value + new Vector2(-textSize.X / 2, 9), label, HorizontalAlignment.Left, -1, 26, Colors.White);
        }
    }
}
