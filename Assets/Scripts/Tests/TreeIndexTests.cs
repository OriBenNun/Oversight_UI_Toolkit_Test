using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Oversight.Model;
using Oversight.Index;
using Oversight.Logic;

namespace Oversight.Tests
{
    internal static class NodeFactory
    {
        internal static TreeNode Make(string name, NodeType type, string parentId = null,
                                      LayerType layerType = LayerType.None)
            => TreeNode.PopulateNewNode(Guid.NewGuid().ToString(), name, type, parentId, layerType);
    }

    [TestFixture]
    public class TreeIndexTests
    {
        private TreeIndex _index;
        private List<TreeNode> _roots;
        private TreeNode _root;
        private TreeNode _childA;
        private TreeNode _childB;
        private TreeNode _grandchildA1;

        [SetUp]
        public void Setup()
        {
            _root = NodeFactory.Make("Root", NodeType.Group);

            _childA = NodeFactory.Make("ChildA", NodeType.Group, _root.NodeId);
            _root.AddChild(_childA, _root.Children.Count);

            _grandchildA1 = NodeFactory.Make("GrandchildA1", NodeType.Item, _childA.NodeId);
            _childA.AddChild(_grandchildA1, _childA.Children.Count);

            _childB = NodeFactory.Make("ChildB", NodeType.Item, _root.NodeId);
            _root.AddChild(_childB, _root.Children.Count);

            _roots = new List<TreeNode> { _root };
            _index = new TreeIndex();
            _index.Build(_roots);
        }

        // ── GetNodeById ───────────────────────────────────────────────────────

        [Test]
        public void GetNodeById_ReturnsCorrectNode()
        {
            var result = _index.GetNodeById(_childA.NodeId);
            Assert.AreEqual(_childA, result);
        }

        [Test]
        public void GetNodeById_ReturnsNullForUnknownId()
        {
            var result = _index.GetNodeById("non-existent-id");
            Assert.IsNull(result);
        }

        // ── BuildFlatList ─────────────────────────────────────────────────────

        [Test]
        public void BuildFlatList_RootNotExpanded_ReturnsOnlyRoot()
        {
            var flat = _index.BuildFlatList();
            Assert.AreEqual(1, flat.Count);
            Assert.AreEqual(_root, flat[0].node);
        }

        [Test]
        public void BuildFlatList_ExpandRoot_ReturnsRootAndDirectChildren()
        {
            _root.SetExpanded(true);
            var flat = _index.BuildFlatList();
            Assert.AreEqual(3, flat.Count); // root, childA, childB
            Assert.AreEqual(_root,   flat[0].node);
            Assert.AreEqual(_childA, flat[1].node);
            Assert.AreEqual(_childB, flat[2].node);
        }

        [Test]
        public void BuildFlatList_ExpandAll_ReturnsDFSOrder()
        {
            _root.SetExpanded(true);
            _childA.SetExpanded(true);
            var flat = _index.BuildFlatList();
            Assert.AreEqual(4, flat.Count);
            Assert.AreEqual(_root,         flat[0].node);
            Assert.AreEqual(_childA,       flat[1].node);
            Assert.AreEqual(_grandchildA1, flat[2].node);
            Assert.AreEqual(_childB,       flat[3].node);
        }

        [Test]
        public void BuildFlatList_DepthIsCorrect()
        {
            _root.SetExpanded(true);
            _childA.SetExpanded(true);
            var flat = _index.BuildFlatList();
            Assert.AreEqual(0, flat[0].depth); // root
            Assert.AreEqual(1, flat[1].depth); // childA
            Assert.AreEqual(2, flat[2].depth); // grandchildA1
            Assert.AreEqual(1, flat[3].depth); // childB
        }

        [Test]
        public void BuildFlatList_InvisibleNode_Excluded()
        {
            _root.SetExpanded(true);
            _childA.SetVisible(false);
            var flat = _index.BuildFlatList();
            Assert.AreEqual(2, flat.Count); // root + childB only
            Assert.IsFalse(flat.Exists(t => t.node == _childA));
        }

        // ── RevealNode ────────────────────────────────────────────────────────

        [Test]
        public void RevealNode_ExpandsAncestors()
        {
            Assert.IsFalse(_root.IsExpanded);
            Assert.IsFalse(_childA.IsExpanded);

            _index.RevealNode(_grandchildA1.NodeId);

            Assert.IsTrue(_root.IsExpanded);
            Assert.IsTrue(_childA.IsExpanded);
        }

