using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Oversight.Model;
using Oversight.Index;
using Oversight.Logic;

namespace Oversight.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class TreeViewController : MonoBehaviour
    {
        private UIDocument _doc;
        private ListView _listView;
        private TextField _searchField;

        private List<TreeNode> _roots;
        private TreeIndex _index;
        private TreeInteractionLogic _logic;
        private DragDropValidator _validator;

        private List<(TreeNode node, int depth)> _flatList = new();

        private float _searchDebounceTimer;
        private const float SearchDebounceSeconds = 0.3f;
        private string _pendingQuery;
        private bool _searchDirty;

        private string _draggedNodeId;
        private int _dragTargetIndex = -1;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _roots = TreeDataGenerator.Generate(2500, 6);

            _index = new TreeIndex();
            _index.Build(_roots);

            _logic = new TreeInteractionLogic(_index);
            _validator = new DragDropValidator(_index, _roots);

            _logic.OnFlatListInvalidated += RebuildFlatList;
        }

        private void OnEnable()
        {
            var root = _doc.rootVisualElement;
            _listView    = root.Q<ListView>("tree-list-view");
            _searchField = root.Q<TextField>("search-field");

            SetupListView();
            SetupSearch();
            SetupKeyboard();
            RebuildFlatList();
        }

        private void OnDisable()
        {
            _logic.OnFlatListInvalidated -= RebuildFlatList;
        }

        private void Update()
        {
            if (!_searchDirty) return;
            _searchDebounceTimer -= Time.deltaTime;
            if (_searchDebounceTimer <= 0f)
            {
                ApplySearch(_pendingQuery);
                _searchDirty = false;
            }
        }

        // ── ListView ───────────────────────────────────────────────────────────

        private void SetupListView()
        {
            _listView.fixedItemHeight = 22;
            _listView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            _listView.makeItem = MakeRow;
            _listView.bindItem = BindRow;
            _listView.selectionType = SelectionType.Single;
            _listView.selectionChanged += OnSelectionChanged;

            // Pointer-based drag/drop (runtime-safe, no UnityEditor.DragAndDrop)
            _listView.RegisterCallback<PointerMoveEvent>(OnListPointerMove, TrickleDown.TrickleDown);
            _listView.RegisterCallback<PointerUpEvent>(OnListPointerUp, TrickleDown.TrickleDown);
            _listView.RegisterCallback<PointerLeaveEvent>(OnListPointerLeave);
        }

        private VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("tree-row");

            var spacer = new VisualElement();
            spacer.AddToClassList("tree-indent-spacer");
            spacer.name = "indent";

            var toggle = new Button();
            toggle.AddToClassList("tree-toggle");
            toggle.name = "toggle";

            var label = new Label();
            label.AddToClassList("tree-label");
            label.name = "label";

            var visBtn = new Button();
            visBtn.AddToClassList("tree-visibility-btn");
            visBtn.name = "vis-btn";

            row.Add(spacer);
            row.Add(toggle);
            row.Add(label);
            row.Add(visBtn);

            // Record drag source on pointer down
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                if (row.userData is RowData data)
                    _draggedNodeId = data.NodeId;
            });

            return row;
        }

        private void BindRow(VisualElement element, int index)
        {
            if (index < 0 || index >= _flatList.Count) return;
            var (node, depth) = _flatList[index];

            var spacer = element.Q("indent");
            var toggle = element.Q<Button>("toggle");
            var label  = element.Q<Label>("label");
            var visBtn = element.Q<Button>("vis-btn");

            element.userData = new RowData { NodeId = node.NodeId };

            spacer.style.width = depth * 16;

            if (node.IsGroup)
            {
                toggle.RemoveFromClassList("tree-toggle--hidden");
                toggle.text = node.IsExpanded ? "▼" : "▶";
            }
            else
            {
                toggle.AddToClassList("tree-toggle--hidden");
            }

            // Unregister old callbacks before rebinding (ListView reuses elements)
            if (toggle.userData is System.Action oldToggleCb) toggle.clicked -= oldToggleCb;
            System.Action newToggleCb = () => _logic.ToggleExpand(node.NodeId);
            toggle.userData = newToggleCb;
            toggle.clicked += newToggleCb;

            label.text = node.DisplayName;
            if (node.IsGroup) label.AddToClassList("tree-label--group");
            else              label.RemoveFromClassList("tree-label--group");

            visBtn.text = node.IsVisible ? "●" : "○";
            if (visBtn.userData is System.Action oldVisCb) visBtn.clicked -= oldVisCb;
            System.Action newVisCb = () => _logic.ToggleVisibility(node.NodeId);
            visBtn.userData = newVisCb;
            visBtn.clicked += newVisCb;

            if (node.NodeId == _logic.GetSelection())
                element.AddToClassList("tree-row--selected");
            else
                element.RemoveFromClassList("tree-row--selected");

            if (!node.IsVisible) element.AddToClassList("tree-row--hidden");
            else                 element.RemoveFromClassList("tree-row--hidden");

            if (index == _dragTargetIndex) element.AddToClassList("tree-row--drag-target");
            else                           element.RemoveFromClassList("tree-row--drag-target");
        }

        // ── Flat list ──────────────────────────────────────────────────────────

        private void RebuildFlatList()
        {
            _flatList = _index.BuildFlatList();
            _listView.itemsSource = _flatList;
            _listView.RefreshItems();
            ResolveSelectionIndex();
        }

        private void ApplySearch(string query)
        {
            _flatList = string.IsNullOrWhiteSpace(query)
                ? _index.BuildFlatList()
                : _index.FilterNodes(query);

            _listView.itemsSource = _flatList;
            _listView.RefreshItems();
            ResolveSelectionIndex();
        }

        // ── Selection ──────────────────────────────────────────────────────────

        private void OnSelectionChanged(IEnumerable<object> _)
        {
            int idx = _listView.selectedIndex;
            if (idx < 0 || idx >= _flatList.Count) return;
            _logic.SetSelection(_flatList[idx].node.NodeId);
            _listView.RefreshItems();
        }

        private void ResolveSelectionIndex()
        {
            var sel = _logic.GetSelection();
            if (sel == null) return;
            int idx = _flatList.FindIndex(t => t.node.NodeId == sel);
            if (idx >= 0)
                _listView.SetSelectionWithoutNotify(new[] { idx });
        }

        // ── Search ─────────────────────────────────────────────────────────────

        private void SetupSearch()
        {
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _pendingQuery = evt.newValue;
                _searchDebounceTimer = SearchDebounceSeconds;
                _searchDirty = true;
            });
        }

        // ── Keyboard ───────────────────────────────────────────────────────────

        private void SetupKeyboard()
        {
            _listView.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                {
                    int next = Mathf.Max(0, _listView.selectedIndex - 1);
                    _listView.selectedIndex = next;
                    _logic.SetSelection(_flatList[next].node.NodeId);
                    _listView.RefreshItems();
                    evt.StopPropagation();
                    break;
                }
                case KeyCode.DownArrow:
                {
                    int next = Mathf.Min(_flatList.Count - 1, _listView.selectedIndex + 1);
                    _listView.selectedIndex = next;
                    _logic.SetSelection(_flatList[next].node.NodeId);
                    _listView.RefreshItems();
                    evt.StopPropagation();
                    break;
                }
                case KeyCode.Return:
                case KeyCode.Space:
                {
                    var sel = _logic.GetSelection();
                    if (sel != null) _logic.ToggleExpand(sel);
                    evt.StopPropagation();
                    break;
                }
            }
        }

        // ── Drag/Drop (pointer-event based, runtime-safe) ──────────────────────

        private void OnListPointerMove(PointerMoveEvent evt)
        {
            if (evt.pressedButtons != 1 || string.IsNullOrEmpty(_draggedNodeId)) return;

            int hoveredIndex = IndexAtY(evt.localPosition.y);
            if (hoveredIndex < 0) return;

            string targetId = _flatList[hoveredIndex].node.NodeId;
            _dragTargetIndex = _validator.IsValidDrop(_draggedNodeId, targetId) ? hoveredIndex : -1;
            _listView.RefreshItems();
        }

        private void OnListPointerUp(PointerUpEvent evt)
        {
            if (!string.IsNullOrEmpty(_draggedNodeId))
            {
                int hoveredIndex = IndexAtY(evt.localPosition.y);
                if (hoveredIndex >= 0)
                {
                    string targetId = _flatList[hoveredIndex].node.NodeId;
                    if (_validator.IsValidDrop(_draggedNodeId, targetId))
                    {
                        _validator.ExecuteDrop(_draggedNodeId, targetId, 0);
                        RebuildFlatList();
                    }
                }
            }

            _draggedNodeId  = null;
            _dragTargetIndex = -1;
            _listView.RefreshItems();
        }

        private void OnListPointerLeave(PointerLeaveEvent evt)
        {
            _draggedNodeId  = null;
            _dragTargetIndex = -1;
            _listView.RefreshItems();
        }

        private int IndexAtY(float y)
        {
            if (_flatList.Count == 0) return -1;
            int idx = Mathf.FloorToInt(y / _listView.fixedItemHeight);
            return Mathf.Clamp(idx, 0, _flatList.Count - 1);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private class RowData
        {
            public string NodeId;
        }
    }
}
