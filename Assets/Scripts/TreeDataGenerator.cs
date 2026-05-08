using System.Collections.Generic;
using Oversight.Model;

namespace Oversight
{
    public static class TreeDataGenerator
    {
        public static List<TreeNode> Generate(int targetCount = 2500, int maxDepth = 6)
        {
            var roots = new List<TreeNode>();
            int created = 0;
            int groupsAtRoot = 10;

            for (int i = 0; i < groupsAtRoot && created < targetCount; i++)
            {
                var group = new TreeNode($"Group_0_{i}", NodeType.Group);
                roots.Add(group);
                created++;
                FillGroup(group, 1, maxDepth, targetCount, ref created);
            }

            return roots;
        }

        private static void FillGroup(TreeNode parent, int depth, int maxDepth, int targetCount, ref int created)
        {
            if (created >= targetCount) return;

            bool atLeafDepth = depth >= maxDepth;
            int childCount = atLeafDepth ? 5 : 4;

            for (int i = 0; i < childCount && created < targetCount; i++)
            {
                if (!atLeafDepth && i < 2)
                {
                    var group = new TreeNode($"Group_{depth}_{i}", NodeType.Group, parent.NodeId);
                    parent.AddChild(group, parent.Children.Count);
                    created++;
                    FillGroup(group, depth + 1, maxDepth, targetCount, ref created);
                }
                else
                {
                    var item = new TreeNode($"Item_{depth}_{i}", NodeType.Item, parent.NodeId);
                    parent.AddChild(item, parent.Children.Count);
                    created++;
                }
            }
        }
    }
}
