extends CanvasLayer
## First-pass Android touch controls.
## The overlay feeds the existing InputMap actions, so gameplay and menus do not
## need a separate mobile input path.

const JOYSTICK_RADIUS := 150.0
const BUTTON_RADIUS := 54.0
const EDGE_MARGIN := 44.0

var joystick_center := Vector2.ZERO
var joystick_position := Vector2.ZERO
var joystick_touch := -1
var active_buttons: Dictionary = {}
var button_centers: Dictionary = {}
var button_actions := {
	"jump": "button_jump",
	"action": "button_action",
	"attack": "button_attack",
	"brake": "button_brake",
	"time": "button_timebreak",
	"speed": "button_speedbreak",
	"pause": "sys_pause"
}

func _ready() -> void:
	layer = 100
	set_process_input(true)
	queue_redraw()

func _process(_delta: float) -> void:
	var scene := get_tree().current_scene
	var is_android := OS.has_feature("android")
	var preview := bool(ProjectSettings.get_setting("mobile/touch_controls_preview", false))
	var in_boot := scene != null and scene.scene_file_path == "res://interface/boot/Boot.tscn"
	visible = (is_android or preview) and not in_boot
	if visible:
		_update_layout()
		queue_redraw()

func _notification(what: int) -> void:
	if what == NOTIFICATION_RESIZED:
		_update_layout()
		queue_redraw()

func _update_layout() -> void:
	var size := get_viewport().get_visible_rect().size
	joystick_center = Vector2(EDGE_MARGIN + JOYSTICK_RADIUS, size.y - EDGE_MARGIN - JOYSTICK_RADIUS)
	if joystick_touch < 0:
		joystick_position = joystick_center
	button_centers = {
		"jump": Vector2(size.x - 150.0, size.y - 150.0),
		"action": Vector2(size.x - 280.0, size.y - 245.0),
		"attack": Vector2(size.x - 290.0, size.y - 95.0),
		"brake": Vector2(size.x - 430.0, size.y - 120.0),
		"time": Vector2(size.x - 425.0, size.y - 255.0),
		"speed": Vector2(size.x - 555.0, size.y - 190.0),
		"pause": Vector2(size.x - 70.0, 70.0)
	}

func _input(event: InputEvent) -> void:
	if not visible:
		return
	if event is InputEventScreenTouch:
		if event.pressed:
			_press_at(event.index, event.position)
		else:
			_release_touch(event.index)
	elif event is InputEventScreenDrag:
		if event.index == joystick_touch:
			_update_joystick(event.position)

func _press_at(index: int, position: Vector2) -> void:
	if position.distance_to(joystick_center) <= JOYSTICK_RADIUS * 1.35 and joystick_touch < 0:
		joystick_touch = index
		_update_joystick(position)
		return
	for key in button_centers:
		if position.distance_to(button_centers[key]) <= BUTTON_RADIUS * 1.35:
		active_buttons[index] = key
		_set_button_actions(key, true)
		queue_redraw()
		return

func _release_touch(index: int) -> void:
	if index == joystick_touch:
		joystick_touch = -1
		joystick_position = joystick_center
		_set_joystick_actions(Vector2.ZERO)
	elif active_buttons.has(index):
		var key: String = active_buttons[index]
		_set_button_actions(key, false)
		active_buttons.erase(index)
	queue_redraw()

func _set_button_actions(key: String, pressed: bool) -> void:
	var actions: Array[StringName] = [StringName(button_actions[key])]
	match key:
		"jump":
			actions.append("ui_accept")
		"action":
			actions.append("ui_select")
		"pause":
			actions.append("ui_cancel")
	for action in actions:
		if pressed:
			Input.action_press(action)
		else:
			Input.action_release(action)

func _update_joystick(position: Vector2) -> void:
	var offset := position - joystick_center
	joystick_position = joystick_center + offset.limit_length(JOYSTICK_RADIUS)
	_set_joystick_actions(offset / JOYSTICK_RADIUS)
	queue_redraw()

func _set_joystick_actions(axis: Vector2) -> void:
	var deadzone := 0.18
	var left := maxf(-axis.x, 0.0) if absf(axis.x) > deadzone else 0.0
	var right := maxf(axis.x, 0.0) if absf(axis.x) > deadzone else 0.0
	var up := maxf(-axis.y, 0.0) if absf(axis.y) > deadzone else 0.0
	var down := maxf(axis.y, 0.0) if absf(axis.y) > deadzone else 0.0
	_set_action("move_left", left)
	_set_action("move_right", right)
	_set_action("move_up", up)
	_set_action("move_down", down)
	_set_action("ui_left", left)
	_set_action("ui_right", right)
	_set_action("ui_up", up)
	_set_action("ui_down", down)

func _set_action(action: StringName, strength: float) -> void:
	if strength > 0.0:
		Input.action_press(action, strength)
	else:
		Input.action_release(action)

func _draw() -> void:
	if not visible:
		return
	_update_layout()
	_draw_joystick()
	for key in button_centers:
		var pressed := active_buttons.values().has(key)
		_draw_button(button_centers[key], key.to_upper().left(1), pressed)

func _draw_joystick() -> void:
	draw_circle(joystick_center, JOYSTICK_RADIUS, Color(0.08, 0.10, 0.14, 0.42))
	draw_arc(joystick_center, JOYSTICK_RADIUS, 0.0, TAU, 48, Color(0.75, 0.85, 1.0, 0.7), 4.0)
	draw_circle(joystick_position, 58.0, Color(0.30, 0.58, 0.95, 0.72))

func _draw_button(center: Vector2, label: String, pressed: bool) -> void:
	var color := Color(0.35, 0.65, 1.0, 0.88) if pressed else Color(0.08, 0.10, 0.14, 0.54)
	draw_circle(center, BUTTON_RADIUS, color)
	draw_arc(center, BUTTON_RADIUS, 0.0, TAU, 32, Color(0.8, 0.9, 1.0, 0.75), 3.0)
	var font := ThemeDB.fallback_font
	var text_size := font.get_string_size(label, HORIZONTAL_ALIGNMENT_LEFT, -1, 26)
	draw_string(font, center - Vector2(text_size.x / 2.0, -9.0), label, HORIZONTAL_ALIGNMENT_LEFT, -1, 26, Color.WHITE)
