# Plan: Custom Tree View — Unity UI Toolkit

## Context
Build a custom high-performance Tree View for Unity 6000.4.5f1 LTS using UI Toolkit only (no built-in TreeView). Target: ~2,500 nodes, 4–6 hierarchy levels. Project is at ground zero — no scripts exist yet. Must deliver running project + Windows build + unit tests within the 5–8 hour budget.

---

## Files to Create

```
Assets/Scripts/
  Model/
    NodeType.cs
    TreeNode.cs
    TreeNode.asmdef          (noEngineReferences: true)
  Index/
    TreeIndex.cs
    TreeIndex.asmdef          (noEngineReferences: true, refs Model)
  Logic/
    TreeInteractionLogic.cs
    DragDropValidator.cs
    TreeLogic.asmdef          (refs Model + Index)
  UI/
    TreeViewController.cs
    TreeUI.asmdef             (refs Model + Index + Logic)
  TreeDataGenerator.cs        (under UI asmdef or default assembly)
  Tests/
    TreeNodeTests.cs
    TreeIndexTests.cs
    TreeTests.asmdef          (Editor-only, refs Model + Index + Logic)
Assets/UI/
  TreeView.uxml
  TreeView.uss
```

---

## Build Order

### Step 1 — Data Model
**`Assets/Scripts/Model/NodeType.cs`**
```csharp
public enum NodeType { Item, Group }
```

**`Assets/Scripts/Model/TreeNode.cs`**
```csharp
[System.Serializable]
public class TreeNode {
    public string NodeId;       // GUID, set in constructor
    public string ParentId;     // null = root
    public string DisplayName;
    public NodeType NodeType;
    public bool IsExpanded;     // default false
    public bool IsVisible;      // default true
    public List<TreeNode> Children;

    public TreeNode(string displayName, NodeType type, string parentId = null)
    public bool IsGroup => NodeType == NodeType.Group;
}
```

**`TreeNode.asmdef`**: `noEngineReferences: true`, `autoReferenced: true`

---

### Step 2 — Tree Index
**`Assets/Scripts/Index/TreeIndex.cs`**
```csharp
public class TreeIndex {
    private Dictionary<string, TreeNode> _idMap;
    private List<TreeNode> _roots;

    public void Build(List<TreeNode> roots)           // DFS-register all nodes
    public TreeNode GetNodeById(string id)            // O(1) lookup
    public void RevealNode(string id)                 // walk ParentId chain, set IsExpanded=true
    public List<(TreeNode node, int depth)> BuildFlatList()   // DFS, respects IsExpanded+IsVisible
    public List<(TreeNode node, int depth)> FilterNodes(string query) // non-destructive; includes ancestors of matches
}
```

**Depth**: computed during traversal (counter passed down), not stored on `TreeNode`.  
**FilterNodes depth**: walk `ParentId` chain for each included node; cache in local `Dictionary<string, int>`.

**`TreeIndex.asmdef`**: `noEngineReferences: true`, refs `OversightTreeModel`

---

### Step 3 — Interaction Logic + Drag/Drop Validator
**`Assets/Scripts/Logic/TreeInteractionLogic.cs`**
```csharp
public class TreeInteractionLogic {
    public event Action OnFlatListInvalidated;

    public void ToggleExpand(string nodeId)       // flip IsExpanded, raise event
    public void ToggleVisibility(string nodeId)   // Item: self only; Group: all descendants recursively
    public void SetSelection(string nodeId)       // store by NodeId
    public string GetSelection()
}
```

**`Assets/Scripts/Logic/DragDropValidator.cs`**
```csharp
public class DragDropValidator {
    public bool IsValidDrop(string draggedId, string targetId)
    // Reject: same node, target is descendant of dragged, same parent+position
    
    public void ExecuteDrop(string draggedId, string targetId, int insertIndex)
    // Detach from old parent → set ParentId → insert at index → _index.Build()
}
```

**`TreeLogic.asmdef`**: refs `OversightTreeModel`, `OversightTreeIndex`

---

### Step 4 — Sample Data Generator
**`Assets/Scripts/TreeDataGenerator.cs`**
```csharp
public static class TreeDataGenerator {
    public static List<TreeNode> Generate(int targetCount = 2500, int maxDepth = 6)
    // ~10 top-level Groups; recursively fill until targetCount reached
    // DisplayName: "Group_{depth}_{i}" / "Item_{depth}_{i}"
}
```

---

