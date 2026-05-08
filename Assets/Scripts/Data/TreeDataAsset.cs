using UnityEngine;

namespace Oversight.Data
{
    [CreateAssetMenu(fileName = "TreeData", menuName = "Oversight/Tree Data Asset")]
    public class TreeDataAsset : ScriptableObject
    {
        public NodeData[] Nodes;
    }
}
