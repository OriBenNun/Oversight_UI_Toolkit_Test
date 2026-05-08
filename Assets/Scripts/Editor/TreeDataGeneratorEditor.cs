using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Oversight.Model;
using Oversight.Data;

namespace Oversight.Editor
{
    public static class TreeDataGeneratorEditor
    {
        [MenuItem("Oversight/Generate Tree Data")]
        private static void GenerateTreeData()
        {
            var roots = TreeDataGenerator.Generate();

            var nodeDataList = new List<NodeData>();
            var queue = new Queue<TreeNode>(roots);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                nodeDataList.Add(new NodeData
                {
                    Id          = node.NodeId,
                    ParentId    = node.ParentId ?? "",
                    DisplayName = node.DisplayName,
                    NodeType    = node.NodeType,
                    LayerType   = node.LayerType
                });
                foreach (var child in node.Children)
                    queue.Enqueue(child);
            }

            if (!System.IO.Directory.Exists("Assets/Resources"))
            {
                System.IO.Directory.CreateDirectory("Assets/Resources");
                AssetDatabase.Refresh();
            }

            const string assetPath = "Assets/Resources/TreeData.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TreeDataAsset>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TreeDataAsset>();
                asset.Nodes = nodeDataList.ToArray();
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            else
            {
                asset.Nodes = nodeDataList.ToArray();
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Oversight] Generated {nodeDataList.Count} nodes → {assetPath}");
        }
    }
}
