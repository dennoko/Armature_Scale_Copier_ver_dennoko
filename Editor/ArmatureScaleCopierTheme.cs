using UnityEngine;
using UnityEditor;

namespace ShimotukiRieru.ArmatureScaleCopier
{
    public static class ArmatureScaleCopierTheme
    {
        // ─── Colors ──────────────────────────────────────────────────────────

        public static readonly Color Surface0 = Hex(0x121212);
        public static readonly Color Surface1 = Hex(0x1e1e1e);
        public static readonly Color Surface2 = Hex(0x2c2c2c);

        public static readonly Color Outline = Hex(0x3a3a3a);

        public static readonly Color TextPrimary   = Hex(0xffffff);
        public static readonly Color TextSecondary = Hex(0xcccccc);
        public static readonly Color TextTertiary  = Hex(0xaaaaaa);
        public static readonly Color TextDisabled  = Hex(0x555555);

        public static readonly Color SemanticError   = Hex(0x9b1b30);
        public static readonly Color SemanticWarning = Hex(0xffb74d);
        public static readonly Color SemanticSuccess = Hex(0x4caf50);
        public static readonly Color SemanticInfo    = Hex(0x64b5f6);

        public static readonly Color Accent       = Color.white;
        public static readonly Color HoverOverlay = new Color(1f, 1f, 1f, 0.05f);

        // ─── Cached Textures ─────────────────────────────────────────────────

        private static Texture2D _texSurface0;
        private static Texture2D _texSurface1;
        private static Texture2D _texSurface2;
        private static Texture2D _texCard;
        private static Texture2D _texAccentCard;

        // ─── Styles ──────────────────────────────────────────────────────────

        private static bool _initialized;

        public static GUIStyle CardStyle      { get; private set; }
        public static GUIStyle CardOuterStyle { get; private set; }
        public static GUIStyle ToolbarStyle   { get; private set; }

        public static GUIStyle TitleStyle            { get; private set; }
        public static GUIStyle SectionHeaderStyle    { get; private set; }
        public static GUIStyle ToggleSectionOnStyle  { get; private set; }
        public static GUIStyle ToggleSectionOffStyle { get; private set; }
        public static GUIStyle SecondaryTextStyle    { get; private set; }
        public static GUIStyle CaptionStyle          { get; private set; }

        public static GUIStyle ActionButtonStyle    { get; private set; }
        public static GUIStyle SecondaryButtonStyle { get; private set; }
        public static GUIStyle MiniButtonStyle      { get; private set; }
        public static GUIStyle MiniButtonLeftStyle  { get; private set; }
        public static GUIStyle MiniButtonRightStyle { get; private set; }

        public static GUIStyle InspectorRootStyle { get; private set; }
        public static GUIStyle ToolbarButtonStyle { get; private set; }

        public static GUIStyle StatusInfoStyle    { get; private set; }
        public static GUIStyle StatusSuccessStyle { get; private set; }
        public static GUIStyle StatusWarningStyle { get; private set; }
        public static GUIStyle StatusErrorStyle   { get; private set; }

        // ─────────────────────────────────────────────────────────────────────

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            EnsureTextures();
            BuildStyles();
        }

        private static void EnsureTextures()
        {
            if (!_texSurface0)   _texSurface0   = MakeTex(Surface0);
            if (!_texSurface1)   _texSurface1   = MakeTex(Surface1);
            if (!_texSurface2)   _texSurface2   = MakeTex(Surface2);
            if (!_texCard)       _texCard       = MakeBorderedTex(Surface1, Outline);
            if (!_texAccentCard) _texAccentCard = MakeBorderedTex(Surface2, Outline);
        }

