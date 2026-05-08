using System;
using NUnit.Framework;
using Oversight.Model;

namespace Oversight.Tests
{
    [TestFixture]
    public class TreeNodeTests
    {
        private static TreeNode Make(string name, NodeType type, string parentId = null,
                                     LayerType layerType = LayerType.None)
            => TreeNode.PopulateNewNode(Guid.NewGuid().ToString(), name, type, parentId, layerType);

        [Test]
        public void Restore_AssignsUniqueIds()
        {
            var a = Make("A", NodeType.Item);
            var b = Make("B", NodeType.Item);
            Assert.AreNotEqual(a.NodeId, b.NodeId);
        }

        [Test]
        public void Restore_NodeIdIsNotNullOrEmpty()
        {
            var node = Make("X", NodeType.Item);
            Assert.IsFalse(string.IsNullOrEmpty(node.NodeId));
        }

        [Test]
        public void Restore_DefaultsIsVisibleTrue()
        {
            var node = Make("X", NodeType.Item);
            Assert.IsTrue(node.IsVisible);
        }

        [Test]
        public void Restore_DefaultsIsExpandedFalse()
        {
            var node = Make("X", NodeType.Item);
            Assert.IsFalse(node.IsExpanded);
        }

        [Test]
        public void Restore_SetsParentId()
        {
            var node = Make("X", NodeType.Item, "parent-123");
            Assert.AreEqual("parent-123", node.ParentId);
        }

        [Test]
        public void Restore_NullParentIdByDefault()
        {
            var node = Make("X", NodeType.Item);
            Assert.IsNull(node.ParentId);
        }

        [Test]
        public void IsGroup_ReturnsTrueForGroupType()
        {
            var node = Make("G", NodeType.Group);
            Assert.IsTrue(node.IsGroup);
        }

        [Test]
        public void IsLeaf_ReturnsTrueForItemType()
        {
            var node = Make("I", NodeType.Item);
            Assert.IsTrue(node.IsLeaf);
        }

        [Test]
        public void IsGroup_ReturnsFalseForItemType()
        {
            var node = Make("I", NodeType.Item);
            Assert.IsFalse(node.IsGroup);
        }

        [Test]
        public void Children_InitializedEmpty()
        {
            var node = Make("X", NodeType.Group);
            Assert.IsNotNull(node.Children);
            Assert.AreEqual(0, node.Children.Count);
        }

        [Test]
        public void Restore_SetsDisplayName()
        {
            var node = Make("MyNode", NodeType.Item);
            Assert.AreEqual("MyNode", node.DisplayName);
        }
    }
}
