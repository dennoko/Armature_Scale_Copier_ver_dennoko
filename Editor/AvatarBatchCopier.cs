using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ShimotukiRieru.ArmatureScaleCopier
{
    /// <summary>
    /// Armature関連の一括操作ユーティリティ
    /// </summary>
    public class ArmatureBatchOperations : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<GameObject> ArmatureObjects = new List<GameObject>();
        private GameObject sourceArmature;
        private GameObject searchRoot;
        private GameObject previousSearchRoot;
        private bool autoFindArmatures = true;
        private bool includeInactive = false;

        private List<GameObject> _activeAvatars = new List<GameObject>();
        private int _selectedAvatarIndex = -1;
        private bool copyTransforms = true;
        private bool copyTransformsScale = true;
        private bool copyTransformsPosition = false;
        private bool copyTransformsRotation = false;
        private bool copyMAComponents = true;
        private bool copyOtherComponents = false;

        [MenuItem("dennokoworks/ArmatureScaleCopier_ver.dennoko/Avatar Batch Copier")]
        public static void ShowWindow()
        {
            GetWindow<ArmatureBatchOperations>("Avatar Batch Copier");
        }

        private void OnEnable()
        {
            RefreshActiveAvatars();
            if (autoFindArmatures && searchRoot != null && _activeAvatars.Count == 0)
            {
                FindAllArmatures();
            }
        }

        private void OnFocus()
        {
            RefreshActiveAvatars();
        }

        private void RefreshActiveAvatars()
        {
            _activeAvatars = VRCHelper.FindActiveAvatars();

            if (searchRoot == null && _activeAvatars.Count > 0)
            {
                _selectedAvatarIndex = 0;
                ApplySelectedAvatar();
            }
            else
            {
                int idx = _activeAvatars.IndexOf(searchRoot);
                _selectedAvatarIndex = idx;
            }

            Repaint();
        }

        private void ApplySelectedAvatar()
        {
            if (_selectedAvatarIndex < 0 || _selectedAvatarIndex >= _activeAvatars.Count) return;
            var avatar = _activeAvatars[_selectedAvatarIndex];
            searchRoot = avatar;
            previousSearchRoot = avatar;
            sourceArmature = avatar.transform.Find("Armature")?.gameObject;
            FindAllArmatures();
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

            // 検索範囲
            GUILayout.Label("検索範囲", EditorStyles.boldLabel);
            searchRoot = (GameObject)EditorGUILayout.ObjectField("検索対象オブジェクト", searchRoot, typeof(GameObject), true);

            // 検索対象が変更された場合の自動再検索
            if (autoFindArmatures && searchRoot != previousSearchRoot)
            {
                previousSearchRoot = searchRoot;

                // そのオブジェクト内にあるArmatureがあればsourceArmatureに代入
                sourceArmature = searchRoot.transform.Find("Armature")?.gameObject;

                // Armatureオブジェクトを再検索
                FindAllArmatures();
            }

            GUILayout.Space(10);

            // 設定
            GUILayout.Label("設定", EditorStyles.boldLabel);
            autoFindArmatures = EditorGUILayout.Toggle("自動的にArmatureを検索", autoFindArmatures);
            includeInactive = EditorGUILayout.Toggle("非アクティブオブジェクトも含む", includeInactive);

            GUILayout.Space(10);


            if (GUILayout.Button("Armatureオブジェクトを再検索"))
            {
                FindAllArmatures();
            }

            GUILayout.Space(15);


            copyTransforms = EditorGUILayout.Toggle("Transform情報をコピー", copyTransforms);
            if (copyTransforms)
            {
                copyTransformsScale = EditorGUILayout.Toggle("  スケールをコピー", copyTransformsScale);
                copyTransformsPosition = EditorGUILayout.Toggle("  位置をコピー", copyTransformsPosition);
                copyTransformsRotation = EditorGUILayout.Toggle("  回転をコピー", copyTransformsRotation);
            }
            copyMAComponents = EditorGUILayout.Toggle("MAコンポーネントをコピー", copyMAComponents);
            copyOtherComponents = EditorGUILayout.Toggle("その他のコンポーネントをコピー", copyOtherComponents);

            if (copyOtherComponents)
            {
                EditorGUILayout.HelpBox("その他のコンポーネントをコピーすると、不明瞭なエラーが発生して正しく動作しない可能性があります。\n" +
                                        "このオプションは慎重に使用してください。", MessageType.Warning);
            }

            GUILayout.Space(15);

            // ソースArmature
            GUILayout.Label("コピー元 Armature", EditorStyles.boldLabel);
            sourceArmature = (GameObject)EditorGUILayout.ObjectField(sourceArmature, typeof(GameObject), true);

            if (sourceArmature != null && !IsValidArmature(sourceArmature))
            {
                EditorGUILayout.HelpBox("選択されたオブジェクトは有効なArmatureではありません。", MessageType.Warning);
            }

            GUILayout.Space(15);

            // Armature一覧

            if (searchRoot != null)
            {
                string searchScopeText = $"'{searchRoot.name}' 内の";
                GUILayout.Label($"{searchScopeText}Armatureオブジェクト ({ArmatureObjects.Count}個)", EditorStyles.boldLabel);
            }

            if (ArmatureObjects.Count == 0)
            {
                if (searchRoot != null)
                {
                    string noResultMessage = $"'{searchRoot.name}' 内にArmatureオブジェクトが見つかりません。";
                    EditorGUILayout.HelpBox(noResultMessage, MessageType.Info);
                }
            }
            else
            {
                // 全選択/全解除ボタン
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("すべて選択"))
                {
                    SelectAllArmatures(true);
                }
                if (GUILayout.Button("すべて解除"))
                {
                    SelectAllArmatures(false);
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                // Armature一覧表示
                for (int i = 0; i < ArmatureObjects.Count; i++)
                {
                    if (ArmatureObjects[i] == null)
                    {
                        ArmatureObjects.RemoveAt(i);
                        i--;
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();

                    bool wasSelected = Selection.gameObjects.Contains(ArmatureObjects[i]);
                    bool isSelected = EditorGUILayout.Toggle(wasSelected, GUILayout.Width(20));

                    if (isSelected != wasSelected)
                    {
                        if (isSelected)
                        {
                            var newSelection = Selection.gameObjects.ToList();
                            newSelection.Add(ArmatureObjects[i]);
                            Selection.objects = newSelection.ToArray();
                        }
                        else
                        {
                            var newSelection = Selection.gameObjects.ToList();
                            newSelection.Remove(ArmatureObjects[i]);
                            Selection.objects = newSelection.ToArray();
                        }
                    }

                    EditorGUILayout.ObjectField(ArmatureObjects[i], typeof(GameObject), true);

                    // 総子オブジェクト数の表示（再帰的）
                    int totalChildren = CountAllChildren(ArmatureObjects[i].transform);
                    EditorGUILayout.LabelField($"({totalChildren} objects)", GUILayout.Width(100));

                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(15);

                // 一括操作ボタン
                GUILayout.Label("一括操作", EditorStyles.boldLabel);

                GUI.enabled = sourceArmature != null && Selection.gameObjects.Length > 0;
                if (GUILayout.Button("選択されたArmatureにコピー", GUILayout.Height(30)))
                {
                    BatchCopyToSelected();
                }
                GUI.enabled = true;
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 指定範囲内のすべてのArmatureオブジェクトを検索
        /// </summary>
        private void FindAllArmatures()
        {
            ArmatureObjects.Clear();

            GameObject[] allObjects;

            if (searchRoot == null)
            {
                // シーン全体で検索しないように。
                return;
            }

            // 指定オブジェクト内から検索
            allObjects = searchRoot.GetComponentsInChildren<Transform>(includeInactive)
                .Select(t => t.gameObject)
                .ToArray();

            foreach (var obj in allObjects)
            {
                if (IsValidArmature(obj) &&
                    obj.scene.isLoaded && // シーンに存在するオブジェクトのみ
                    !EditorUtility.IsPersistent(obj)) // プレハブではないオブジェクトのみ
                {
                    if (obj == sourceArmature)
                    {
                        // ソースArmatureは除外
                        continue;
                    }
                    ArmatureObjects.Add(obj);
                }
            }

            ArmatureObjects.Sort((a, b) => string.Compare(a.name, b.name));
        }

        /// <summary>
        /// オブジェクトがArmatureかどうかを判定
        /// </summary>
        private bool IsValidArmature(GameObject obj)
        {
            return ValidationHelper.IsValidArmature(obj);
        }

        /// <summary>
        /// すべてのArmatureの選択状態を変更
        /// </summary>
        private void SelectAllArmatures(bool select)
        {
            if (select)
            {
                Selection.objects = ArmatureObjects.Where(obj => obj != null).ToArray();
            }
            else
            {
                Selection.objects = new Object[0];
            }
        }

        /// <summary>
        /// 選択されたArmatureにソースのデータを一括コピー（再帰処理）
        /// </summary>
        private void BatchCopyToSelected()
        {
            if (sourceArmature == null) return;

            var selectedArmatures = Selection.gameObjects.Where(obj =>
                obj != null && obj != sourceArmature && IsValidArmature(obj)).ToArray();

            if (selectedArmatures.Length == 0)
            {
                EditorUtility.DisplayDialog("エラー", "コピー先のArmatureオブジェクトが選択されていません。", "OK");
                return;
            }

            // ソースデータの収集（再帰的）
            var sourceData = new ArmatureData();
            foreach (Transform child in sourceArmature.transform)
            {
                var childData = CopyChildObjectDataRecursive(child);
                sourceData.childrenData.Add(childData);
            }

            // 各ターゲットに適用
            foreach (var target in selectedArmatures)
            {
                Undo.RegisterCompleteObjectUndo(target, "Batch Copy Armature Data");

                foreach (var childData in sourceData.childrenData)
                {
                    ApplyChildObjectDataRecursive(childData, target.transform);
                }
            }

            int totalObjects = CountTotalObjects(sourceData.childrenData);
            Debug.Log($"{selectedArmatures.Length}個のArmatureに '{sourceArmature.name}' のデータをコピーしました。総オブジェクト数: {totalObjects}");
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

                bool shouldCopyComponent = false;
                if (copyMAComponents && IsModularAvatarComponent(component))
                    shouldCopyComponent = true;
                if (copyOtherComponents && !IsModularAvatarComponent(component))
                    shouldCopyComponent = true;

                if (!shouldCopyComponent) continue;

                var compData = new ComponentData
                {
                    typeName = component.GetType().AssemblyQualifiedName,
                    serializedData = ArmatureScaleCopierLogger.TryExecute(() => JsonUtility.ToJson(component), "{}", "コンポーネントのシリアライズ")
                };
                childData.componentData.Add(compData);
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
        /// 子オブジェクトのデータを再帰的に適用
        /// </summary>
        private void ApplyChildObjectDataRecursive(ChildObjectData childData, Transform parentTransform)
        {
            Transform existingChild = parentTransform.Find(childData.name);
            GameObject targetChild;

            if (existingChild == null)
                return;

            targetChild = existingChild.gameObject;
            Undo.RegisterCompleteObjectUndo(targetChild, "Update Child Object");

            // Transform情報の適用
            if (copyTransforms)
            {
                if (copyTransformsPosition)
                    targetChild.transform.localPosition = childData.localPosition;
                if (copyTransformsRotation)
                    targetChild.transform.localRotation = childData.localRotation;
                if (copyTransformsScale)
                    targetChild.transform.localScale = childData.localScale;
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
                        ApplyChildObjectDataRecursive(grandChildData, targetChild.transform);
                    }
                }
            }
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
        /// Transform階層の総子オブジェクト数をカウント（再帰）
        /// </summary>
        private int CountAllChildren(Transform parent)
        {
            int count = parent.childCount;
            foreach (Transform child in parent)
            {
                count += CountAllChildren(child);
            }
            return count;
        }

        /// <summary>
        /// ModularAvatarコンポーネントかどうかを判定
        /// </summary>
        private bool IsModularAvatarComponent(Component component)
        {
            return ModularAvatarHelper.IsModularAvatarComponent(component);
        }
    }
}
