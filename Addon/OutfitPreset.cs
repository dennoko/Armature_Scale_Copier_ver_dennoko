using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShimotukiRieru.ArmatureScaleCopier.Addon
{
    [CreateAssetMenu(fileName = "OutfitPreset", menuName = "dennokoworks/Outfit Preset")]
    public class OutfitPreset : ScriptableObject
    {
        public List<BoneAdjustment> boneAdjustments  = new List<BoneAdjustment>();
        public List<ComponentEntry> componentEntries  = new List<ComponentEntry>();
        public List<BlendshapeEntry> blendshapes      = new List<BlendshapeEntry>();
        public bool runSetupOutfit = true;
    }

    [Serializable]
    public class BoneAdjustment
    {
        public string     bonePath;
        public Vector3    localPosition;
        public Quaternion localRotation;
        public Vector3    localScale;
    }

    [Serializable]
    public class ComponentEntry
    {
        public string bonePath;
        public string typeName;
        public string serializedData;
    }

    [Serializable]
    public class BlendshapeEntry
    {
        public string blendshapeName;
        public float  value;
    }
}
