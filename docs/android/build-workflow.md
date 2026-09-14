# Android build workflow

The workflow is intentionally not committed to `.github/workflows/`. Create
`.github/workflows/android.yml` yourself using the YAML supplied in the agent
response. It will copy `docs/android/export_presets.cfg.example` to the ignored
`Project/export_presets.cfg`, import the project, export an unsigned arm64 APK,
and upload it as a workflow artifact.

The workflow does not sign the APK. Signing keys should never be committed to
this repository or pasted into chat.
