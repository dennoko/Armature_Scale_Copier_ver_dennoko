using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ShimotukiRieru.ArmatureScaleCopier
{
    /// <summary>
    /// Armatureオブジェクトの子にあるコンポーネントとTransform情報をコピー・ペーストするエディターツール
    /// </summary>
    public class ArmatureScaleCopierWindow : EditorWindow
    {
        private GameObject sourceArmature;
        private GameObject[] targetArmatures = new GameObject[0];
        private ArmatureData copiedData;
        private Vector2 scrollPosition;
        private Vector2 copiedDataScrollPosition;
        private bool copyTransforms = true;
        private bool copyTransformsScale = true;
        private bool copyTransformsPosition = false;
        private bool copyTransformsRotation = false;
        private bool copyMAComponents = true;
        private bool copyOtherComponents = false;
        private bool showTargetList = true;

        private List<GameObject> _activeAvatars = new List<GameObject>();
        private int _selectedAvatarIndex = -1;

        [MenuItem("dennokoworks/ArmatureScaleCopier_ver.dennoko/Single Copier")]
        public static void ShowWindow()
        {
            GetWindow<ArmatureScaleCopierWindow>("Single Copier");
        }

        private void OnEnable()
        {
            RefreshActiveAvatars();
        }

        private void OnFocus()
        {
            RefreshActiveAvatars();
        }

        private void RefreshActiveAvatars()
        {
            _activeAvatars = VRCHelper.FindActiveAvatars();

            if (sourceArmature == null && _activeAvatars.Count > 0)
            {
                _selectedAvatarIndex = 0;
                ApplySelectedAvatar();
            }
            else if (sourceArmature != null)
            {
                // sourceArmatureの親がアバターリスト内にあれば選択インデックスを同期
                var parent = sourceArmature.transform.parent?.gameObject;
                int idx = parent != null ? _activeAvatars.IndexOf(parent) : -1;
                _selectedAvatarIndex = idx;
            }
            else
            {
                _selectedAvatarIndex = -1;
            }

            Repaint();
        }

        private void ApplySelectedAvatar()
        {
            if (_selectedAvatarIndex < 0 || _selectedAvatarIndex >= _activeAvatars.Count) return;
            var avatar = _activeAvatars[_selectedAvatarIndex];
            var armature = avatar.transform.Find("Armature");
            if (armature != null)
                sourceArmature = armature.gameObject;
        }

        private void DrawAvatarAutoDetectSection()
        {
            GUILayout.Label("アバター自動認識", EditorStyles.boldLabel);

            if (!VRCHelper.IsVRCSdkAvailable)
            {
                EditorGUILayout.HelpBox("VRC SDK未検出のため、アバターの自動認識は利用できません。", MessageType.Info);
                GUILayout.Space(10);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            if (_activeAvatars.Count == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Popup("アクティブアバター", 0, new[] { "アバターが見つかりません" });
                }
            }
            else
            {
                string[] names = _activeAvatars.Select(a => a.name).ToArray();
                int clampedIndex = Mathf.Clamp(_selectedAvatarIndex, 0, names.Length - 1);
                int newIndex = EditorGUILayout.Popup("アクティブアバター", clampedIndex, names);
                if (newIndex != _selectedAvatarIndex)
                {
                    _selectedAvatarIndex = newIndex;
                    ApplySelectedAvatar();
                }
            }

            if (GUILayout.Button("更新", GUILayout.Width(50)))
            {
                RefreshActiveAvatars();
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawAvatarAutoDetectSection();

            // ソースArmature選択
            GUILayout.Label("コピー元 (Source Armature):", EditorStyles.boldLabel);
            sourceArmature = (GameObject)EditorGUILayout.ObjectField(sourceArmature, typeof(GameObject), true);

            if (sourceArmature != null && !IsValidArmature(sourceArmature))
            {
                EditorGUILayout.HelpBox("選択されたオブジェクトは「Armature」という名前ではありません。", MessageType.Warning);
            }

            GUILayout.Space(10);

            // ターゲットArmature選択
            GUILayout.Label("コピー先 (Target Armatures):", EditorStyles.boldLabel);

            showTargetList = EditorGUILayout.Foldout(showTargetList, $"ターゲットリスト ({targetArmatures.Length} 個)");

            if (showTargetList)
            {
                EditorGUI.indentLevel++;


                // リストのサイズを調整するボタン
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    System.Array.Resize(ref targetArmatures, targetArmatures.Length + 1);
                }
                if (GUILayout.Button("-", GUILayout.Width(30)) && targetArmatures.Length > 0)
                {
                    System.Array.Resize(ref targetArmatures, targetArmatures.Length - 1);
                }
                EditorGUILayout.EndHorizontal();

                // 各ターゲットArmatureの設定
                for (int i = 0; i < targetArmatures.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    targetArmatures[i] = (GameObject)EditorGUILayout.ObjectField($"Target {i + 1}", targetArmatures[i], typeof(GameObject), true);

                    if (GUILayout.Button("x", GUILayout.Width(20)))
                    {
                        // この要素を削除
                        var newArray = new GameObject[targetArmatures.Length - 1];
                        for (int j = 0, k = 0; j < targetArmatures.Length; j++)
                        {
                            if (j != i) newArray[k++] = targetArmatures[j];
                        }
                        targetArmatures = newArray;
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    if (targetArmatures[i] != null && !IsValidArmature(targetArmatures[i]))
                    {
                        EditorGUILayout.HelpBox($"Target {i + 1}: 選択されたオブジェクトは「Armature」という名前ではありません。", MessageType.Warning);
                    }
                }

                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);


            copyTransforms = EditorGUILayout.Toggle("Transform情報をコピー", copyTransforms);

            if (copyTransforms)
            {
                EditorGUI.indentLevel++;
                copyTransformsScale = EditorGUILayout.Toggle("スケールをコピー", copyTransformsScale);
                copyTransformsPosition = EditorGUILayout.Toggle("位置をコピー", copyTransformsPosition);
                copyTransformsRotation = EditorGUILayout.Toggle("回転をコピー", copyTransformsRotation);
                EditorGUI.indentLevel--;
            }

            copyMAComponents = EditorGUILayout.Toggle("ModularAvatarコンポーネントをコピー", copyMAComponents);
            copyOtherComponents = EditorGUILayout.Toggle("その他のコンポーネントをコピー", copyOtherComponents);

            if (copyOtherComponents)
            {
                EditorGUILayout.HelpBox("その他のコンポーネントをコピーすると、不明瞭なエラーが発生して正しく動作しない可能性があります。\n" +
                                        "このオプションは慎重に使用してください。", MessageType.Warning);
            }

            GUILayout.Space(15);

            // コピーボタン
            GUI.enabled = sourceArmature != null && IsValidArmature(sourceArmature);
            if (GUILayout.Button("Copy Armature Data", GUILayout.Height(30)))
            {
                CopyArmatureData();
            }
            GUI.enabled = true;

            GUILayout.Space(10);

            // ペーストボタン
            bool hasValidTargets = targetArmatures.Any(t => t != null && IsValidArmature(t));
            GUI.enabled = hasValidTargets && copiedData != null;
            if (GUILayout.Button("Paste Armature Data", GUILayout.Height(30)))
            {
                PasteArmatureData();
            }
            GUI.enabled = true;

            GUILayout.Space(15);

            // コピー済みデータの情報表示
            if (copiedData != null)
            {


                GUILayout.Label("コピー済みデータ:", EditorStyles.boldLabel);
                int totalObjects = CountTotalObjects(copiedData.childrenData);
                EditorGUILayout.HelpBox($"総オブジェクト数: {totalObjects}", MessageType.Info);


                copiedDataScrollPosition = EditorGUILayout.BeginScrollView(copiedDataScrollPosition);

                DisplayChildrenDataRecursive(copiedData.childrenData, 0);

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// オブジェクトがArmatureかどうかを判定
        /// </summary>
        private bool IsValidArmature(GameObject obj)
        {
            return ValidationHelper.IsValidArmature(obj);
        }

        /// <summary>
        /// Armatureの子オブジェクトのデータをコピー（再帰処理）
        /// </summary>
        private void CopyArmatureData()
        {
            if (sourceArmature == null) return;

            copiedData = new ArmatureData();

            foreach (Transform child in sourceArmature.transform)
            {
                var childData = CopyChildObjectDataRecursive(child);
                copiedData.childrenData.Add(childData);
            }

            int totalObjectCount = CountTotalObjects(copiedData.childrenData);
            ArmatureScaleCopierLogger.Log($"Armature '{sourceArmature.name}' のデータをコピーしました。総オブジェクト数: {totalObjectCount}");
        }

        /// <summary>
        /// 子オブジェクトのデータを再帰的にコピー
        /// </summary>
        private ChildObjectData CopyChildObjectDataRecursive(Transform transform)
        {
            var childData = new ChildObjectData
            {
                name = transform.name,
                localPosition = transform.localPosition,
                localRotation = transform.localRotation,
                localScale = transform.localScale,
                componentData = new List<ComponentData>(),
                childrenData = new List<ChildObjectData>()
            };

            // コンポーネントデータの収集
            var components = transform.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null || component is Transform) continue;

                bool shouldCopy = false;

                if (copyMAComponents && IsModularAvatarComponent(component))
                    shouldCopy = true;
                else if (copyOtherComponents && !IsModularAvatarComponent(component))
                    shouldCopy = true;

                if (shouldCopy)
                {
                    var compData = new ComponentData
                    {
                        typeName = component.GetType().AssemblyQualifiedName,
                        serializedData = ArmatureScaleCopierLogger.TryExecute(() => JsonUtility.ToJson(component), "{}", "コンポーネントのシリアライズ")
                    };
                    childData.componentData.Add(compData);
                }
            }

            // 子オブジェクトを再帰的に処理
            foreach (Transform child in transform)
            {
                var childObjectData = CopyChildObjectDataRecursive(child);
                childData.childrenData.Add(childObjectData);
            }

            return childData;
        }

        /// <summary>
        /// 総オブジェクト数をカウント（再帰）
        /// </summary>
        private int CountTotalObjects(List<ChildObjectData> childrenData)
        {
            if (childrenData == null) return 0;

            int count = childrenData.Count;
            foreach (var child in childrenData)
            {
                if (child?.childrenData != null)
                {
                    count += CountTotalObjects(child.childrenData);
                }
            }
            return count;
        }

        /// <summary>
        /// コピーしたデータをターゲットのArmatureにペースト（再帰処理）
        /// </summary>
        private void PasteArmatureData()
        {
            if (copiedData == null) return;

            var validTargets = targetArmatures.Where(t => t != null && IsValidArmature(t)).ToArray();
            if (validTargets.Length == 0)
            {
                Debug.LogWarning("有効なターゲットArmatureが見つかりません。");
                return;
            }

            foreach (var target in validTargets)
            {
                Undo.RegisterCompleteObjectUndo(target, "Paste Armature Data");

                foreach (var childData in copiedData.childrenData)
                {
                    PasteChildObjectDataRecursive(childData, target.transform);
                }

                ArmatureScaleCopierLogger.Log($"Armature '{target.name}' にデータをペーストしました。");
            }

            ArmatureScaleCopierLogger.Log($"合計 {validTargets.Length} 個のArmatureにデータをペーストしました。");
        }

        /// <summary>
        /// 子オブジェクトのデータを再帰的にペースト
        /// </summary>
        private void PasteChildObjectDataRecursive(ChildObjectData childData, Transform parentTransform)
        {
            Transform existingChild = parentTransform.Find(childData.name);

            // 対応する名前のオブジェクトが見つからない場合はスキップ
            if (existingChild == null)
                return;

            GameObject targetChild = existingChild.gameObject;
            Undo.RegisterCompleteObjectUndo(targetChild, "Update Child Object");

            // Transform情報の適用
            if (copyTransforms)
            {
                if (copyTransformsScale)
                    targetChild.transform.localScale = childData.localScale;
                if (copyTransformsPosition)
                    targetChild.transform.localPosition = childData.localPosition;
                if (copyTransformsRotation)
                    targetChild.transform.localRotation = childData.localRotation;
            }

            // コンポーネントの適用
            foreach (var compData in childData.componentData)
            {
                try
                {
                    var componentType = System.Type.GetType(compData.typeName);
                    if (componentType != null)
                    {
                        var existingComponent = targetChild.GetComponent(componentType);
                        if (existingComponent != null)
                        {
                            Undo.RegisterCompleteObjectUndo(existingComponent, "Update Component");
                            JsonUtility.FromJsonOverwrite(compData.serializedData, existingComponent);
                        }
                        else
                        {
                            var newComponent = targetChild.AddComponent(componentType);
                            Undo.RegisterCreatedObjectUndo(newComponent, "Add Component");
                            JsonUtility.FromJsonOverwrite(compData.serializedData, newComponent);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"コンポーネント {compData.typeName} の適用に失敗しました: {e.Message}");
                }
            }

            // 子オブジェクトを再帰的に処理
            if (childData.childrenData != null)
            {
                foreach (var grandChildData in childData.childrenData)
                {
                    if (grandChildData != null)
                    {
                        PasteChildObjectDataRecursive(grandChildData, targetChild.transform);
                    }
                }
            }
        }

        /// <summary>
        /// 子オブジェクトデータを再帰的に表示
        /// </summary>
        private void DisplayChildrenDataRecursive(List<ChildObjectData> childrenData, int indentLevel)
        {
            if (childrenData == null) return;

            string indent = new string(' ', indentLevel * 2);
            foreach (var childData in childrenData)
            {
                if (childData == null) continue;

                EditorGUILayout.LabelField($"{indent}• {childData.name} - {childData.componentData?.Count ?? 0} components");
                if (childData.childrenData != null && childData.childrenData.Count > 0)
                {
                    DisplayChildrenDataRecursive(childData.childrenData, indentLevel + 1);
                }
            }
        }

        /// <summary>
        /// ModularAvatarコンポーネントかどうかを判定
        /// </summary>
        private bool IsModularAvatarComponent(Component component)
        {
            return ModularAvatarHelper.IsModularAvatarComponent(component);
        }
    }

    /// <summary>
    /// コピーするArmatureのデータ
    /// </summary>
    [System.Serializable]
    public class ArmatureData
    {
        public List<ChildObjectData> childrenData = new List<ChildObjectData>();
    }

    /// <summary>
    /// 子オブジェクトのデータ
    /// </summary>
    [System.Serializable]
    public class ChildObjectData
    {
        public string name;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public List<ComponentData> componentData;
        public List<ChildObjectData> childrenData;
    }

    /// <summary>
    /// コンポーネントのデータ
    /// </summary>
    [System.Serializable]
    public class ComponentData
    {
        public string typeName;
        public string serializedData;
    }
}
