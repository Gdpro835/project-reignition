# Android port

This repository is being prepared for an Android 10+ APK with touch controls. The
Android target is deliberately configured in the project rather than through a
GitHub Actions workflow; CI workflow files are not part of this port.

## Current baseline

- Godot 4.7 .NET / C# project.
- Desktop renderer remains Compatibility for development.
- Android selects the Mobile renderer through `renderer/rendering_method.mobile`.
- Touchscreen input is enabled and mouse-to-touch emulation remains available for
  desktop testing.
- Existing keyboard and gamepad actions are unchanged.

## Export settings

Create an Android export preset locally in the Godot editor. The preset should use:

- Export path: `build/android/ProjectReignition.apk`
- Package / unique name: `com.projectreignition.game`
- Minimum SDK: Android 10 / API 29
- Architectures: arm64-v8a
- Orientation: landscape
- Renderer: Mobile
- Export mode: Release for a release APK, Debug for device diagnostics

`export_presets.cfg` is ignored by this repository on purpose. Do not add it to a
commit unless the project maintainers decide that editor export settings should be
versioned.

## Known blockers before a release build

1. `addons/ffmpeg/ffmpeg.gdextension` declares Android ARM64 libraries, but the
   corresponding files are not currently present in the checkout. Until those
   binaries are supplied, video playback is intentionally hidden on Android while
   its surrounding audio and animation timeline continues.
2. A first-pass touch HUD is now connected as an autoload. It feeds the existing
   actions (`move_*`, `ui_*`, `button_jump`, `button_action`, `button_attack`,
   `button_brake`, `button_speedbreak`, `button_timebreak`, and `sys_pause`).
   It is visible on Android and can be previewed in the editor by setting
   `mobile/touch_controls_preview` to `true`.
3. The Mobile renderer needs a device pass for post-processing, reflections,
   particles, and texture memory. The project is large enough that loading and
   memory testing must happen on a real device.
4. The release build must be tested with saves, localization, level streaming,
   and the party/network features disabled or enabled deliberately.

## Local validation

The first validation should be done with the Godot editor's Android export and a
physical arm64 device. The repository currently does not assume that the Android
SDK, .NET SDK, Godot executable, or export templates are installed in the build
sandbox.
