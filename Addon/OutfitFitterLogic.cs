using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ShimotukiRieru.ArmatureScaleCopier.Addon
{
    public static class OutfitFitterLogic
    {
        // ─── Bone Adjustments ────────────────────────────────────────────────

        public static void ApplyBoneAdjustments(OutfitPreset preset, Transform armatureRoot)
        {
            if (preset == null || armatureRoot == null) return;

            foreach (var adj in preset.boneAdjustments)
            {
                var bone = FindBoneByPath(armatureRoot, adj.bonePath);
                if (bone == null) continue;

                Undo.RegisterCompleteObjectUndo(bone.gameObject, "Apply Outfit Preset - Bone");
                bone.localPosition = adj.localPosition;
                bone.localRotation = adj.localRotation;
                bone.localScale    = adj.localScale;
            }
        }

        // ─── Components ──────────────────────────────────────────────────────

        public static void ApplyComponents(OutfitPreset preset, Transform armatureRoot, HashSet<string> skippedTypeNames)
        {
            if (preset == null || armatureRoot == null) return;

            foreach (var entry in preset.componentEntries)
            {
                if (skippedTypeNames != null && skippedTypeNames.Contains(entry.typeName)) continue;

                var bone = FindBoneByPath(armatureRoot, entry.bonePath);
                if (bone == null) continue;

                var componentType = Type.GetType(entry.typeName);
                if (componentType == null) continue;

                try
                {
                    var existing = bone.GetComponent(componentType);
                    if (existing != null)
                    {
                        Undo.RegisterCompleteObjectUndo(existing, "Apply Outfit Preset - Component");
                        JsonUtility.FromJsonOverwrite(entry.serializedData, existing);
                    }
                    else
                    {
                        var added = bone.gameObject.AddComponent(componentType);
                        Undo.RegisterCreatedObjectUndo(added, "Apply Outfit Preset - Add Component");
                        JsonUtility.FromJsonOverwrite(entry.serializedData, added);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[OutfitFitter] コンポーネント {GetShortTypeName(entry.typeName)} の適用に失敗: {e.Message}");
                }
            }
        }

        // ─── Setup Outfit (MA) ───────────────────────────────────────────────

        public static bool IsSetupOutfitAvailable()
        {
            return ModularAvatarHelper.IsModularAvatarAvailable;
        }

        public static bool RunSetupOutfit(GameObject costumeRoot)
        {
            if (costumeRoot == null) return false;
            if (!IsSetupOutfitAvailable()) return false;

            var prevSelection = Selection.activeGameObject;
            try
            {
                Selection.activeGameObject = costumeRoot;
                bool result = EditorApplication.ExecuteMenuItem("GameObject/[ModularAvatar] Setup Outfit");
                if (!result)
                    Debug.LogWarning("[OutfitFitter] Setup Outfit の実行に失敗しました。衣装オブジェクトの選択を確認してください。");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OutfitFitter] Setup Outfit の呼び出し中にエラー: {e.Message}");
                return false;
            }
            finally
            {
                Selection.activeGameObject = prevSelection;
            }
        }

        // ─── Blendshapes ─────────────────────────────────────────────────────

        public static int ApplyBlendshapes(OutfitPreset preset, GameObject costumeRoot, HashSet<string> skippedNames)
        {
            if (preset == null || costumeRoot == null) return 0;

            var renderers = costumeRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int appliedCount = 0;

            foreach (var entry in preset.blendshapes)
            {
                if (skippedNames != null && skippedNames.Contains(entry.blendshapeName)) continue;

                foreach (var smr in renderers)
                {
                    if (smr.sharedMesh == null) continue;
                    int idx = smr.sharedMesh.GetBlendShapeIndex(entry.blendshapeName);
                    if (idx < 0) continue;

                    Undo.RegisterCompleteObjectUndo(smr, "Apply Outfit Preset - Blendshape");
                    smr.SetBlendShapeWeight(idx, entry.value);
                    appliedCount++;
                }
            }

            return appliedCount;
        }

        // ─── Record Current State ────────────────────────────────────────────

        public static List<BoneAdjustment> RecordBoneState(Transform armatureRoot)
        {
            var adjustments = new List<BoneAdjustment>();
            if (armatureRoot == null) return adjustments;
            RecordBoneRecursive(armatureRoot, armatureRoot, adjustments);
            return adjustments;
        }

        private static void RecordBoneRecursive(Transform root, Transform current, List<BoneAdjustment> result)
        {
            foreach (Transform child in current)
            {
                result.Add(new BoneAdjustment
                {
                    bonePath      = GetRelativePath(root, child),
                    localPosition = child.localPosition,
                    localRotation = child.localRotation,
                    localScale    = child.localScale
                });
                RecordBoneRecursive(root, child, result);
            }
        }

        public static List<ComponentEntry> CollectComponentEntries(Transform armatureRoot, bool maOnly)
        {
            var entries = new List<ComponentEntry>();
            if (armatureRoot == null) return entries;
            CollectComponentsRecursive(armatureRoot, armatureRoot, entries, maOnly);
            return entries;
        }

        private static void CollectComponentsRecursive(Transform root, Transform current, List<ComponentEntry> entries, bool maOnly)
        {
            foreach (var comp in current.GetComponents<Component>())
            {
                if (comp == null || comp is Transform) continue;
                if (maOnly && !ModularAvatarHelper.IsModularAvatarComponent(comp)) continue;

                try
                {
                    entries.Add(new ComponentEntry
                    {
                        bonePath       = GetRelativePath(root, current),
                        typeName       = comp.GetType().AssemblyQualifiedName,
                        serializedData = JsonUtility.ToJson(comp)
                    });
                }
                catch { }
            }

            foreach (Transform child in current)
                CollectComponentsRecursive(root, child, entries, maOnly);
        }

        // ─── Unique Type Names ────────────────────────────────────────────────

        public static List<string> GetUniqueComponentTypeNames(OutfitPreset preset)
        {
            var seen = new HashSet<string>();
            var ordered = new List<string>();
            if (preset == null) return ordered;

            foreach (var entry in preset.componentEntries)
            {
                if (!string.IsNullOrEmpty(entry.typeName) && seen.Add(entry.typeName))
                    ordered.Add(entry.typeName);
            }
            return ordered;
        }

        public static string GetShortTypeName(string assemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(assemblyQualifiedName)) return "Unknown";
            var typeName = assemblyQualifiedName.Split(',')[0].Trim();
            int dot = typeName.LastIndexOf('.');
            return dot >= 0 ? typeName.Substring(dot + 1) : typeName;
        }

        // ─── Path Utilities ───────────────────────────────────────────────────

        private static Transform FindBoneByPath(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path)) return root;
            return root.Find(path);
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (target == root) return "";
            var parts = new List<string>();
            var current = target;
            while (current != null && current != root)
            {
                parts.Insert(0, current.name);
                current = current.parent;
            }
            return string.Join("/", parts);
        }
    }
}
