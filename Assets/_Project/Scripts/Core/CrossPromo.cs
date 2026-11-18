using UnityEngine;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// The other Farm Fury games, advertised on the menu hub (2026-11-18, same design as Cannonfire's hub): plain store
    /// links behind the parental gate, no reward for installing (Apple 3.2.2 / Google Play forbid incentivised installs).
    /// Store ids: Cannonfire App Store 6834522614 (approved 2026-11-18), Arcade App Store 6804415300; Google Play
    /// packages com.farmfury.cannonfire / com.farmfury.arcade.
    /// Cannonfire is not on Google Play yet: <see cref="CannonfireOnGooglePlay"/> keeps its card off Android until it is.
    /// Google Play installs carry a referrer (utm_source=stampede) for Play Console's acquisition reports.
    /// Card art: Resources/CrossPromo/&lt;Game&gt;.png (each game's app icon on the shared wood rim).
    /// </summary>
    public static class CrossPromo
    {
        public enum Game { Cannonfire, Arcade }

        /// <summary>Flip to true once Farm Fury Cannonfire is live on Google Play.</summary>
        public const bool CannonfireOnGooglePlay = false;

        public static string AppStoreId(Game g) => g == Game.Cannonfire ? "6834522614" : "6804415300";
        public static string PlayPackage(Game g) => g == Game.Cannonfire ? "com.farmfury.cannonfire" : "com.farmfury.arcade";
        public static string PromoId(Game g) => g == Game.Cannonfire ? "xpromo_cannonfire" : "xpromo_arcade";
        public static string Title(Game g) => g == Game.Cannonfire ? "FARM FURY\nCANNONFIRE" : "FARM FURY\nARCADE";

        const string Referrer = "utm_source%3Dstampede%26utm_medium%3Dcross_promo%26utm_campaign%3Dmenu_hub";

        /// <summary>Whether this game's card should show on the current platform (it needs a store page to open).</summary>
        public static bool Available(Game g)
        {
#if UNITY_ANDROID
            return g != Game.Cannonfire || CannonfireOnGooglePlay;
#else
            return true;
#endif
        }

        /// <summary>The store page for this platform: the App Store app on iOS, the Play Store app on Android, the web
        /// page elsewhere (Editor / desktop).</summary>
        public static string StoreUrl(Game g)
        {
#if UNITY_IOS
            return $"itms-apps://apps.apple.com/app/id{AppStoreId(g)}";
#elif UNITY_ANDROID
            return $"market://details?id={PlayPackage(g)}&referrer={Referrer}";
#else
            return Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor
                ? $"https://apps.apple.com/app/id{AppStoreId(g)}"
                : $"https://play.google.com/store/apps/details?id={PlayPackage(g)}&referrer={Referrer}";
#endif
        }

        /// <summary>The card art, loaded as a texture so it works whatever the PNG's import type.</summary>
        public static Sprite Icon(Game g)
        {
            var tex = Resources.Load<Texture2D>($"CrossPromo/{g}");
            return tex == null ? null
                : Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Called once the parental gate is passed.</summary>
        public static void Open(Game g)
        {
            AnalyticsManager.Instance?.LogPromoTapped(PromoId(g));
            Application.OpenURL(StoreUrl(g));
        }
    }
}