        private static void BuildStyles()
        {
            // ── Container ────────────────────────────────────────────────────

            CardStyle = new GUIStyle();
            CardStyle.normal.background = _texCard;
            CardStyle.border  = new RectOffset(1, 1, 1, 1);
            CardStyle.padding = new RectOffset(10, 10, 8, 8);
            CardStyle.margin  = new RectOffset(8, 8, 4, 4);

            CardOuterStyle = new GUIStyle();
            CardOuterStyle.normal.background = _texCard;
            CardOuterStyle.border  = new RectOffset(1, 1, 1, 1);
            CardOuterStyle.padding = new RectOffset(0, 0, 0, 0);
            CardOuterStyle.margin  = new RectOffset(8, 8, 4, 4);

            ToolbarStyle = new GUIStyle();
            ToolbarStyle.normal.background = _texSurface2;
            ToolbarStyle.padding = new RectOffset(6, 6, 4, 4);
            ToolbarStyle.margin  = new RectOffset(0, 0, 0, 0);

            // ── Typography ───────────────────────────────────────────────────

            TitleStyle = new GUIStyle();
            TitleStyle.fontStyle = FontStyle.Bold;
            TitleStyle.fontSize  = 14;
            TitleStyle.alignment = TextAnchor.MiddleLeft;
            FixAllTextColors(TitleStyle, TextPrimary);

            SectionHeaderStyle = new GUIStyle();
            SectionHeaderStyle.fontStyle = FontStyle.Bold;
            SectionHeaderStyle.fontSize  = 10;
            SectionHeaderStyle.margin    = new RectOffset(0, 0, 0, 2);
            FixAllTextColors(SectionHeaderStyle, TextTertiary);

            ToggleSectionOnStyle = new GUIStyle();
            ToggleSectionOnStyle.fontStyle = FontStyle.Bold;
            ToggleSectionOnStyle.fontSize  = 10;
            ToggleSectionOnStyle.margin    = new RectOffset(0, 0, 0, 2);
            FixAllTextColors(ToggleSectionOnStyle, TextPrimary);

            ToggleSectionOffStyle = new GUIStyle();
            ToggleSectionOffStyle.fontStyle = FontStyle.Bold;
            ToggleSectionOffStyle.fontSize  = 10;
            ToggleSectionOffStyle.margin    = new RectOffset(0, 0, 0, 2);
            FixAllTextColors(ToggleSectionOffStyle, TextTertiary);

            SecondaryTextStyle = new GUIStyle();
            SecondaryTextStyle.wordWrap = true;
            FixAllTextColors(SecondaryTextStyle, TextSecondary);

            CaptionStyle = new GUIStyle();
            CaptionStyle.fontSize = 9;
            FixAllTextColors(CaptionStyle, TextTertiary);

            // ── Toolbar Button ────────────────────────────────────────────────

            ToolbarButtonStyle = new GUIStyle();
            ToolbarButtonStyle.normal.background   = null;
            ToolbarButtonStyle.hover.background    = MakeTex(Color.Lerp(Surface2, Color.white, 0.10f));
            ToolbarButtonStyle.active.background   = MakeTex(Color.Lerp(Surface2, Color.white, 0.18f));
            ToolbarButtonStyle.border    = new RectOffset(0, 0, 0, 0);
            ToolbarButtonStyle.margin    = new RectOffset(1, 1, 1, 1);
            ToolbarButtonStyle.padding   = new RectOffset(6, 6, 2, 2);
            ToolbarButtonStyle.fontSize  = 10;
            ToolbarButtonStyle.alignment = TextAnchor.MiddleCenter;
            ToolbarButtonStyle.normal.textColor    = TextTertiary;
            ToolbarButtonStyle.hover.textColor     = TextSecondary;
            ToolbarButtonStyle.active.textColor    = TextPrimary;
            ToolbarButtonStyle.focused.textColor   = TextTertiary;
            ToolbarButtonStyle.onNormal.textColor  = TextPrimary;
            ToolbarButtonStyle.onHover.textColor   = TextPrimary;
            ToolbarButtonStyle.onActive.textColor  = TextPrimary;
            ToolbarButtonStyle.onFocused.textColor = TextPrimary;

            // ── Inspector Root ────────────────────────────────────────────────

            InspectorRootStyle = new GUIStyle();
            InspectorRootStyle.normal.background = _texSurface0;
            InspectorRootStyle.margin   = new RectOffset(0, 0, 0, 0);
            InspectorRootStyle.padding  = new RectOffset(10, 10, 8, 8);
            InspectorRootStyle.overflow = new RectOffset(20, 20, 0, 0);

            // ── Buttons ──────────────────────────────────────────────────────

            ActionButtonStyle = new GUIStyle();
            ActionButtonStyle.normal.background  = _texAccentCard;
            ActionButtonStyle.hover.background   = MakeTex(Color.Lerp(Surface2, Color.white, 0.07f));
            ActionButtonStyle.active.background  = MakeTex(Color.Lerp(Surface2, Color.white, 0.15f));
            ActionButtonStyle.border       = new RectOffset(1, 1, 1, 1);
            ActionButtonStyle.margin       = new RectOffset(4, 4, 2, 2);
            ActionButtonStyle.padding      = new RectOffset(6, 6, 3, 3);
            ActionButtonStyle.fontSize     = 13;
            ActionButtonStyle.fontStyle    = FontStyle.Bold;
            ActionButtonStyle.fixedHeight  = 34;
            ActionButtonStyle.alignment    = TextAnchor.MiddleCenter;
            ActionButtonStyle.stretchWidth = true;
            FixAllTextColors(ActionButtonStyle, TextPrimary);

            SecondaryButtonStyle = new GUIStyle();
            SecondaryButtonStyle.normal.background = MakeBorderedTex(Surface1, Outline);
            SecondaryButtonStyle.hover.background  = _texAccentCard;
            SecondaryButtonStyle.active.background = MakeTex(Color.Lerp(Surface1, Color.white, 0.10f));
            SecondaryButtonStyle.border       = new RectOffset(1, 1, 1, 1);
            SecondaryButtonStyle.margin       = new RectOffset(4, 4, 2, 2);
            SecondaryButtonStyle.padding      = new RectOffset(6, 6, 3, 3);
            SecondaryButtonStyle.fontSize     = 11;
            SecondaryButtonStyle.fixedHeight  = 26;
            SecondaryButtonStyle.alignment    = TextAnchor.MiddleCenter;
            SecondaryButtonStyle.stretchWidth = true;
            SecondaryButtonStyle.normal.textColor   = TextSecondary;
            SecondaryButtonStyle.hover.textColor    = TextPrimary;
            SecondaryButtonStyle.active.textColor   = TextPrimary;
            SecondaryButtonStyle.focused.textColor  = TextSecondary;
            SecondaryButtonStyle.onNormal.textColor  = TextSecondary;
            SecondaryButtonStyle.onHover.textColor   = TextPrimary;
            SecondaryButtonStyle.onActive.textColor  = TextPrimary;
            SecondaryButtonStyle.onFocused.textColor = TextSecondary;

            MiniButtonStyle = new GUIStyle();
            MiniButtonStyle.normal.background = _texAccentCard;
            MiniButtonStyle.normal.textColor  = TextTertiary;
            MiniButtonStyle.hover.background  = MakeTex(Color.Lerp(Surface2, Color.white, 0.10f));
            MiniButtonStyle.hover.textColor   = TextSecondary;
            MiniButtonStyle.active.background = MakeTex(Color.Lerp(Surface2, Color.white, 0.18f));
            MiniButtonStyle.active.textColor  = TextPrimary;
            MiniButtonStyle.border      = new RectOffset(1, 1, 1, 1);
            MiniButtonStyle.margin      = new RectOffset(2, 2, 1, 1);
            MiniButtonStyle.padding     = new RectOffset(4, 4, 1, 2);
            MiniButtonStyle.fontSize    = 10;
            MiniButtonStyle.fixedHeight = 18;
            MiniButtonStyle.alignment   = TextAnchor.MiddleCenter;
            MiniButtonStyle.focused.textColor = TextTertiary;
            MiniButtonStyle.onNormal.textColor  = TextPrimary;
            MiniButtonStyle.onHover.textColor   = TextPrimary;
            MiniButtonStyle.onActive.textColor  = TextPrimary;
            MiniButtonStyle.onFocused.textColor = TextPrimary;

            MiniButtonLeftStyle = new GUIStyle(MiniButtonStyle);
            MiniButtonRightStyle = new GUIStyle(MiniButtonStyle);

            // ── Status Bar ───────────────────────────────────────────────────

            var statusBase = new GUIStyle();
            statusBase.border    = new RectOffset(1, 1, 1, 1);
            statusBase.padding   = new RectOffset(8, 8, 5, 5);
            statusBase.margin    = new RectOffset(4, 4, 2, 2);
            statusBase.fontSize  = 11;
            statusBase.wordWrap  = true;
            statusBase.alignment = TextAnchor.MiddleLeft;

            StatusInfoStyle = new GUIStyle(statusBase);
            StatusInfoStyle.normal.background = _texSurface1;
            FixAllTextColors(StatusInfoStyle, TextSecondary);

            StatusSuccessStyle = new GUIStyle(statusBase);
            StatusSuccessStyle.normal.background = MakeTex(Color.Lerp(Surface1, SemanticSuccess, 0.3f));
            FixAllTextColors(StatusSuccessStyle, SemanticSuccess);

            StatusWarningStyle = new GUIStyle(statusBase);
            StatusWarningStyle.normal.background = MakeTex(Color.Lerp(Surface1, SemanticWarning, 0.25f));
            FixAllTextColors(StatusWarningStyle, SemanticWarning);

            StatusErrorStyle = new GUIStyle(statusBase);
            StatusErrorStyle.normal.background = MakeTex(Color.Lerp(Surface1, SemanticError, 0.5f));
            FixAllTextColors(StatusErrorStyle, new Color(1f, 0.65f, 0.65f));
        }