### Step 5 — UXML + USS
**`Assets/UI/TreeView.uxml`**
```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
  <ui:VisualElement name="tree-root" class="tree-root">
    <ui:TextField name="search-field" placeholder-text="Search..." />
    <ui:ListView name="tree-list-view" />
  </ui:VisualElement>
</ui:UXML>
```
ListView configured fully in C# (makeItem/bindItem), not in UXML.

**`Assets/UI/TreeView.uss`** key rules:
- `.tree-row`: `flex-direction: row; height: 22px`
- `.tree-indent-spacer`: width set inline per depth (`depth * 16px`)
- `.tree-toggle`: 16×16, hidden for Item nodes
- `.tree-row--selected`: highlight color
- `.tree-row--hidden`: `opacity: 0.4`

Create scene: `File > New Scene`, add `UIDocument` GameObject, create `PanelSettings` asset, assign UXML.

---

### Step 6 — TreeViewController
**`Assets/Scripts/UI/TreeViewController.cs`**
```csharp
[RequireComponent(typeof(UIDocument))]
public class TreeViewController : MonoBehaviour {
    // Awake: generate roots, Build index, wire OnFlatListInvalidated → RebuildFlatList
    // OnEnable: query UI, SetupListView(), SetupSearch(), SetupKeyboard(), RebuildFlatList()
    // Update: debounce timer (300ms) → ApplySearch()

    private void SetupListView()
    // fixedItemHeight=22, FixedHeight virtualization, makeItem=MakeRow, bindItem=BindRow
    // selectionType=Single, selectionChanged → OnSelectionChanged

    private VisualElement MakeRow()
    // Returns: container(tree-row) > [indent-spacer, toggle-btn, label, visibility-btn]

    private void BindRow(VisualElement el, int index)
    // CRITICAL: unregister old callbacks before registering new ones (ListView reuses elements)
    // Set indent width, toggle text (▶/▼), label.text, visibility icon
    // Apply/remove CSS classes: tree-row--selected, tree-row--hidden

    private void RebuildFlatList()    // _index.BuildFlatList() → set itemsSource → RefreshItems() → ResolveSelectionIndex()
    private void ApplySearch(string query)  // BuildFlatList or FilterNodes → set itemsSource → RefreshItems() → ResolveSelectionIndex()
    private void ResolveSelectionIndex()    // FindIndex by NodeId → SetSelectionWithoutNotify
}
```

**Drag/drop**: register `DragUpdatedEvent` + `DragPerformEvent` on `_listView` (not rows). Initiate from `PointerMoveEvent` with button check. Compute hover index from `event.localPosition.y / fixedItemHeight`.

**`TreeUI.asmdef`**: refs Model, Index, Logic

---

### Step 7 — Unit Tests

**`TreeTests.asmdef`**: `includePlatforms: ["Editor"]`, refs Model + Index + Logic, `optionalUnityReferences: ["TestAssemblies"]`

**`TreeNodeTests.cs`**: constructor assigns unique IDs, defaults (IsVisible=true, IsExpanded=false), IsGroup/IsLeaf, Children initialized empty.

**`TreeIndexTests.cs`**:
- `GetNodeById` — hit and miss
- `BuildFlatList` — collapsed root returns only root; expand root returns children; DFS order; invisible node excluded
- `RevealNode` — expands ancestor chain
- `FilterNodes` — returns match + ancestors; case-insensitive; empty query = full list; no match = empty
- `DragDropValidator` — rejects descendant target, self-drop, same position; accepts valid reparent; `ExecuteDrop` updates ParentId, removes from old parent, rebuilds index

Run: Unity Editor → Window → General → Test Runner → EditMode → Run All

---

### Step 8 — Windows Build
File > Build Settings > Windows > Build. Confirm scene is in build list.

---

## Key Gotchas

| Risk | Mitigation |
|---|---|
| ListView reuses elements — callbacks accumulate | Unregister in `BindRow` before registering. Store callback ref in `element.userData` |
| `List<(TreeNode,int)>` not assignable to `IList` | Explicit cast: `_listView.itemsSource = (IList)_flatList` |
| `[Serializable]` — must use `System.Serializable`, not `UnityEngine` | Enforced by `noEngineReferences: true` on Model/Index asmdefs |
| DragAndDrop in UI Toolkit differs from IMGUI | Register events on ListView, not rows. Use PointerMoveEvent for drag start |
| Filter depth recomputation | Walk ParentId chain per included node; cache locally |

---

## Verification

1. Unity imports all scripts with zero errors
2. Test Runner: all EditMode tests green
3. Runtime: 2,500 nodes load, expand/collapse smooth, search debounces correctly, drag/drop validates and commits, visibility toggle works per semantics
4. Windows build runs standalone
