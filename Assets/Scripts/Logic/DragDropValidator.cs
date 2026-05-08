using System;
using System.Collections.Generic;
using Oversight.Index;
using Oversight.Model;

namespace Oversight.Logic
{
    public class DragDropValidator
    {
        private readonly TreeIndex _index;
        private readonly List<TreeNode> _roots;

        public DragDropValidator(TreeIndex index, List<TreeNode> roots)
        {
            _index = index;
            _roots = roots;
        }

        public bool IsValidDrop(string draggedId, string targetId)
        {
            if (draggedId == targetId) return false;
            if (IsDescendant(draggedId, targetId)) return false;

            var dragged = _index.GetNodeById(draggedId);
            var target  = _index.GetNodeById(targetId);
            if (dragged == null || target == null) return false;

            // Reject same parent + same position (no-op reorder)
            if (dragged.ParentId == target.ParentId)
            {
                var siblings = GetSiblingList(dragged.ParentId);
                int di = siblings.IndexOf(dragged);
                int ti = siblings.IndexOf(target);
                if (Math.Abs(di - ti) == 1) return false; // adjacent → no effective move
            }

            return true;
        }

        public void ExecuteDrop(string draggedId, string targetId, int insertIndex)
        {
            var dragged = _index.GetNodeById(draggedId);
            var target  = _index.GetNodeById(targetId);
            if (dragged == null || target == null) return;

            // Detach from current parent
            var oldSiblings = GetSiblingList(dragged.ParentId);
            oldSiblings.Remove(dragged);

            // Reparent
            dragged.ParentId = targetId;
            int clampedIndex = Math.Clamp(insertIndex, 0, target.Children.Count);
            target.Children.Insert(clampedIndex, dragged);

            // Rebuild index
            _index.Build(_roots);
        }

        // Returns true if candidateId is a descendant of ancestorId
        public bool IsDescendant(string ancestorId, string candidateId)
        {
            var current = _index.GetNodeById(candidateId);
            if (current == null) return false;

            while (current.ParentId != null)
            {
                current = _index.GetNodeById(current.ParentId);
                if (current == null) break;
                if (current.NodeId == ancestorId) return true;
            }
            return false;
        }

        private List<TreeNode> GetSiblingList(string parentId)
        {
            if (parentId == null) return _roots;
            var parent = _index.GetNodeById(parentId);
            return parent?.Children ?? _roots;
        }
    }
}
