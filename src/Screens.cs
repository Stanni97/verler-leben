using System;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace VerlerLeben
{
    /// <summary>Momo on the main menu. Only the own game, no server involved.</summary>
    internal static class Screens
    {
        internal static ManualLogSource Log;
        internal static bool Menu = true;

        private static Texture2D _texture;
        private static Sprite _sprite;

        internal static void Bind(ConfigFile config, ManualLogSource log)
        {
            Log = log;
            Menu = config.Bind("Screens", "MenuBackground", true,
                "Show the picture of Momo on the main menu. Only your own game, needs a restart.").Value;
        }

        internal static Texture2D Texture()
        {
            if (_texture != null)
            {
                return _texture;
            }

            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("VerlerLeben.momo_screen.jpg"))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    var bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    var texture = new Texture2D(2, 2, TextureFormat.RGB24, false) { name = "DF_MomoScreen" };
                    if (!ImageConversion.LoadImage(texture, bytes, true))
                    {
                        return null;
                    }

                    texture.wrapMode = TextureWrapMode.Clamp;
                    _texture = texture;
                }
            }
            catch (Exception e)
            {
                Log?.LogWarning("Could not load the screen picture: " + e.Message);
            }

            return _texture;
        }

        internal static Sprite Sprite()
        {
            Texture2D texture = Texture();
            if (_sprite == null && texture != null)
            {
                _sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }

            return _sprite;
        }

        /// <summary>Makes the picture cover its parent without stretching; what does not fit is cut off at the edges.</summary>
        internal static void Fit(GameObject target, Texture2D texture, AspectRatioFitter.AspectMode mode)
        {
            var fitter = target.GetComponent<AspectRatioFitter>() ?? target.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = mode;
            fitter.aspectRatio = texture.width / (float)texture.height;
        }
    }

    /// <summary>Keeps the picture on the main menu only: it shows while the main menu panel is open.</summary>
    internal class MenuPicture : MonoBehaviour
    {
        internal GameObject Menu;
        internal GameObject Picture;

        private void Update()
        {
            bool show = Menu != null && Menu.activeInHierarchy;
            if (Picture != null && Picture.activeSelf != show)
            {
                Picture.SetActive(show);
            }
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "Awake")]
    internal static class MenuBackground
    {
        [HarmonyPostfix]
        private static void Postfix(FejdStartup __instance)
        {
            try
            {
                Texture2D texture = Screens.Menu ? Screens.Texture() : null;
                if (texture == null || __instance.m_mainMenu == null)
                {
                    return;
                }

                Transform canvas = __instance.m_mainMenu.transform.parent;
                var holder = new GameObject("DF_MomoMenu", typeof(RectTransform));
                holder.transform.SetParent(canvas, false);
                holder.transform.SetAsFirstSibling();
                var rect = (RectTransform)holder.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;

                var picture = new GameObject("Picture", typeof(RectTransform), typeof(RawImage));
                picture.transform.SetParent(holder.transform, false);
                var raw = picture.GetComponent<RawImage>();
                raw.texture = texture;
                raw.raycastTarget = false;
                Screens.Fit(picture, texture, AspectRatioFitter.AspectMode.EnvelopeParent);

                var follower = holder.AddComponent<MenuPicture>();
                follower.Menu = __instance.m_mainMenu;
                follower.Picture = picture;
            }
            catch (Exception e)
            {
                Screens.Log?.LogWarning("Could not put Momo on the main menu: " + e.Message);
            }
        }
    }
}
