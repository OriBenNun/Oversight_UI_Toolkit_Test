using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "TreeData", menuName = "Oversight/Tree Data Asset")]
    public class TreeDataAsset : ScriptableObject
    {
        public NodeData[] Nodes;
    }
}
