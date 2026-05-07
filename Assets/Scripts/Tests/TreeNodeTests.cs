using NUnit.Framework;
using System.Collections.Generic;
using Oversight.Model;

namespace Oversight.Tests
{
    [TestFixture]
    public class TreeNodeTests
    {
        [Test]
        public void Constructor_AssignsUniqueIds()
        {
            var a = new TreeNode("A", NodeType.Item);
            var b = new TreeNode("B", NodeType.Item);
            Assert.AreNotEqual(a.NodeId, b.NodeId);
        }

        [Test]
        public void Constructor_NodeIdIsNotNullOrEmpty()
        {
            var node = new TreeNode("X", NodeType.Item);
            Assert.IsFalse(string.IsNullOrEmpty(node.NodeId));
        }

        [Test]
        public void Constructor_DefaultsIsVisibleTrue()
        {
            var node = new TreeNode("X", NodeType.Item);
            Assert.IsTrue(node.IsVisible);
        }

        [Test]
        public void Constructor_DefaultsIsExpandedFalse()
        {
            var node = new TreeNode("X", NodeType.Item);
            Assert.IsFalse(node.IsExpanded);
        }

        [Test]
        public void Constructor_SetsParentId()
        {
            var node = new TreeNode("X", NodeType.Item, "parent-123");
            Assert.AreEqual("parent-123", node.ParentId);
        }

        [Test]
        public void Constructor_NullParentIdByDefault()
        {
            var node = new TreeNode("X", NodeType.Item);
            Assert.IsNull(node.ParentId);
        }

        [Test]
        public void IsGroup_ReturnsTrueForGroupType()
        {
            var node = new TreeNode("G", NodeType.Group);
            Assert.IsTrue(node.IsGroup);
        }

        [Test]
        public void IsLeaf_ReturnsTrueForItemType()
        {
            var node = new TreeNode("I", NodeType.Item);
            Assert.IsTrue(node.IsLeaf);
        }

        [Test]
        public void IsGroup_ReturnsFalseForItemType()
        {
            var node = new TreeNode("I", NodeType.Item);
            Assert.IsFalse(node.IsGroup);
        }

        [Test]
        public void Children_InitializedEmpty()
        {
            var node = new TreeNode("X", NodeType.Group);
            Assert.IsNotNull(node.Children);
            Assert.AreEqual(0, node.Children.Count);
        }

        [Test]
        public void Constructor_SetsDisplayName()
        {
            var node = new TreeNode("MyNode", NodeType.Item);
            Assert.AreEqual("MyNode", node.DisplayName);
        }
    }
}