        // ─── Status Style Selector ───────────────────────────────────────────

        public static GUIStyle GetStatusStyle(int statusLevel)
        {
            return statusLevel switch
            {
                1 => StatusSuccessStyle,
                2 => StatusWarningStyle,
                3 => StatusErrorStyle,
                _ => StatusInfoStyle,
            };
        }

        // ─── Editor Style Override (Light Mode Fix) ──────────────────────────

        private static bool _overrideActive;

        private class GUIStyleBackup
        {
            private readonly GUIStyle _style;
            private readonly Color _normalColor, _hoverColor, _activeColor, _focusedColor;
            private readonly Color _onNormalColor, _onHoverColor, _onActiveColor, _onFocusedColor;
            private readonly Texture2D _normalBg, _hoverBg, _activeBg, _focusedBg;
            private readonly Texture2D _onNormalBg, _onHoverBg, _onActiveBg, _onFocusedBg;
            private readonly RectOffset _border;
            private readonly RectOffset _padding;

            public GUIStyleBackup(GUIStyle style)
            {
                _style = style;
                _normalColor   = style.normal.textColor;
                _hoverColor    = style.hover.textColor;
                _activeColor   = style.active.textColor;
                _focusedColor  = style.focused.textColor;
                _onNormalColor  = style.onNormal.textColor;
                _onHoverColor   = style.onHover.textColor;
                _onActiveColor  = style.onActive.textColor;
                _onFocusedColor = style.onFocused.textColor;

                _normalBg   = style.normal.background;
                _hoverBg    = style.hover.background;
                _activeBg   = style.active.background;
                _focusedBg  = style.focused.background;
                _onNormalBg  = style.onNormal.background;
                _onHoverBg   = style.onHover.background;
                _onActiveBg  = style.onActive.background;
                _onFocusedBg = style.onFocused.background;

                _border  = style.border;
                _padding = style.padding;
            }