        [Test]
        public void RevealNode_DoesNotCollapseAlreadyExpanded()
        {
            _root.SetExpanded(true);
            _childA.SetExpanded(true);
            _index.RevealNode(_grandchildA1.NodeId);
            Assert.IsTrue(_root.IsExpanded);
            Assert.IsTrue(_childA.IsExpanded);
        }

        // ── FilterNodes ───────────────────────────────────────────────────────

        [Test]
        public void FilterNodes_EmptyQuery_ReturnsFullFlatList()
        {
            _root.SetExpanded(true);
            _childA.SetExpanded(true);
            var full   = _index.BuildFlatList();
            var filter = _index.FilterNodes("");
            Assert.AreEqual(full.Count, filter.Count);
        }

        [Test]
        public void FilterNodes_ReturnsMatchAndAncestors()
        {
            var result = _index.FilterNodes("GrandchildA1");
            var ids = result.ConvertAll(t => t.node.NodeId);
            Assert.Contains(_grandchildA1.NodeId, ids);
            Assert.Contains(_childA.NodeId,       ids);
            Assert.Contains(_root.NodeId,         ids);
        }

        [Test]
        public void FilterNodes_NoMatch_ReturnsEmptyList()
        {
            var result = _index.FilterNodes("zzznomatch");
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void FilterNodes_IsCaseInsensitive()
        {
            var lower = _index.FilterNodes("grandchilda1");
            var upper = _index.FilterNodes("GRANDCHILDA1");
            Assert.AreEqual(lower.Count, upper.Count);
            Assert.Greater(lower.Count, 0);
        }
    }

    [TestFixture]
    public class DragDropValidatorTests
    {
        private TreeIndex _index;
        private List<TreeNode> _roots;
        private TreeNode _root;
        private TreeNode _childA;
        private TreeNode _childB;
        private TreeNode _grandchildA1;
        private DragDropValidator _validator;

        [SetUp]
        public void Setup()
        {
            _root = NodeFactory.Make("Root", NodeType.Group);

            _childA = NodeFactory.Make("ChildA", NodeType.Group, _root.NodeId);
            _root.AddChild(_childA, _root.Children.Count);

            _grandchildA1 = NodeFactory.Make("GrandchildA1", NodeType.Item, _childA.NodeId);
            _childA.AddChild(_grandchildA1, _childA.Children.Count);

            _childB = NodeFactory.Make("ChildB", NodeType.Item, _root.NodeId);
            _root.AddChild(_childB, _root.Children.Count);

            _roots = new List<TreeNode> { _root };
            _index = new TreeIndex();
            _index.Build(_roots);
            _validator = new DragDropValidator(_index, _roots);
        }

        [Test]
        public void IsValidDrop_RejectsSelfDrop()
        {
            Assert.IsFalse(_validator.IsValidDrop(_childA.NodeId, _childA.NodeId));
        }

        [Test]
        public void IsValidDrop_RejectsDescendantTarget()
        {
            Assert.IsFalse(_validator.IsValidDrop(_root.NodeId, _grandchildA1.NodeId));
        }

        [Test]
        public void IsValidDrop_AcceptsReparentToUncle()
        {
            Assert.IsTrue(_validator.IsValidDrop(_grandchildA1.NodeId, _childB.NodeId));
        }

        [Test]
        public void IsDescendant_ReturnsTrueForDescendant()
        {
            Assert.IsTrue(_validator.IsDescendant(_root.NodeId, _grandchildA1.NodeId));
        }

        [Test]
        public void IsDescendant_ReturnsFalseForNonDescendant()
        {
            Assert.IsFalse(_validator.IsDescendant(_childB.NodeId, _childA.NodeId));
        }

        [Test]
        public void ExecuteDrop_UpdatesParentId()
        {
            _validator.ExecuteDrop(_grandchildA1.NodeId, _childB.NodeId, 0);
            Assert.AreEqual(_childB.NodeId, _grandchildA1.ParentId);
        }

        [Test]
        public void ExecuteDrop_RemovedFromOldParentChildren()
        {
            _validator.ExecuteDrop(_grandchildA1.NodeId, _childB.NodeId, 0);
            Assert.IsFalse(_childA.Children.Contains(_grandchildA1));
        }

        [Test]
        public void ExecuteDrop_AddedToNewParentChildren()
        {
            _validator.ExecuteDrop(_grandchildA1.NodeId, _childB.NodeId, 0);
            Assert.IsTrue(_childB.Children.Contains(_grandchildA1));
        }

        [Test]
        public void ExecuteDrop_RebuildsIndex()
        {
            _validator.ExecuteDrop(_grandchildA1.NodeId, _childB.NodeId, 0);
            var found = _index.GetNodeById(_grandchildA1.NodeId);
            Assert.IsNotNull(found);
        }
    }
}
