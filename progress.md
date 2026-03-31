# Progress Log

## 2026-03-25
- Started architecture and wiring review.
- Confirmed playable scene setup centers on `GameScene`.
- Identified `ManagersRoot.prefab` and `GamePlayRoot.prefab` as primary runtime roots.
- Inspected service registration, selection/command flow, building placement, movement, save/load, UI, audio/settings, and flow-field integration.
- Confirmed that the current runtime is mostly NavMesh-based, with flow-field support present but not actively wired as the default path.