            public void Restore()
            {
                _style.normal.textColor   = _normalColor;
                _style.hover.textColor    = _hoverColor;
                _style.active.textColor   = _activeColor;
                _style.focused.textColor  = _focusedColor;
                _style.onNormal.textColor  = _onNormalColor;
                _style.onHover.textColor   = _onHoverColor;
                _style.onActive.textColor  = _onActiveColor;
                _style.onFocused.textColor = _onFocusedColor;

                _style.normal.background   = _normalBg;
                _style.hover.background    = _hoverBg;
                _style.active.background   = _activeBg;
                _style.focused.background  = _focusedBg;
                _style.onNormal.background  = _onNormalBg;
                _style.onHover.background   = _onHoverBg;
                _style.onActive.background  = _onActiveBg;
                _style.onFocused.background = _onFocusedBg;

                _style.border  = _border;
                _style.padding = _padding;
            }
        }

        private static GUIStyleBackup[] _backups;

        public static void PushEditorTheme()
        {
            _overrideActive = true;

            if (_backups == null)
            {
                _backups = new[]
                {
                    new GUIStyleBackup(EditorStyles.label),
                    new GUIStyleBackup(EditorStyles.objectField),
                    new GUIStyleBackup(EditorStyles.numberField),
                    new GUIStyleBackup(EditorStyles.textField),
                    new GUIStyleBackup(EditorStyles.popup),
                    new GUIStyleBackup(EditorStyles.toggle)
                };
            }

            FixAllTextColors(EditorStyles.label,       TextSecondary);
            FixAllTextColors(EditorStyles.objectField, TextSecondary);
            FixAllTextColors(EditorStyles.numberField, TextSecondary);
            FixAllTextColors(EditorStyles.textField,   TextSecondary);
            FixAllTextColors(EditorStyles.popup,       TextSecondary);
            FixAllTextColors(EditorStyles.toggle,      TextSecondary);

            FixAllStateBackgrounds(EditorStyles.objectField, _texSurface1);
            FixAllStateBackgrounds(EditorStyles.numberField, _texSurface1);
            FixAllStateBackgrounds(EditorStyles.textField,   _texSurface1);

            FixAllStateBackgrounds(EditorStyles.popup, _texCard);
            EditorStyles.popup.border  = new RectOffset(1, 1, 1, 1);
            EditorStyles.popup.padding = new RectOffset(6, 18, 4, 4);
        }

