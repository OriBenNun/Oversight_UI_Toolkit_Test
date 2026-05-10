using System.Collections.Generic;
using DragDropValidation;
using Index;
using Interaction;
using Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [RequireComponent(typeof(UIDocument))]
    public class RenderingHandler : MonoBehaviour
    {
        private UIDocument _doc;
        private ListView _listView;
        private TextField _searchField;
        private InteractionsHandler _interactions;
        private IndexHandler _indexHandler;
        private DragDropValidator _validator;

        private float _searchDebounceTimer;
        private const float SearchDebounceSeconds = 0.3f;
        private string _pendingQuery;
        private bool _isSearchActive;
        private bool _searchDirty;

        private string _draggedNodeId;
        private int _dragTargetIndex = -1;
        private bool _insertBefore = true;

        public void Initialize(InteractionsHandler interactions, IndexHandler index, DragDropValidator validator)
        {
            _doc = GetComponent<UIDocument>();
            _interactions = interactions;
            _indexHandler = index;
            _validator = validator;
        }

        private void OnEnable()
        {
            var root = _doc.rootVisualElement;
            _listView = root.Q<ListView>("tree-list-view");
            _searchField = root.Q<TextField>("search-field");

            SetupListView();
            SetupSearch();
            SetupKeyboard();
            _indexHandler.OnFlatListInvalidated += RebuildFlatList;
            _indexHandler.OnRevealNode += ScrollToNode;
            RebuildFlatList();
        }

        private void OnDisable()
        {
            _indexHandler.OnFlatListInvalidated -= RebuildFlatList;
            _indexHandler.OnRevealNode -= ScrollToNode;
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
            _listView.fixedItemHeight = 48;
            _listView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            _listView.makeItem = MakeRow;
            _listView.bindItem = BindRow;
            _listView.selectionType = SelectionType.Single;
            _listView.selectionChanged += OnSelectionChanged;

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

            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return; // Only left mouse button
                if (row.userData is RowData data)
                    _draggedNodeId = data.NodeId;
            });

            return row;
        }

        private void BindRow(VisualElement element, int index)
        {
            if (index < 0 || index >= _indexHandler.FlatList.Count) return;
            var (node, depth, visState) = _indexHandler.FlatList[index];

            var spacer = element.Q("indent");
            var toggle = element.Q<Button>("toggle");
            var label = element.Q<Label>("label");
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

            if (toggle.userData is System.Action oldToggleCb) toggle.clicked -= oldToggleCb;
            System.Action newToggleCb = () => _interactions.ToggleExpand(node.NodeId);
            toggle.userData = newToggleCb;
            toggle.clicked += newToggleCb;

            label.text = node.DisplayName;
            if (node.IsGroup) label.AddToClassList("tree-label--group");
            else label.RemoveFromClassList("tree-label--group");

            visBtn.text = visState == VisibilityState.Visible ? "●" : visState == VisibilityState.Mixed ? "◑" : "○";
            visBtn.EnableInClassList("tree-visibility-btn--visible", visState == VisibilityState.Visible);
            visBtn.EnableInClassList("tree-visibility-btn--mixed", visState == VisibilityState.Mixed);
            visBtn.EnableInClassList("tree-visibility-btn--hidden", visState == VisibilityState.Hidden);
            if (visBtn.userData is System.Action oldVisCb) visBtn.clicked -= oldVisCb;
            System.Action newVisCb = () => _interactions.ToggleVisibility(node.NodeId);
            visBtn.userData = newVisCb;
            visBtn.clicked += newVisCb;

            if (node.NodeId == _interactions.GetSelection())
                element.AddToClassList("tree-row--selected");
            else
                element.RemoveFromClassList("tree-row--selected");

            element.EnableInClassList("tree-row--drop-before", index == _dragTargetIndex && _insertBefore);
            element.EnableInClassList("tree-row--drop-after", index == _dragTargetIndex && !_insertBefore);
        }

        // ── Flat list ──────────────────────────────────────────────────────────

        private void RebuildFlatList()
        {
            _listView.itemsSource = _indexHandler.FlatList;
            _listView.RefreshItems();
            ResolveSelectionIndex();
        }

        private void ApplySearch(string query)
        {
            var wasSearchActive = _isSearchActive;
            _isSearchActive = !string.IsNullOrWhiteSpace(query);
            _indexHandler.SetFilter(query);

            if (wasSearchActive && !_isSearchActive)
            {
                var sel = _interactions.GetSelection();
                if (sel != null) _indexHandler.RevealNode(sel);
            }
        }

        // ── Selection ──────────────────────────────────────────────────────────

        private void OnSelectionChanged(IEnumerable<object> _)
        {
            var idx = _listView.selectedIndex;
            if (idx < 0 || idx >= _indexHandler.FlatList.Count) return;
            _interactions.SetSelection(_indexHandler.FlatList[idx].node.NodeId);
            _listView.RefreshItems();
        }

        private void ResolveSelectionIndex()
        {
            var sel = _interactions.GetSelection();
            if (sel == null) return;
            var idx = _indexHandler.FlatList.FindIndex(t => t.node.NodeId == sel);
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
                    var next = Mathf.Max(0, _listView.selectedIndex - 1);
                    _listView.selectedIndex = next;
                    _interactions.SetSelection(_indexHandler.FlatList[next].node.NodeId);
                    _listView.RefreshItems();
                    evt.StopPropagation();
                    break;
                }
                case KeyCode.DownArrow:
                {
                    var next = Mathf.Min(_indexHandler.FlatList.Count - 1, _listView.selectedIndex + 1);
                    _listView.selectedIndex = next;
                    _interactions.SetSelection(_indexHandler.FlatList[next].node.NodeId);
                    _listView.RefreshItems();
                    evt.StopPropagation();
                    break;
                }
                case KeyCode.Return:
                case KeyCode.Space:
                {
                    var sel = _interactions.GetSelection();
                    if (sel != null) _interactions.ToggleExpand(sel);
                    evt.StopPropagation();
                    break;
                }
            }
        }

        // ── Drag/Drop (pointer-event based, runtime-safe) ──────────────────────

        private void OnListPointerMove(PointerMoveEvent evt)
        {
            if (evt.pressedButtons != 1 || string.IsNullOrEmpty(_draggedNodeId)) return;

            var hoveredIndex = IndexAtY(evt.localPosition.y);
            if (hoveredIndex < 0)
            {
                ClearDropTarget();
                _listView.RefreshItems();
                return;
            }

            var hoveredNode = _indexHandler.FlatList[hoveredIndex].node;
            if (hoveredNode.NodeId == _draggedNodeId)
            {
                ClearDropTarget();
                _listView.RefreshItems();
                return;
            }

            var yInRow = evt.localPosition.y - hoveredIndex * _listView.fixedItemHeight;
            var before = yInRow < _listView.fixedItemHeight * 0.5f;
            var parentId = (!before && hoveredNode.IsGroup && hoveredNode.IsExpanded)
                ? hoveredNode.NodeId
                : hoveredNode.ParentId;

            if (_validator.IsValidDrop(_draggedNodeId, parentId))
            {
                _dragTargetIndex = hoveredIndex;
                _insertBefore = before;
            }
            else
                ClearDropTarget();

            _listView.RefreshItems();
        }

        private void OnListPointerUp(PointerUpEvent evt)
        {
            if (!string.IsNullOrEmpty(_draggedNodeId))
            {
                var hoveredIndex = IndexAtY(evt.localPosition.y);
                if (hoveredIndex >= 0)
                {
                    var hoveredNode = _indexHandler.FlatList[hoveredIndex].node;
                    var yInRow = evt.localPosition.y - hoveredIndex * _listView.fixedItemHeight;
                    var insertBefore = yInRow < _listView.fixedItemHeight * 0.5f;

                    if (_interactions.ExecuteDrop(_draggedNodeId, hoveredNode.NodeId, insertBefore))
                        _indexHandler.RevealNode(_draggedNodeId);
                }
            }

            _draggedNodeId = null;
            ClearDropTarget();
            _listView.RefreshItems();
        }

        private void OnListPointerLeave(PointerLeaveEvent evt)
        {
            _draggedNodeId = null;
            ClearDropTarget();
            _listView.RefreshItems();
        }

        private void ClearDropTarget()
        {
            _dragTargetIndex = -1;
            _insertBefore = true;
        }

        private void ScrollToNode(string id)
        {
            var idx = _indexHandler.FlatList.FindIndex(t => t.node.NodeId == id);
            if (idx >= 0)
                _listView.ScrollToItem(idx);
        }

        private int IndexAtY(float y)
        {
            if (_indexHandler.FlatList.Count == 0) return -1;
            var idx = Mathf.FloorToInt(y / _listView.fixedItemHeight);
            return Mathf.Clamp(idx, 0, _indexHandler.FlatList.Count - 1);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        
        // Comment by Claude:
        // RowData stores NodeId on each reused VisualElement via userData. Without it, drag start (line 116) can't know which
        // node a row represents.
        // userData is typed object, so a named class is cleaner than boxing a raw string — but you could replace it with just
        // element.userData = node.NodeId and cast (string)row.userData on read. The class adds nothing functionally here; it's
        // just a named wrapper.
        private class RowData
        {
            public string NodeId;
        }
    }
}