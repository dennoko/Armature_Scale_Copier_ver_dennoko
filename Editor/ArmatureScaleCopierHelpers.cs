using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ShimotukiRieru.ArmatureScaleCopier
{
    /// <summary>
    /// ModularAvatarコンポーネントの検出と処理を行うヘルパークラス
    /// </summary>
    public static class ModularAvatarHelper
    {
        private static bool? _isMAAvailable;
        private static Type[] _maComponentTypes;

        /// <summary>
        /// ModularAvatarが利用可能かどうか
        /// </summary>
        public static bool IsModularAvatarAvailable
        {
            get
            {
                if (!_isMAAvailable.HasValue)
                {
                    CheckModularAvatarAvailability();
                }
                return _isMAAvailable.Value;
            }
        }

        /// <summary>
        /// ModularAvatarコンポーネントの型一覧
        /// </summary>
        public static Type[] MAComponentTypes
        {
            get
            {
                if (_maComponentTypes == null)
                {
                    GetMAComponentTypes();
                }
                return _maComponentTypes ?? new Type[0];
            }
        }

        /// <summary>
        /// ModularAvatarの利用可能性をチェック
        /// </summary>
        private static void CheckModularAvatarAvailability()
        {
            try
            {
                // nadena.dev.modular_avatar.core アセンブリの存在をチェック
                var assembly = Assembly.Load("nadena.dev.modular-avatar.core");
                _isMAAvailable = assembly != null;
            }
            catch
            {
                _isMAAvailable = false;
            }
        }

        /// <summary>
        /// ModularAvatarコンポーネントの型を取得
        /// </summary>
        private static void GetMAComponentTypes()
        {
            if (!IsModularAvatarAvailable)
            {
                _maComponentTypes = new Type[0];
                return;
            }

            try
            {
                var assembly = Assembly.Load("nadena.dev.modular-avatar.core");
                var types = assembly.GetTypes();
                var maTypes = new System.Collections.Generic.List<Type>();

                foreach (var type in types)
                {
                    if (typeof(Component).IsAssignableFrom(type) &&
                        type.Namespace?.StartsWith("nadena.dev.modular_avatar") == true)
                    {
                        maTypes.Add(type);
                    }
                }

                _maComponentTypes = maTypes.ToArray();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ModularAvatarコンポーネント型の取得に失敗しました: {e.Message}");
                _maComponentTypes = new Type[0];
            }
        }

        /// <summary>
        /// 指定されたコンポーネントがModularAvatarコンポーネントかどうかを判定
        /// </summary>
        public static bool IsModularAvatarComponent(Component component)
        {
            if (!IsModularAvatarAvailable || component == null)
                return false;

            var componentType = component.GetType();
            return componentType.Namespace?.StartsWith("nadena.dev.modular_avatar") == true;
        }

        /// <summary>
        /// コンポーネントの種類を取得
        /// </summary>
        public static ComponentCategory GetComponentCategory(Component component)
        {
            if (component == null)
                return ComponentCategory.Unknown;

            if (component is Transform)
                return ComponentCategory.Transform;

            if (IsModularAvatarComponent(component))
                return ComponentCategory.ModularAvatar;

            // 一般的なUnityコンポーネント
            var componentType = component.GetType();
            if (componentType.Namespace?.StartsWith("UnityEngine") == true)
                return ComponentCategory.Unity;

            // VRChatコンポーネント
            if (componentType.Namespace?.StartsWith("VRC") == true)
                return ComponentCategory.VRChat;

            return ComponentCategory.Other;
        }

        /// <summary>
        /// コンポーネントが安全にコピー可能かどうかを判定
        /// </summary>
        public static bool IsSafeToCopy(Component component)
        {
            if (component == null || component is Transform)
                return false;

            var category = GetComponentCategory(component);

            // ModularAvatarコンポーネントは通常安全
            if (category == ComponentCategory.ModularAvatar)
                return true;

            // 特定のUnityコンポーネントは避ける
            if (component is Renderer || component is Collider || component is Rigidbody)
                return false;

            return true;
        }
    }

    /// <summary>
    /// コンポーネントのカテゴリ
    /// </summary>
    public enum ComponentCategory
    {
        Unknown,
        Transform,
        ModularAvatar,
        Unity,
        VRChat,
        Other
    }

    /// <summary>
    /// エラーハンドリングとログ管理
    /// </summary>
    public static class ArmatureScaleCopierLogger
    {
        private const string LOG_PREFIX = "[Armature Scale Copier]";

        public static void Log(string message)
        {
            Debug.Log($"{LOG_PREFIX} {message}");
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{LOG_PREFIX} {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"{LOG_PREFIX} {message}");
        }

        public static void LogException(Exception exception, string context = "")
        {
            var message = string.IsNullOrEmpty(context)
                ? $"例外が発生しました: {exception.Message}"
                : $"{context}で例外が発生しました: {exception.Message}";

            Debug.LogError($"{LOG_PREFIX} {message}");
            Debug.LogException(exception);
        }

        public static bool TryExecute(System.Action action, string context = "")
        {
            try
            {
                action?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                LogException(e, context);
                return false;
            }
        }

        public static T TryExecute<T>(System.Func<T> func, T defaultValue = default(T), string context = "")
        {
            try
            {
                return func != null ? func() : defaultValue;
            }
            catch (Exception e)
            {
                LogException(e, context);
                return defaultValue;
            }
        }
    }

    /// <summary>
    /// バリデーションヘルパー
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Armatureオブジェクトかどうかを判定
        /// </summary>
        public static bool IsValidArmature(GameObject obj)
        {
            if (obj == null) return false;

            return obj.name.ToLower().Contains("armature");
        }

        /// <summary>
        /// オブジェクトが有効なコピー元として使用できるかを判定
        /// </summary>
        public static bool IsValidSource(GameObject obj)
        {
            if (!IsValidArmature(obj)) return false;

            // 子オブジェクトが存在するかチェック
            return obj.transform.childCount > 0;
        }

        /// <summary>
        /// オブジェクトが有効なコピー先として使用できるかを判定
        /// </summary>
        public static bool IsValidTarget(GameObject obj, GameObject source = null)
        {
            if (!IsValidArmature(obj)) return false;

            // 自分自身をターゲットにしていないかチェック
            if (source != null && obj == source) return false;

            return true;
        }

        /// <summary>
        /// コンポーネントの有効性をチェック
        /// </summary>
        public static bool IsValidComponent(Component component)
        {
            return component != null && !(component is Transform);
        }
    }

    /// <summary>
    /// VRChatアバターの検出を行うヘルパークラス（VRC SDK未導入環境でも動作）
    /// </summary>
    public static class VRCHelper
    {
        private static Type _vrcAvatarDescriptorType;
        private static bool _checked;

        public static bool IsVRCSdkAvailable
        {
            get
            {
                if (!_checked) CheckAvailability();
                return _vrcAvatarDescriptorType != null;
            }
        }

        private static void CheckAvailability()
        {
            _checked = true;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
                    if (type != null)
                    {
                        _vrcAvatarDescriptorType = type;
                        return;
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// シーン上でアクティブなVRChatアバターをすべて取得
        /// </summary>
        public static List<GameObject> FindActiveAvatars()
        {
            var result = new List<GameObject>();
            if (!IsVRCSdkAvailable) return result;

            var components = UnityEngine.Object.FindObjectsOfType(_vrcAvatarDescriptorType);
            foreach (var comp in components)
            {
                var go = (comp as Component)?.gameObject;
                if (go != null && go.activeInHierarchy && go.scene.isLoaded)
                    result.Add(go);
            }
            return result;
        }
    }
}
