using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ShimotukiRieru.ArmatureScaleCopier
{
    public class ArmatureBatchOperations : EditorWindow
    {
        // ─── Fields ──────────────────────────────────────────────────────────

        private Vector2 scrollPosition;
        private List<GameObject> ArmatureObjects = new List<GameObject>();
        private GameObject sourceArmature;
        private GameObject searchRoot;
        private GameObject previousSearchRoot;
        private bool autoFindArmatures = true;
        private bool includeInactive = false;

        private List<GameObject> _activeAvatars = new List<GameObject>();
        private int _selectedAvatarIndex = -1;

        private static System.Type _addonWindowType;
        private static bool        _addonTypeSearched;

        private bool copyTransforms = true;
        private bool copyTransformsScale = true;
        private bool copyTransformsPosition = false;
        private bool copyTransformsRotation = false;
        private bool copyMAComponents = true;
        private bool copyOtherComponents = false;

        // ─── Status ──────────────────────────────────────────────────────────

        private string _statusMessage  = "準備完了";
        private int    _statusLevel    = 0; // 0=info 1=success 2=warning 3=error
        private double _statusResetTime = -1.0;

        // ─── Window Registration ─────────────────────────────────────────────

        [MenuItem("dennokoworks/ArmatureScaleCopier_ver.dennoko/Avatar Batch Copier")]
        public static void ShowWindow()
        {
            GetWindow<ArmatureBatchOperations>("Avatar Batch Copier");
        }

        // ─── Lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            RefreshActiveAvatars();
            if (autoFindArmatures && searchRoot != null && _activeAvatars.Count == 0)
                FindAllArmatures();
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

        // ─── OnGUI Entry Point ───────────────────────────────────────────────

        private void OnGUI()
        {
            if (_statusResetTime > 0 && EditorApplication.timeSinceStartup > _statusResetTime)
            {
                _statusMessage   = "準備完了";
                _statusLevel     = 0;
                _statusResetTime = -1.0;
            }

            ArmatureScaleCopierTheme.Initialize();
            ArmatureScaleCopierTheme.PushEditorTheme();
            try
            {
                EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), ArmatureScaleCopierTheme.Surface0);

                DrawHeader();

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                DrawSettingsArea();
                EditorGUILayout.EndScrollView();

                DrawFooter();
                DrawStatusBar();
            }
            finally
            {
                ArmatureScaleCopierTheme.PopEditorTheme();
            }
        }

        // ─── Header ──────────────────────────────────────────────────────────

        private void DrawHeader()
        {
            EditorGUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label("Avatar Batch Copier", ArmatureScaleCopierTheme.TitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Space(6);
            GUILayout.EndHorizontal();
            EditorGUILayout.Space(6);
            DrawSeparator();
        }

        // ─── Settings Area ───────────────────────────────────────────────────

        private void DrawSettingsArea()
        {
            GUILayout.BeginVertical();

            // アバター自動認識
            DrawSection("アバター自動認識", () =>
            {
                if (!VRCHelper.IsVRCSdkAvailable)
                {
                    EditorGUILayout.HelpBox("VRC SDK未検出のため、アバターの自動認識は利用できません。", MessageType.Info);
                    return;
                }

                GUILayout.BeginHorizontal();
                if (_activeAvatars.Count == 0)
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.Popup("アクティブアバター", 0, new[] { "アバターが見つかりません" });
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
                if (GUILayout.Button("更新", ArmatureScaleCopierTheme.MiniButtonStyle, GUILayout.Width(50)))
                    RefreshActiveAvatars();
                GUILayout.EndHorizontal();
            });

            // 検索範囲
            DrawSection("検索範囲", () =>
            {
                var newRoot = (GameObject)EditorGUILayout.ObjectField("検索対象オブジェクト", searchRoot, typeof(GameObject), true);
                if (newRoot != searchRoot)
                    searchRoot = newRoot;
            });

            // searchRoot 変更時の自動再検索
            if (autoFindArmatures && searchRoot != previousSearchRoot)
            {
                previousSearchRoot = searchRoot;
                if (searchRoot != null)
                    sourceArmature = searchRoot.transform.Find("Armature")?.gameObject;
                FindAllArmatures();
            }

            // 設定
            DrawSection("設定", () =>
            {
                autoFindArmatures = EditorGUILayout.Toggle("自動的にArmatureを検索", autoFindArmatures);
                includeInactive   = EditorGUILayout.Toggle("非アクティブオブジェクトも含む", includeInactive);
                EditorGUILayout.Space(4);
                if (GUILayout.Button("Armatureオブジェクトを再検索", ArmatureScaleCopierTheme.SecondaryButtonStyle))
                    FindAllArmatures();
            });

            // Transform コピーオプション
            DrawToggleSection("Transform 情報をコピー", ref copyTransforms, () =>
            {
                copyTransformsScale    = EditorGUILayout.Toggle("スケールをコピー",   copyTransformsScale);
                copyTransformsPosition = EditorGUILayout.Toggle("位置をコピー",       copyTransformsPosition);
                copyTransformsRotation = EditorGUILayout.Toggle("回転をコピー",       copyTransformsRotation);
            });

            // コンポーネントオプション
            DrawSection("コンポーネント設定", () =>
            {
                copyMAComponents    = EditorGUILayout.Toggle("MAコンポーネントをコピー",       copyMAComponents);
                copyOtherComponents = EditorGUILayout.Toggle("その他のコンポーネントをコピー", copyOtherComponents);
                if (copyOtherComponents)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox(
                        "その他のコンポーネントをコピーすると、不明瞭なエラーが発生して正しく動作しない可能性があります。\nこのオプションは慎重に使用してください。",
                        MessageType.Warning);
                }
            });

            // コピー元 Armature
            DrawSection("コピー元 Armature", () =>
            {
                sourceArmature = (GameObject)EditorGUILayout.ObjectField(sourceArmature, typeof(GameObject), true);
                if (sourceArmature != null && !IsValidArmature(sourceArmature))
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox("選択されたオブジェクトは有効なArmatureではありません。", MessageType.Warning);
                }
            });

            DrawAddonSection();

            // Armature 一覧
            string listTitle = searchRoot != null
                ? $"Armature 一覧 — '{searchRoot.name}' ({ArmatureObjects.Count} 個)"
                : "Armature 一覧";

            DrawSection(listTitle, () =>
            {
                if (searchRoot == null)
                {
                    GUILayout.Label("検索対象オブジェクトを指定してください。", ArmatureScaleCopierTheme.SecondaryTextStyle);
                    return;
                }

                if (ArmatureObjects.Count == 0)
                {
                    EditorGUILayout.HelpBox($"'{searchRoot.name}' 内にArmatureオブジェクトが見つかりません。", MessageType.Info);
                    return;
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("すべて選択", ArmatureScaleCopierTheme.SecondaryButtonStyle))
                    SelectAllArmatures(true);
                if (GUILayout.Button("すべて解除", ArmatureScaleCopierTheme.SecondaryButtonStyle))
                    SelectAllArmatures(false);
                GUILayout.EndHorizontal();

                EditorGUILayout.Space(6);

                for (int i = 0; i < ArmatureObjects.Count; i++)
                {
                    if (ArmatureObjects[i] == null) { ArmatureObjects.RemoveAt(i--); continue; }

                    GUILayout.BeginHorizontal();
                    bool wasSelected = Selection.gameObjects.Contains(ArmatureObjects[i]);
                    bool isSelected  = EditorGUILayout.Toggle(wasSelected, GUILayout.Width(20));
                    if (isSelected != wasSelected)
                    {
                        var sel = Selection.gameObjects.ToList();
                        if (isSelected) sel.Add(ArmatureObjects[i]);
                        else            sel.Remove(ArmatureObjects[i]);
                        Selection.objects = sel.ToArray();
                    }
                    EditorGUILayout.ObjectField(ArmatureObjects[i], typeof(GameObject), true);
                    int childCount = CountAllChildren(ArmatureObjects[i].transform);
                    GUILayout.Label($"({childCount})", ArmatureScaleCopierTheme.CaptionStyle, GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                }
            });

            GUILayout.EndVertical();
        }

        // ─── Footer ──────────────────────────────────────────────────────────

        private void DrawFooter()
        {
            GUILayout.BeginVertical(ArmatureScaleCopierTheme.CardStyle);
            DrawSeparator();

            bool canCopy = sourceArmature != null && Selection.gameObjects.Length > 0;
            using (new EditorGUI.DisabledScope(!canCopy))
            {
                if (GUILayout.Button("選択されたArmatureにコピー", ArmatureScaleCopierTheme.ActionButtonStyle))
                    BatchCopyToSelected();
            }

            GUILayout.EndVertical();
        }

        // ─── Status Bar ──────────────────────────────────────────────────────

        private void DrawStatusBar()
        {
            GUILayout.Box(_statusMessage, ArmatureScaleCopierTheme.GetStatusStyle(_statusLevel), GUILayout.ExpandWidth(true));
        }

        // ─── Section Helpers ─────────────────────────────────────────────────

        private void DrawSection(string title, System.Action content)
        {
            GUILayout.BeginVertical(ArmatureScaleCopierTheme.CardStyle);
            GUILayout.Label(title, ArmatureScaleCopierTheme.SectionHeaderStyle);
            DrawSeparator();
            content?.Invoke();
            GUILayout.EndVertical();
        }

        private void DrawToggleSection(string title, ref bool toggle, System.Action content)
        {
            GUILayout.BeginVertical(ArmatureScaleCopierTheme.CardStyle);

            GUILayout.BeginHorizontal();
            var headerStyle = toggle ? ArmatureScaleCopierTheme.ToggleSectionOnStyle : ArmatureScaleCopierTheme.ToggleSectionOffStyle;
            EditorGUI.BeginChangeCheck();
            bool newToggle = EditorGUILayout.ToggleLeft(title, toggle, headerStyle, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck()) { toggle = newToggle; Repaint(); }
            GUILayout.EndHorizontal();

            DrawSeparator();

            using (new EditorGUI.DisabledGroupScope(!toggle))
                content?.Invoke();

            GUILayout.EndVertical();
        }

        private void DrawSeparator()
        {
            var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, ArmatureScaleCopierTheme.Outline);
            EditorGUILayout.Space(4);
        }

        private void SetStatus(string message, int level, double autoResetSeconds = 3.0)
        {
            _statusMessage   = message;
            _statusLevel     = level;
            _statusResetTime = level == 0 ? -1.0 : EditorApplication.timeSinceStartup + autoResetSeconds;
            Repaint();
        }

        // ─── Logic ───────────────────────────────────────────────────────────

        private void FindAllArmatures()
        {
            ArmatureObjects.Clear();

            if (searchRoot == null) return;

            var allObjects = searchRoot.GetComponentsInChildren<Transform>(includeInactive)
                .Select(t => t.gameObject)
                .ToArray();

            foreach (var obj in allObjects)
            {
                if (IsValidArmature(obj) &&
                    obj.scene.isLoaded &&
                    !EditorUtility.IsPersistent(obj) &&
                    obj != sourceArmature)
                {
                    ArmatureObjects.Add(obj);
                }
            }

            ArmatureObjects.Sort((a, b) => string.Compare(a.name, b.name));
        }

        private bool IsValidArmature(GameObject obj)
        {
            return ValidationHelper.IsValidArmature(obj);
        }

        private void SelectAllArmatures(bool select)
        {
            if (select)
                Selection.objects = ArmatureObjects.Where(obj => obj != null).ToArray();
            else
                Selection.objects = new Object[0];
        }

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

            var sourceData = new ArmatureData();
            foreach (Transform child in sourceArmature.transform)
                sourceData.childrenData.Add(CopyChildObjectDataRecursive(child));

            foreach (var target in selectedArmatures)
            {
                Undo.RegisterCompleteObjectUndo(target, "Batch Copy Armature Data");
                foreach (var childData in sourceData.childrenData)
                    ApplyChildObjectDataRecursive(childData, target.transform);
            }

            int totalObjects = CountTotalObjects(sourceData.childrenData);
            Debug.Log($"[ArmatureScaleCopier] {selectedArmatures.Length}個のArmatureに '{sourceArmature.name}' のデータをコピーしました。総オブジェクト数: {totalObjects}");
            SetStatus($"{selectedArmatures.Length} 個のArmatureにコピーしました。", 1);
        }

        private ChildObjectData CopyChildObjectDataRecursive(Transform transform)
        {
            var childData = new ChildObjectData
            {
                name          = transform.name,
                localPosition = transform.localPosition,
                localRotation = transform.localRotation,
                localScale    = transform.localScale,
                componentData = new List<ComponentData>(),
                childrenData  = new List<ChildObjectData>()
            };

            foreach (var component in transform.GetComponents<Component>())
            {
                if (component == null || component is Transform) continue;

                bool shouldCopy = false;
                if (copyMAComponents    && IsModularAvatarComponent(component)) shouldCopy = true;
                if (copyOtherComponents && !IsModularAvatarComponent(component)) shouldCopy = true;
                if (!shouldCopy) continue;

                childData.componentData.Add(new ComponentData
                {
                    typeName       = component.GetType().AssemblyQualifiedName,
                    serializedData = ArmatureScaleCopierLogger.TryExecute(() => JsonUtility.ToJson(component), "{}", "コンポーネントのシリアライズ")
                });
            }

            foreach (Transform child in transform)
                childData.childrenData.Add(CopyChildObjectDataRecursive(child));

            return childData;
        }

        private void ApplyChildObjectDataRecursive(ChildObjectData childData, Transform parentTransform)
        {
            Transform existingChild = parentTransform.Find(childData.name);
            if (existingChild == null) return;

            var targetChild = existingChild.gameObject;
            Undo.RegisterCompleteObjectUndo(targetChild, "Update Child Object");

            if (copyTransforms)
            {
                if (copyTransformsPosition) targetChild.transform.localPosition = childData.localPosition;
                if (copyTransformsRotation) targetChild.transform.localRotation = childData.localRotation;
                if (copyTransformsScale)    targetChild.transform.localScale    = childData.localScale;
            }

            foreach (var compData in childData.componentData)
            {
                try
                {
                    var componentType = System.Type.GetType(compData.typeName);
                    if (componentType == null) continue;

                    var existing = targetChild.GetComponent(componentType);
                    if (existing != null)
                    {
                        Undo.RegisterCompleteObjectUndo(existing, "Update Component");
                        JsonUtility.FromJsonOverwrite(compData.serializedData, existing);
                    }
                    else
                    {
                        var added = targetChild.AddComponent(componentType);
                        Undo.RegisterCreatedObjectUndo(added, "Add Component");
                        JsonUtility.FromJsonOverwrite(compData.serializedData, added);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"コンポーネント {compData.typeName} の適用に失敗しました: {e.Message}");
                }
            }

            if (childData.childrenData != null)
            {
                foreach (var grandChild in childData.childrenData)
                {
                    if (grandChild != null)
                        ApplyChildObjectDataRecursive(grandChild, targetChild.transform);
                }
            }
        }

        private int CountTotalObjects(List<ChildObjectData> childrenData)
        {
            if (childrenData == null) return 0;
            int count = childrenData.Count;
            foreach (var child in childrenData)
                if (child?.childrenData != null)
                    count += CountTotalObjects(child.childrenData);
            return count;
        }

        private int CountAllChildren(Transform parent)
        {
            int count = parent.childCount;
            foreach (Transform child in parent)
                count += CountAllChildren(child);
            return count;
        }

        private bool IsModularAvatarComponent(Component component)
        {
            return ModularAvatarHelper.IsModularAvatarComponent(component);
        }

        // ─── Addon Section ────────────────────────────────────────────────────

        private static System.Type GetAddonWindowType()
        {
            if (!_addonTypeSearched)
            {
                _addonTypeSearched = true;
                _addonWindowType = System.Type.GetType(
                    "ShimotukiRieru.ArmatureScaleCopier.Addon.OutfitFitterWindow, " +
                    "ShimotukiRieru.ArmatureScaleCopier.Addon");
            }
            return _addonWindowType;
        }

        private void DrawAddonSection()
        {
            if (GetAddonWindowType() == null) return;

            DrawSection("Outfit Fitter  [Addon]", () =>
            {
                if (GUILayout.Button("Outfit Fitter を開く →", ArmatureScaleCopierTheme.SecondaryButtonStyle))
                {
                    var method = GetAddonWindowType().GetMethod("ShowWindow",
                        BindingFlags.Static | BindingFlags.Public);
                    method?.Invoke(null, null);
                }
            });
        }
    }
}
