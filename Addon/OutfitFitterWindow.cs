using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ShimotukiRieru.ArmatureScaleCopier;

namespace ShimotukiRieru.ArmatureScaleCopier.Addon
{
    public class OutfitFitterWindow : EditorWindow
    {
        // ─── Fields ──────────────────────────────────────────────────────────

        private GameObject  _costumeRoot;
        private OutfitPreset _preset;
        private Vector2     _scrollPos;

        private List<GameObject> _activeAvatars     = new List<GameObject>();
        private int              _selectedAvatarIdx = -1;

        private bool _applyBones       = true;
        private bool _runSetupOutfit   = true;
        private bool _applyComponents  = true;
        private bool _applyBlendshapes = true;

        private bool _showBoneList = false;

        private HashSet<string> _skippedComponents  = new HashSet<string>();
        private HashSet<string> _skippedBlendshapes = new HashSet<string>();

        // ─── Status ──────────────────────────────────────────────────────────

        private string _statusMessage   = "準備完了";
        private int    _statusLevel     = 0;
        private double _statusResetTime = -1.0;

        // ─── Window Registration ─────────────────────────────────────────────

        [MenuItem("dennokoworks/ArmatureScaleCopier_ver.dennoko/Outfit Fitter")]
        public static void ShowWindow()
        {
            GetWindow<OutfitFitterWindow>("Outfit Fitter");
        }

        // ─── Lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()  => RefreshAvatars();
        private void OnFocus()   => RefreshAvatars();

        private void RefreshAvatars()
        {
            _activeAvatars = VRCHelper.FindActiveAvatars();
            if (_selectedAvatarIdx < 0 && _activeAvatars.Count > 0)
                _selectedAvatarIdx = 0;
            Repaint();
        }

        // ─── OnGUI ───────────────────────────────────────────────────────────

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
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                DrawContent();
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
            GUILayout.Label("Outfit Fitter", ArmatureScaleCopierTheme.TitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Space(6);
            GUILayout.EndHorizontal();
            EditorGUILayout.Space(6);
            DrawSeparator();
        }

        // ─── Content ─────────────────────────────────────────────────────────

        private void DrawContent()
        {
            GUILayout.BeginVertical();

            DrawAvatarSection();
            DrawCostumeSection();
            DrawPresetSection();

            if (_preset != null)
            {
                DrawBoneSection();
                DrawSetupOutfitSection();
                DrawComponentSection();
                DrawBlendshapeSection();
            }

            GUILayout.EndVertical();
        }

        // ─── Avatar Section ───────────────────────────────────────────────────

