using System;
using System.Collections.Generic;
using Oversight.Model;

namespace Oversight.Editor
{
    public static class TreeDataGenerator
    {
        private static readonly string[][] GroupNames =
        {
            new[] { "FOB Kestrel", "TF Ironside", "AOC Vanguard", "JTF Specter",
                    "FOB Halcyon", "TF Nightwatch", "COC Ridgeline", "JTF Corsair",
                    "AOC Sentinel", "TF Wolfpack" },
            new[] { "Sector Alpha", "AO Hammer", "Zone Cerberus", "AO Falcon",
                    "Sector Delta", "AO Trident", "Zone Griffin", "AO Reaper",
                    "Zone Phoenix", "Sector Foxtrot" },
            new[] { "Alpha Team", "ISR Cell", "Recon Team", "Strike Package",
                    "EW Team", "SIGINT Cell", "Overwatch Team", "JTAC Element",
                    "Pathfinder", "Sniper Cell" },
            new[] { "Asset Group", "Forward Package", "Standoff Element", "Coverage Cell",
                    "Relay Node Group", "Loiter Asset Set", "Point Defense Cell", "Spot Report Group" }
        };

        private static readonly string[] MapNames =
        {
            "MSR Tampa", "Phase Line Amber", "Grid 4472 NW", "Checkpoint 17",
            "Objective Crow", "MSR Jackson", "Phase Line Blue", "Battle Position 4",
            "Engagement Area 3", "Limit of Advance", "Fire Support Coord Line", "Patrol Base Echo"
        };

        private static readonly string[] Model3DNames =
        {
            "M1A2 Abrams", "MQ-9 Reaper", "UH-60 Blackhawk", "Bradley IFV",
            "RQ-4 Global Hawk", "Stryker ICV", "AH-64D Apache", "M777 Howitzer",
            "UAS Predator B", "MQ-1C Gray Eagle", "Humvee CROWS", "Paladin SPH"
        };

        private static readonly string[] CameraNames =
        {
            "EO/IR Turret", "Wide-Area WAMI", "FMV Feed", "FLIR Node",
            "Panoramic EO", "LIDAR Scan", "Persistent EO/IR", "SWIR Sensor",
            "High-Alt EO", "FMV Slew-to-Cue", "CCD Turret", "EO Spotlight"
        };

        private static readonly string[] SensorNames =
        {
            "GSR Seismic Node", "SIGINT Collector", "GMTI Radar", "Acoustic Array",
            "RF Emitter Tracker", "Unattended Ground Sensor", "EW Jammer Node", "SAR Radar Pass",
            "ELINT Collector", "Blue Force Tracker", "ADS-B Receiver", "MASINT Collector"
        };

        public static List<TreeNode> Generate(int targetCount = 2500, int maxDepth = 6, int seed = 0)
        {
            var rng = new Random(seed);
            var roots = new List<TreeNode>();
            int created = 0;
            int groupsAtRoot = 10;

            var itemCounters  = new int[5];
            var groupCounters = new int[maxDepth + 1];

            for (int i = 0; i < groupsAtRoot && created < targetCount; i++)
            {
                string name = GroupNames[0][i % GroupNames[0].Length];
                var group = TreeNode.PopulateNewNode(Guid.NewGuid().ToString(), name, NodeType.Group, null, LayerType.None);
                roots.Add(group);
                created++;
                FillGroup(group, 1, maxDepth, targetCount, ref created, rng, itemCounters, groupCounters);
            }

            return roots;
        }

        private static void FillGroup(TreeNode parent, int depth, int maxDepth, int targetCount,
                                       ref int created, Random rng, int[] itemCounters, int[] groupCounters)
        {
            if (created >= targetCount) return;

            bool atLeafDepth = depth >= maxDepth;
            int childCount = atLeafDepth ? 5 : 4;
            var groupBank = GroupNames[Math.Min(depth, GroupNames.Length - 1)];

            for (int i = 0; i < childCount && created < targetCount; i++)
            {
                if (!atLeafDepth && i < 2)
                {
                    groupCounters[depth]++;
                    string name = $"{groupBank[groupCounters[depth] % groupBank.Length]} {groupCounters[depth]}";
                    var group = TreeNode.PopulateNewNode(Guid.NewGuid().ToString(), name, NodeType.Group, parent.NodeId, LayerType.None);
                    parent.AddChild(group, parent.Children.Count);
                    created++;
                    FillGroup(group, depth + 1, maxDepth, targetCount, ref created, rng, itemCounters, groupCounters);
                }
                else
                {
                    var layerType = PickLayerType(depth, maxDepth, rng);
                    itemCounters[(int)layerType]++;
                    string name = BuildItemName(layerType, itemCounters[(int)layerType], rng);
                    var item = TreeNode.PopulateNewNode(Guid.NewGuid().ToString(), name, NodeType.Item, parent.NodeId, layerType);
                    parent.AddChild(item, parent.Children.Count);
                    created++;
                }
            }
        }

        private static LayerType PickLayerType(int depth, int maxDepth, Random rng)
        {
            double r = rng.NextDouble();
            if (depth <= 2)
            {
                if (r < 0.40) return LayerType.Map;
                if (r < 0.70) return LayerType.Model3D;
                if (r < 0.85) return LayerType.Camera;
                return LayerType.Sensor;
            }
            if (depth <= 4)
            {
                if (r < 0.20) return LayerType.Map;
                if (r < 0.40) return LayerType.Model3D;
                if (r < 0.70) return LayerType.Camera;
                return LayerType.Sensor;
            }
            if (r < 0.10) return LayerType.Map;
            if (r < 0.20) return LayerType.Model3D;
            if (r < 0.55) return LayerType.Camera;
            return LayerType.Sensor;
        }

        private static string BuildItemName(LayerType layerType, int counter, Random rng)
        {
            string[] bank = layerType switch
            {
                LayerType.Map     => MapNames,
                LayerType.Model3D => Model3DNames,
                LayerType.Camera  => CameraNames,
                LayerType.Sensor  => SensorNames,
                _                 => MapNames
            };
            return $"{bank[rng.Next(bank.Length)]} {counter}";
        }
    }
}