        public static void PopEditorTheme()
        {
            if (!_overrideActive) return;
            _overrideActive = false;

            if (_backups != null)
            {
                foreach (var backup in _backups)
                    backup.Restore();
            }
        }

        private static void FixAllStateBackgrounds(GUIStyle style, Texture2D tex)
        {
            style.normal.background    = tex;
            style.hover.background     = tex;
            style.active.background    = tex;
            style.focused.background   = tex;
            style.onNormal.background  = tex;
            style.onHover.background   = tex;
            style.onActive.background  = tex;
            style.onFocused.background = tex;
        }

        private static void FixAllTextColors(GUIStyle style, Color color)
        {
            style.normal.textColor    = color;
            style.hover.textColor     = color;
            style.active.textColor    = color;
            style.focused.textColor   = color;
            style.onNormal.textColor  = color;
            style.onHover.textColor   = color;
            style.onActive.textColor  = color;
            style.onFocused.textColor = color;
        }

        // ─── Texture Utilities ───────────────────────────────────────────────

        private static Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        private static Texture2D MakeBorderedTex(Color fillColor, Color borderColor)
        {
            const int size = 3;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y,
                        (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                            ? borderColor : fillColor);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            tex.hideFlags  = HideFlags.HideAndDontSave;
            return tex;
        }

        private static Color Hex(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >>  8) & 0xFF) / 255f,
            ( rgb        & 0xFF) / 255f);
    }
}
