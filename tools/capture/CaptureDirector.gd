extends Node

var scenario := "starting-room"

func _ready() -> void:
	for argument in OS.get_cmdline_user_args():
		if argument.begins_with("--scenario="):
			scenario = argument.trim_prefix("--scenario=")
	call_deferred("_prepare_capture")

func _prepare_capture() -> void:
	await get_tree().process_frame
	$GameRoot/Hud/StartOverlay.hide()
	$GameRoot/LevelManager.BeginGame()

	match scenario:
		"crawler-edge":
			await _press("move_right")
		"first-branch":
			await _repeat("move_right", 3)
		"lamp-tether":
			await _press("lamp_toggle")
			await _repeat("move_right", 3)
		"flask-darkness":
			await _repeat("move_right", 4)
		"stair-clear":
			$CaptureBridge.StageStairClear()
		"loss-depleted":
			$CaptureBridge.StageDepleted()
		"loss-smothered":
			$CaptureBridge.StageSmothered()

func _repeat(action: StringName, count: int) -> void:
	for index in count:
		await _press(action)

func _press(action: StringName) -> void:
	var event := InputEventAction.new()
	event.action = action
	event.pressed = true
	Input.parse_input_event(event)
	await get_tree().process_frame
