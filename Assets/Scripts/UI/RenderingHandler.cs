using System.Collections.Generic;
using Logic;
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

        private List<(TreeNode node, int depth, VisibilityState visState)> _flatList = new();

        private float _searchDebounceTimer;
        private const float SearchDebounceSeconds = 0.3f;
        private string _pendingQuery;
        private string _activeQuery;
        private bool _searchDirty;

        private string _draggedNodeId;
        private int _dragTargetIndex = -1;

        public void Initialize(InteractionsHandler interactions)
        {
            _doc = GetComponent<UIDocument>();
            _interactions = interactions;
        }

        private void OnEnable()
        {
            if (_interactions == null) return;

            var root = _doc.rootVisualElement;
            _listView    = root.Q<ListView>("tree-list-view");
            _searchField = root.Q<TextField>("search-field");

            SetupListView();
            SetupSearch();
            SetupKeyboard();
            _interactions.OnFlatListInvalidated += RebuildFlatList;
            RebuildFlatList();
        }

        private void OnDisable()
        {
            if (_interactions != null)
                _interactions.OnFlatListInvalidated -= RebuildFlatList;
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
                if (evt.button != 0) return;
                if (row.userData is RowData data)
                    _draggedNodeId = data.NodeId;
            });

            return row;
        }

        private void BindRow(VisualElement element, int index)
        {
            if (index < 0 || index >= _flatList.Count) return;
            var (node, depth, visState) = _flatList[index];

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

            if (toggle.userData is System.Action oldToggleCb) toggle.clicked -= oldToggleCb;
            System.Action newToggleCb = () => _interactions.ToggleExpand(node.NodeId);
            toggle.userData = newToggleCb;
            toggle.clicked += newToggleCb;

            label.text = node.DisplayName;
            if (node.IsGroup) label.AddToClassList("tree-label--group");
            else              label.RemoveFromClassList("tree-label--group");

            visBtn.text = visState == VisibilityState.Visible ? "●" : visState == VisibilityState.Mixed ? "◑" : "○";
            visBtn.EnableInClassList("tree-visibility-btn--visible", visState == VisibilityState.Visible);
            visBtn.EnableInClassList("tree-visibility-btn--mixed",   visState == VisibilityState.Mixed);
            visBtn.EnableInClassList("tree-visibility-btn--hidden",  visState == VisibilityState.Hidden);
            if (visBtn.userData is System.Action oldVisCb) visBtn.clicked -= oldVisCb;
            System.Action newVisCb = () => _interactions.ToggleVisibility(node.NodeId);
            visBtn.userData = newVisCb;
            visBtn.clicked += newVisCb;

            if (node.NodeId == _interactions.GetSelection())
                element.AddToClassList("tree-row--selected");
            else
                element.RemoveFromClassList("tree-row--selected");

            if (index == _dragTargetIndex) element.AddToClassList("tree-row--drag-target");
            else                           element.RemoveFromClassList("tree-row--drag-target");
        }

        // ── Flat list ──────────────────────────────────────────────────────────

        private void RebuildFlatList()
        {
            _flatList = string.IsNullOrWhiteSpace(_activeQuery)
                ? _interactions.BuildFlatList()
                : _interactions.FilterNodes(_activeQuery);
            _listView.itemsSource = _flatList;
            _listView.RefreshItems();
            ResolveSelectionIndex();
        }

        private void ApplySearch(string query)
        {
            _activeQuery = query;
            _flatList = string.IsNullOrWhiteSpace(query)
                ? _interactions.BuildFlatList()
                : _interactions.FilterNodes(query);
            _listView.itemsSource = _flatList;
            _listView.RefreshItems();
            ResolveSelectionIndex();
        }

        // ── Selection ──────────────────────────────────────────────────────────

        private void OnSelectionChanged(IEnumerable<object> _)
        {
            int idx = _listView.selectedIndex;
            if (idx < 0 || idx >= _flatList.Count) return;
            _interactions.SetSelection(_flatList[idx].node.NodeId);
            _listView.RefreshItems();
        }

        private void ResolveSelectionIndex()
        {
            var sel = _interactions.GetSelection();
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
                    _interactions.SetSelection(_flatList[next].node.NodeId);
                    _listView.RefreshItems();
                    evt.StopPropagation();
                    break;
                }
                case KeyCode.DownArrow:
                {
                    int next = Mathf.Min(_flatList.Count - 1, _listView.selectedIndex + 1);
                    _listView.selectedIndex = next;
                    _interactions.SetSelection(_flatList[next].node.NodeId);
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

            int hoveredIndex = IndexAtY(evt.localPosition.y);
            if (hoveredIndex < 0) return;

            string targetId = _flatList[hoveredIndex].node.NodeId;
            _dragTargetIndex = _interactions.IsValidDrop(_draggedNodeId, targetId) ? hoveredIndex : -1;
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
                    if (_interactions.IsValidDrop(_draggedNodeId, targetId))
                        _interactions.ExecuteDrop(_draggedNodeId, targetId, 0);
                }
            }

            _draggedNodeId   = null;
            _dragTargetIndex = -1;
            _listView.RefreshItems();
        }

        private void OnListPointerLeave(PointerLeaveEvent evt)
        {
            _draggedNodeId   = null;
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
