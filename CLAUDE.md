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

## File Structure

```
Assets/Scripts/
├── Model/          TreeNode.cs, NodeType.cs          (namespace Oversight.Model)
├── Index/          TreeIndex.cs                       (namespace Oversight.Index)
├── Logic/          TreeInteractionLogic.cs, DragDropValidator.cs  (namespace Oversight.Logic)
├── UI/             TreeViewController.cs              (namespace Oversight.UI)
├── Data/           NodeData.cs, TreeDataAsset.cs      (namespace Oversight.Data)
├── Tests/          TreeNodeTests.cs, TreeIndexTests.cs
└── Editor/         TreeDataGenerator.cs, TreeDataGeneratorEditor.cs  (namespace Oversight.Editor)

Assets/UI/
├── TreeView.uxml
└── TreeView.uss
```

Each layer has its own `.asmdef` for compile isolation. Editor assembly (`OversightTreeEditor`) is excluded from runtime builds.

## Architecture

| Layer | File | Key API |
|---|---|---|
| **Data Model** | `Model/TreeNode.cs` | `NodeId`, `ParentId`, `Children` (IReadOnlyList), `IsExpanded`, `IsVisible`, `SetExpanded/SetVisible/SetParent/AddChild/RemoveChild`, `Restore(id,name,type,parentId,layerType)` |
| **Tree Index** | `Index/TreeIndex.cs` | `Build(roots)`, `GetNodeById(id)`, `RevealNode(id)`, `BuildFlatList()`, `FilterNodes(query)` |
| **Interaction Logic** | `Logic/TreeInteractionLogic.cs` | `ToggleExpand`, `ToggleVisibility`, `SetSelection/GetSelection`, `OnFlatListInvalidated` event |
| **Drag/Drop Validator** | `Logic/DragDropValidator.cs` | `IsValidDrop`, `ExecuteDrop`, `IsDescendant` |
| **UI Toolkit View** | `UI/TreeViewController.cs` | MonoBehaviour on UIDocument; owns ListView + search TextField |
| **Data Generator** | `Editor/TreeDataGenerator.cs` | `Generate(2500, 6)` — editor-only, produces `List<TreeNode>` via `Restore` with pre-generated Guids |

## Key Technical Decisions

**Flat list type**: `List<(TreeNode node, int depth)>` — depth drives indent spacer width (`depth * 16px`). Rebuilt on every expand/collapse/filter.

**Virtualization**: `ListView` with `CollectionVirtualizationMethod.FixedHeight`, `fixedItemHeight = 22`. Elements reused via `makeItem`/`bindItem`. Always unregister old callbacks before rebinding (ListView reuses VisualElements).

**Drag/drop**: Pointer-event based (`PointerDownEvent`, `PointerMoveEvent`, `PointerUpEvent`) — runtime-safe, no `UnityEditor.DragAndDrop`. `IndexAtY` converts pointer Y to flat list index via `FloorToInt(y / fixedItemHeight)`. Validate in move, commit in up. Reject: drop onto self, onto descendant, adjacent sibling (no-op reorder).

**Search**: 300ms debounce via `Update()` timer. `FilterNodes` collects matching IDs + all ancestors, then DFS through tree skipping non-included nodes. Non-destructive — does not mutate model.

**Visibility toggle semantics**: Items toggle only themselves. Groups toggle all descendants recursively via `SetVisibilityRecursive`. Independent of `IsExpanded`.

**Selection persistence**: Stored as `NodeId` string in `TreeInteractionLogic`. After flat list rebuild, `ResolveSelectionIndex()` re-resolves by linear scan and calls `SetSelectionWithoutNotify`.

**Keyboard navigation**: Up/Down arrows move selection, Enter/Space toggle expand on selected node.

**TreeNode encapsulation**: Children exposed as `IReadOnlyList<TreeNode>` — mutations go through `AddChild(node, index)` / `RemoveChild(node)` only.

**TreeNode construction**: No public constructor. Only `TreeNode.Restore(id, name, type, parentId, layerType)` creates instances. Guids are generated once at editor time by `TreeDataGenerator`, baked into `TreeData.asset`, and loaded at runtime via `Restore`. Runtime never calls `Guid.NewGuid()`. Tests use a `NodeFactory.Make` helper that calls `Restore` with a fresh Guid.

## Packages

- `com.unity.modules.uielements` — UI Toolkit (ListView, VisualElement, USS)
- `com.unity.test-framework 1.6.0` — NUnit-based; use for data model and index logic unit tests
- `com.unity.inputsystem 1.19.0` — available for keyboard navigation bonus

## Coding Conventions

C# only. Place runtime code under `Assets/Scripts/`, editor-only code under `Assets/Scripts/Editor/`. USS stylesheets alongside their UXML files. No third-party packages.
