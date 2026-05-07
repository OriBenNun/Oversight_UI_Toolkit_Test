# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 6000.4.5f1 LTS assignment: build a custom high-performance Tree View using **UI Toolkit only** (not Unity's built-in TreeView). Target: ~2,500 nodes, 4–6 hierarchy levels.

Open in Unity Hub → select `Oversight_UI_Toolkit_Test/` folder. No build scripts — use Unity Editor directly.

Run tests: Unity Editor → Window → General → Test Runner → Run All.

## Assignment Constraints

- **Time budget**: 5–8 hours total
- **No built-in TreeView** — must implement from scratch
- Must deliver: running project, README, Windows build, .git history intact
- AI usage is encouraged but must be disclosed in README

## Architecture

Strict separation of concerns across these layers:

| Layer | Responsibility |
|---|---|
| **Data Model** | `TreeNode` with `NodeId`, `ParentId`, `Children`, `DisplayName`, `NodeType`, `IsExpanded`, `IsVisible` |
| **Tree Index** | `GetNodeById(id)`, `RevealNode(id)` (auto-expands ancestors), flat list generation for virtualized view |
| **Interaction Logic** | Selection persistence through expand/collapse/filter/drag-drop; visibility toggle rules |
| **Drag/Drop Validator** | Prevents invalid moves (e.g., node into its own descendant); model integrity |
| **UI Toolkit View** | `ListView`-based virtualized renderer; binds to flat visible-row list |

## Key Technical Decisions

**Virtualization**: Use `ListView` (not manual VisualElement pooling). `ListView` renders only visible rows — critical for 2,500+ node performance. The data source is a flat `List<TreeNode>` of currently-visible rows rebuilt on expand/collapse/filter.

**Visibility toggle semantics**: Items toggle only themselves. Groups toggle all descendants recursively. These are independent of IsExpanded.

**Search/filter**: Non-destructive — filter produces a separate visible set, does not mutate the model. Show ancestors of matching nodes to preserve context.

**Selection persistence**: Selection is by `NodeId`, not list index. Re-resolve after any operation that rebuilds the flat list.

**Drag/drop**: Validate in `DragUpdated` (reject if target is descendant of dragged node or same parent/position). Commit only in `DragPerform`.

## Packages

- `com.unity.modules.uielements` — UI Toolkit (ListView, VisualElement, USS)
- `com.unity.test-framework 1.6.0` — NUnit-based; use for data model and index logic unit tests
- `com.unity.inputsystem 1.19.0` — available for keyboard navigation bonus

## Coding Conventions

C# only. Place runtime code under `Assets/Scripts/`, editor-only code under `Assets/Scripts/Editor/`. USS stylesheets alongside their UXML files. No third-party packages.
