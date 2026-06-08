using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ShimotukiRieru.ArmatureScaleCopier
{
    public class ArmatureScaleCopierWindow : EditorWindow
    {
        // ─── Fields ──────────────────────────────────────────────────────────

        private GameObject sourceArmature;
        private GameObject[] targetArmatures = new GameObject[0];
        private ArmatureData copiedData;
        private Vector2 scrollPosition;
        private bool copyTransforms = true;
        private bool copyTransformsScale = true;
        private bool copyTransformsPosition = false;
        private bool copyTransformsRotation = false;
        private bool copyMAComponents = true;
        private bool copyOtherComponents = false;

        private List<GameObject> _activeAvatars = new List<GameObject>();
        private int _selectedAvatarIndex = -1;

        // ─── Status ──────────────────────────────────────────────────────────

        private string _statusMessage   = "準備完了";
        private int    _statusLevel     = 0; // 0=info 1=success 2=warning 3=error
        private double _statusResetTime = -1.0;

        // ─── Window Registration ─────────────────────────────────────────────

        [MenuItem("dennokoworks/ArmatureScaleCopier_ver.dennoko/Single Copier")]
        public static void ShowWindow()
        {
            GetWindow<ArmatureScaleCopierWindow>("Single Copier");
        }

        // ─── Lifecycle ───────────────────────────────────────────────────────

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
            GUILayout.Label("Single Copier", ArmatureScaleCopierTheme.TitleStyle);
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

            // コピー元
            DrawSection("コピー元 (Source Armature)", () =>
            {
                sourceArmature = (GameObject)EditorGUILayout.ObjectField(sourceArmature, typeof(GameObject), true);
                if (sourceArmature != null && !IsValidArmature(sourceArmature))
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox("選択されたオブジェクトは「Armature」という名前ではありません。", MessageType.Warning);
                }
            });

            // コピー先
            DrawSection($"コピー先 (Target Armatures) — {targetArmatures.Length} 個", () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+ 追加", ArmatureScaleCopierTheme.MiniButtonLeftStyle))
                    System.Array.Resize(ref targetArmatures, targetArmatures.Length + 1);
                using (new EditorGUI.DisabledScope(targetArmatures.Length == 0))
                {
                    if (GUILayout.Button("- 削除", ArmatureScaleCopierTheme.MiniButtonRightStyle))
                        System.Array.Resize(ref targetArmatures, targetArmatures.Length - 1);
                }
                GUILayout.EndHorizontal();

                if (targetArmatures.Length > 0)
                    EditorGUILayout.Space(4);

                for (int i = 0; i < targetArmatures.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    targetArmatures[i] = (GameObject)EditorGUILayout.ObjectField($"Target {i + 1}", targetArmatures[i], typeof(GameObject), true);
                    if (GUILayout.Button("×", ArmatureScaleCopierTheme.MiniButtonStyle, GUILayout.Width(22)))
                    {
                        var newArray = new GameObject[targetArmatures.Length - 1];
                        for (int j = 0, k = 0; j < targetArmatures.Length; j++)
                            if (j != i) newArray[k++] = targetArmatures[j];
                        targetArmatures = newArray;
                        break;
                    }
                    GUILayout.EndHorizontal();

                    if (targetArmatures.Length > i && targetArmatures[i] != null && !IsValidArmature(targetArmatures[i]))
                        EditorGUILayout.HelpBox($"Target {i + 1}: 「Armature」という名前ではありません。", MessageType.Warning);
                }
            });

            // Transform コピーオプション
            DrawToggleSection("Transform 情報をコピー", ref copyTransforms, () =>
            {
                copyTransformsScale    = EditorGUILayout.Toggle("スケールをコピー", copyTransformsScale);
                copyTransformsPosition = EditorGUILayout.Toggle("位置をコピー",     copyTransformsPosition);
                copyTransformsRotation = EditorGUILayout.Toggle("回転をコピー",     copyTransformsRotation);
            });

            // コンポーネントオプション
            DrawSection("コンポーネント設定", () =>
            {
                copyMAComponents    = EditorGUILayout.Toggle("ModularAvatarコンポーネントをコピー", copyMAComponents);
                copyOtherComponents = EditorGUILayout.Toggle("その他のコンポーネントをコピー",     copyOtherComponents);
                if (copyOtherComponents)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox(
                        "その他のコンポーネントをコピーすると、不明瞭なエラーが発生して正しく動作しない可能性があります。\nこのオプションは慎重に使用してください。",
                        MessageType.Warning);
                }
            });

            // コピー済みデータ表示
            if (copiedData != null)
            {
                int totalObjects = CountTotalObjects(copiedData.childrenData);
                DrawSection($"コピー済みデータ — {totalObjects} オブジェクト", () =>
                {
                    DisplayChildrenDataRecursive(copiedData.childrenData, 0);
                });
            }

            GUILayout.EndVertical();
        }

        // ─── Footer ──────────────────────────────────────────────────────────

        private void DrawFooter()
        {
            GUILayout.BeginVertical(ArmatureScaleCopierTheme.CardStyle);
            DrawSeparator();

            bool canCopy = sourceArmature != null && IsValidArmature(sourceArmature);
            using (new EditorGUI.DisabledScope(!canCopy))
            {
                if (GUILayout.Button("Copy Armature Data", ArmatureScaleCopierTheme.ActionButtonStyle))
                    CopyArmatureData();
            }

            EditorGUILayout.Space(4);

            bool hasValidTargets = targetArmatures.Any(t => t != null && IsValidArmature(t));
            bool canPaste = hasValidTargets && copiedData != null;
            using (new EditorGUI.DisabledScope(!canPaste))
            {
                if (GUILayout.Button("Paste Armature Data", ArmatureScaleCopierTheme.SecondaryButtonStyle))
                    PasteArmatureData();
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

        private bool IsValidArmature(GameObject obj)
        {
            return ValidationHelper.IsValidArmature(obj);
        }

        private void CopyArmatureData()
        {
            if (sourceArmature == null) return;

            copiedData = new ArmatureData();
            foreach (Transform child in sourceArmature.transform)
                copiedData.childrenData.Add(CopyChildObjectDataRecursive(child));

            int totalObjectCount = CountTotalObjects(copiedData.childrenData);
            ArmatureScaleCopierLogger.Log($"Armature '{sourceArmature.name}' のデータをコピーしました。総オブジェクト数: {totalObjectCount}");
            SetStatus($"'{sourceArmature.name}' からコピーしました（{totalObjectCount} オブジェクト）。", 1);
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
                else if (copyOtherComponents && !IsModularAvatarComponent(component)) shouldCopy = true;

                if (shouldCopy)
                {
                    childData.componentData.Add(new ComponentData
                    {
                        typeName       = component.GetType().AssemblyQualifiedName,
                        serializedData = ArmatureScaleCopierLogger.TryExecute(() => JsonUtility.ToJson(component), "{}", "コンポーネントのシリアライズ")
                    });
                }
            }

            foreach (Transform child in transform)
                childData.childrenData.Add(CopyChildObjectDataRecursive(child));

            return childData;
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

        private void PasteArmatureData()
        {
            if (copiedData == null) return;

            var validTargets = targetArmatures.Where(t => t != null && IsValidArmature(t)).ToArray();
            if (validTargets.Length == 0)
            {
                Debug.LogWarning("[ArmatureScaleCopier] 有効なターゲットArmatureが見つかりません。");
                SetStatus("有効なターゲットが見つかりません。", 3);
                return;
            }

            foreach (var target in validTargets)
            {
                Undo.RegisterCompleteObjectUndo(target, "Paste Armature Data");
                foreach (var childData in copiedData.childrenData)
                    PasteChildObjectDataRecursive(childData, target.transform);
                ArmatureScaleCopierLogger.Log($"Armature '{target.name}' にデータをペーストしました。");
            }

            ArmatureScaleCopierLogger.Log($"合計 {validTargets.Length} 個のArmatureにデータをペーストしました。");
            SetStatus($"{validTargets.Length} 個のArmatureにペーストしました。", 1);
        }

        private void PasteChildObjectDataRecursive(ChildObjectData childData, Transform parentTransform)
        {
            Transform existingChild = parentTransform.Find(childData.name);
            if (existingChild == null) return;

            var targetChild = existingChild.gameObject;
            Undo.RegisterCompleteObjectUndo(targetChild, "Update Child Object");

            if (copyTransforms)
            {
                if (copyTransformsScale)    targetChild.transform.localScale    = childData.localScale;
                if (copyTransformsPosition) targetChild.transform.localPosition = childData.localPosition;
                if (copyTransformsRotation) targetChild.transform.localRotation = childData.localRotation;
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
                    Debug.LogWarning($"[ArmatureScaleCopier] コンポーネント {compData.typeName} の適用に失敗しました: {e.Message}");
                }
            }

            if (childData.childrenData != null)
            {
                foreach (var grandChild in childData.childrenData)
                {
                    if (grandChild != null)
                        PasteChildObjectDataRecursive(grandChild, targetChild.transform);
                }
            }
        }

        private void DisplayChildrenDataRecursive(List<ChildObjectData> childrenData, int indentLevel)
        {
            if (childrenData == null) return;
            string indent = new string(' ', indentLevel * 3);
            foreach (var childData in childrenData)
            {
                if (childData == null) continue;
                string compInfo = childData.componentData?.Count > 0
                    ? $"  [{childData.componentData.Count} comp]"
                    : "";
                GUILayout.Label($"{indent}• {childData.name}{compInfo}", ArmatureScaleCopierTheme.CaptionStyle);
                if (childData.childrenData != null && childData.childrenData.Count > 0)
                    DisplayChildrenDataRecursive(childData.childrenData, indentLevel + 1);
            }
        }

        private bool IsModularAvatarComponent(Component component)
        {
            return ModularAvatarHelper.IsModularAvatarComponent(component);
        }
    }

    // ─── Data Classes ────────────────────────────────────────────────────────

    [System.Serializable]
    public class ArmatureData
    {
        public List<ChildObjectData> childrenData = new List<ChildObjectData>();
    }

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

    [System.Serializable]
    public class ComponentData
    {
        public string typeName;
        public string serializedData;
    }
}
