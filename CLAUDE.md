# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GGJ2026 is a 2D game prototype built for Global Game Jam 2026 using **Unity 6 (6000.3.5f2)** with the **Universal Render Pipeline (URP)**. Version control uses Plastic SCM (not Git).

## Build & Run

There are no CLI build/test scripts. All workflows go through the Unity Editor:

- **Open:** Unity Hub → select editor version `6000.3.5f2` (from `ProjectSettings/ProjectVersion.txt`)
- **Run:** Play mode in editor
- **Build:** File → Build Settings → export to a `Builds/` folder
- **Tests:** Window → General → Test Runner (Unity Test Framework is installed but no tests exist yet; place tests in `Assets/Tests/EditMode/` or `Assets/Tests/PlayMode/`)

## Architecture

Simple MonoBehaviour component pattern — no ECS, singletons, or service layers. The project is a rapid prototype.

- **Input:** Uses Unity's new Input System (`com.unity.inputsystem 1.17.0`) with preprocessor guards (`#if ENABLE_INPUT_SYSTEM`) that fall back to the legacy Input Manager
- **Physics:** Rigidbody2D-based movement via `MovePosition`
- **Rendering:** URP with 2D renderer, Global Light 2D, orthographic camera

## Code Conventions

- C# scripts go in `Assets/Scripts/`
- `PascalCase` for classes, public members, folder names; `camelCase` for locals and private fields
- File names must match class names
- Always keep `.meta` files alongside their assets — never move/rename an asset without its `.meta`
- Avoid editing `ProjectSettings/` unless intentional

## Key Packages

- `com.unity.inputsystem` — modern input handling
- `com.unity.render-pipelines.universal` — URP rendering
- `com.unity.2d.animation`, `com.unity.2d.tilemap`, `com.unity.2d.spriteshape` — 2D tooling
- `com.unity.test-framework` — unit/integration testing
