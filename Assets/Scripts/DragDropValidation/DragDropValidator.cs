using System;
using Model;

namespace DragDropValidation
{
    public class DragDropValidator
    {
        private readonly Func<string, TreeNode> _getNode;

        // newParentId is the group the dragged node would be inserted into (null = root level)
        public DragDropValidator(Func<string, TreeNode> getNode)
        {
            _getNode = getNode;
        }

        public bool IsValidDrop(string draggedId, string newParentId)
        {
            if (newParentId == draggedId) return false; // Can't drop into self
            if (newParentId != null && IsDescendant(draggedId, newParentId))
                return false; // Can't drop into own subtree

            if (newParentId != null)
            {
                // Parent must be a group — items cannot contain children
                var parent = _getNode(newParentId);
                if (parent is { IsGroup: false }) return false; // pattern for hidden null check
            }
            else
            {
                // Root level only accepts groups — items must live inside a group
                var dragged = _getNode(draggedId);
                if (dragged is { IsGroup: false }) return false; // pattern for hidden null check
            }

            return true;
        }

        private bool IsDescendant(string ancestorId, string candidateId)
        {
            var current = _getNode(candidateId);
            if (current == null) return false;

            while (current.ParentId != null)
            {
                current = _getNode(current.ParentId);
                if (current == null) break;
                if (current.NodeId == ancestorId) return true;
            }

            return false;
        }
    }
}