        private void DrawAvatarSection()
        {
            DrawSection("アバター自動認識", () =>
            {
                if (!VRCHelper.IsVRCSdkAvailable)
                {
                    EditorGUILayout.HelpBox("VRC SDK 未検出のため、アバターの自動認識は利用できません。", MessageType.Info);
                    return;
                }

                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("アクティブアバター");
                if (_activeAvatars.Count == 0)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.DropdownButton(new GUIContent("アバターが見つかりません"), FocusType.Passive, ArmatureScaleCopierTheme.MiniButtonStyle);
                    }
                }
                else
                {
                    var names   = _activeAvatars.Select(a => a.name).ToArray();
                    int clamped = Mathf.Clamp(_selectedAvatarIdx, 0, names.Length - 1);
                    string selectedName = names[clamped];
                    if (EditorGUILayout.DropdownButton(new GUIContent(selectedName + " ▼"), FocusType.Passive, ArmatureScaleCopierTheme.MiniButtonStyle))
                    {
                        GenericMenu menu = new GenericMenu();
                        for (int i = 0; i < names.Length; i++)
                        {
                            int index = i;
                            menu.AddItem(new GUIContent(names[i]), i == clamped, () =>
                            {
                                _selectedAvatarIdx = index;
                            });
                        }
                        menu.ShowAsContext();
                    }
                }
                if (GUILayout.Button("更新", ArmatureScaleCopierTheme.MiniButtonStyle, GUILayout.Width(50)))
                    RefreshAvatars();
                GUILayout.EndHorizontal();
            });
        }

        // ─── Costume Section ──────────────────────────────────────────────────

        private void DrawCostumeSection()
        {
            DrawSection("衣装の設定", () =>
            {
                _costumeRoot = (GameObject)EditorGUILayout.ObjectField("衣装オブジェクト", _costumeRoot, typeof(GameObject), true);

                if (_costumeRoot != null && GetCostumeArmature() == null)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox("「Armature」という名前の子オブジェクトが見つかりません。", MessageType.Warning);
                }
            });
        }

        // ─── Preset Section ───────────────────────────────────────────────────

        private void DrawPresetSection()
        {
            DrawSection("プリセット", () =>
            {
                EditorGUI.BeginChangeCheck();
                _preset = (OutfitPreset)EditorGUILayout.ObjectField(_preset, typeof(OutfitPreset), false);
                if (EditorGUI.EndChangeCheck())
                {
                    _skippedComponents.Clear();
                    _skippedBlendshapes.Clear();
                    if (_preset != null) _runSetupOutfit = _preset.runSetupOutfit;
                    Repaint();
                }

                EditorGUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("新規作成", ArmatureScaleCopierTheme.MiniButtonLeftStyle))
                    CreateNewPreset();
                using (new EditorGUI.DisabledScope(_preset == null))
                {
                    if (GUILayout.Button("Inspector で開く", ArmatureScaleCopierTheme.MiniButtonRightStyle))
                    {
                        Selection.activeObject = _preset;
                        EditorGUIUtility.PingObject(_preset);
                    }
                }
                GUILayout.EndHorizontal();

                if (_preset == null) return;

                EditorGUILayout.Space(6);
                DrawSeparator();

                GUILayout.Label(
                    $"ボーン調整: {_preset.boneAdjustments.Count} 件   " +
                    $"コンポーネント: {_preset.componentEntries.Count} 件   " +
                    $"Blendshape: {_preset.blendshapes.Count} 件",
                    ArmatureScaleCopierTheme.CaptionStyle);

                EditorGUILayout.Space(4);
                using (new EditorGUI.DisabledScope(GetCostumeArmature() == null))
                {
                    if (GUILayout.Button("現在のボーン状態を Preset に記録", ArmatureScaleCopierTheme.SecondaryButtonStyle))
                        RecordCurrentState();
                }
            });
        }

        // ─── Bone Section ─────────────────────────────────────────────────────

        private void DrawBoneSection()
        {
            DrawToggleSection("ボーン調整を適用", ref _applyBones, () =>
            {
                if (_preset.boneAdjustments.Count == 0)
                {
                    GUILayout.Label("プリセットにボーン調整データがありません。", ArmatureScaleCopierTheme.SecondaryTextStyle);
                    return;
                }

                if (GUILayout.Button(
                    _showBoneList ? "▲ 一覧を閉じる" : $"▼ ボーン一覧を表示 ({_preset.boneAdjustments.Count} 件)",
                    ArmatureScaleCopierTheme.MiniButtonStyle))
                {
                    _showBoneList = !_showBoneList;
                }

                if (_showBoneList)
                {
                    EditorGUILayout.Space(4);
                    foreach (var adj in _preset.boneAdjustments)
                        GUILayout.Label($"  • {adj.bonePath}", ArmatureScaleCopierTheme.CaptionStyle);
                }
            });
        }

        // ─── Setup Outfit Section ──────────────────────────────────────────────

        private void DrawSetupOutfitSection()
        {
            DrawToggleSection("Setup Outfit を実行", ref _runSetupOutfit, () =>
            {
                if (!OutfitFitterLogic.IsSetupOutfitAvailable())
                    EditorGUILayout.HelpBox("ModularAvatar が検出されていません。", MessageType.Info);
            });
        }

        // ─── Component Section ────────────────────────────────────────────────

        private void DrawComponentSection()
        {
            DrawToggleSection("コンポーネントを反映", ref _applyComponents, () =>
            {
                if (_preset.componentEntries.Count == 0)
                {
                    GUILayout.Label("プリセットにコンポーネントデータがありません。", ArmatureScaleCopierTheme.SecondaryTextStyle);
                    return;
                }

                var typeNames = OutfitFitterLogic.GetUniqueComponentTypeNames(_preset);
                foreach (var typeName in typeNames)
                {
                    bool isSkipped = _skippedComponents.Contains(typeName);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(OutfitFitterLogic.GetShortTypeName(typeName), ArmatureScaleCopierTheme.SecondaryTextStyle);
                    GUILayout.FlexibleSpace();
                    bool newSkip = EditorGUILayout.ToggleLeft("スキップ", isSkipped, GUILayout.Width(76));
                    GUILayout.EndHorizontal();

                    if (newSkip != isSkipped)
                    {
                        if (newSkip) _skippedComponents.Add(typeName);
                        else         _skippedComponents.Remove(typeName);
                    }
                }
            });
        }

        // ─── Blendshape Section ───────────────────────────────────────────────

        private void DrawBlendshapeSection()
        {
            DrawToggleSection("Blendshape を適用", ref _applyBlendshapes, () =>
            {
                if (_preset.blendshapes.Count == 0)
                {
                    GUILayout.Label("プリセットに Blendshape データがありません。", ArmatureScaleCopierTheme.SecondaryTextStyle);
                    return;
                }

                EditorGUILayout.HelpBox("対象メッシュに存在しない Blendshape は自動でスキップされます。", MessageType.Info);
                EditorGUILayout.Space(4);

                foreach (var entry in _preset.blendshapes)
                {
                    bool isSkipped = _skippedBlendshapes.Contains(entry.blendshapeName);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{entry.blendshapeName}: {entry.value:F0}", ArmatureScaleCopierTheme.SecondaryTextStyle);
                    GUILayout.FlexibleSpace();
                    bool newSkip = EditorGUILayout.ToggleLeft("スキップ", isSkipped, GUILayout.Width(76));
                    GUILayout.EndHorizontal();

                    if (newSkip != isSkipped)
                    {
                        if (newSkip) _skippedBlendshapes.Add(entry.blendshapeName);
                        else         _skippedBlendshapes.Remove(entry.blendshapeName);
                    }
                }
            });
        }

        // ─── Footer ──────────────────────────────────────────────────────────

        private void DrawFooter()
        {
            GUILayout.BeginVertical(ArmatureScaleCopierTheme.CardStyle);
            DrawSeparator();

            bool canExecute = _costumeRoot != null && _preset != null && GetCostumeArmature() != null;
            using (new EditorGUI.DisabledScope(!canExecute))
            {
                if (GUILayout.Button("適用を実行", ArmatureScaleCopierTheme.ActionButtonStyle))
                    ExecuteApply();
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
            bool next = EditorGUILayout.ToggleLeft(title, toggle, headerStyle, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck()) { toggle = next; Repaint(); }
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

        private void SetStatus(string message, int level, double autoReset = 4.0)
        {
            _statusMessage   = message;
            _statusLevel     = level;
            _statusResetTime = level == 0 ? -1.0 : EditorApplication.timeSinceStartup + autoReset;
            Repaint();
        }

        // ─── Logic Helpers ────────────────────────────────────────────────────

        private Transform GetCostumeArmature()
        {
            if (_costumeRoot == null) return null;
            return _costumeRoot.transform.Find("Armature");
        }

        private void CreateNewPreset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Outfit Preset を保存", "OutfitPreset", "asset",
                "プリセットの保存先を選択してください");
            if (string.IsNullOrEmpty(path)) return;

            var asset = ScriptableObject.CreateInstance<OutfitPreset>();
            asset.runSetupOutfit = true;

            var armature = GetCostumeArmature();
            if (armature != null)
            {
                asset.boneAdjustments  = OutfitFitterLogic.RecordBoneState(armature);
                asset.componentEntries = OutfitFitterLogic.CollectComponentEntries(armature, maOnly: true);
            }

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _preset = asset;
            _skippedComponents.Clear();
            _skippedBlendshapes.Clear();
            _runSetupOutfit = asset.runSetupOutfit;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            string detail = armature != null
                ? $"ボーン {asset.boneAdjustments.Count} 件を記録しました。"
                : "衣装未指定のため空のプリセットを作成しました。";
            SetStatus($"プリセットを作成しました。{detail} Inspector で Blendshape などを追記できます。", 1, 6.0);
        }

        private void RecordCurrentState()
        {
            if (_preset == null) return;
            var armature = GetCostumeArmature();
            if (armature == null) return;

            Undo.RecordObject(_preset, "Record Outfit Bone State");
            _preset.boneAdjustments  = OutfitFitterLogic.RecordBoneState(armature);
            _preset.componentEntries = OutfitFitterLogic.CollectComponentEntries(armature, maOnly: true);
            EditorUtility.SetDirty(_preset);
            AssetDatabase.SaveAssets();

            SetStatus($"ボーン状態を記録しました（{_preset.boneAdjustments.Count} 件）。", 1);
        }

        private void ExecuteApply()
        {
            if (_preset == null || _costumeRoot == null) return;
            var armature = GetCostumeArmature();
            if (armature == null) return;

            var log = new System.Text.StringBuilder("適用完了:");

            if (_applyBones)
            {
                OutfitFitterLogic.ApplyBoneAdjustments(_preset, armature);
                log.Append($" ボーン({_preset.boneAdjustments.Count})");
            }

            if (_applyComponents)
            {
                OutfitFitterLogic.ApplyComponents(_preset, armature, _skippedComponents);
                int count = _preset.componentEntries.Count - _skippedComponents.Count;
                log.Append($" コンポーネント({System.Math.Max(0, count)})");
            }

            if (_runSetupOutfit && OutfitFitterLogic.IsSetupOutfitAvailable())
            {
                bool ok = OutfitFitterLogic.RunSetupOutfit(_costumeRoot);
                log.Append(ok ? " SetupOutfit(✓)" : " SetupOutfit(失敗)");
            }

            if (_applyBlendshapes)
            {
                int n = OutfitFitterLogic.ApplyBlendshapes(_preset, _costumeRoot, _skippedBlendshapes);
                log.Append($" Blendshape({n})");
            }

            SetStatus(log.ToString(), 1);
        }
    }
}
