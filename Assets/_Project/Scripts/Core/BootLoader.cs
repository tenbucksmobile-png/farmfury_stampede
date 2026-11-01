using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// The only thing in Boot.unity, the first scene in the build: loads the game. Android's Split Application Binary
    /// keeps the first scene and all it references in the base module, limited to 200 MB on Google Play, and Game.unity
    /// references all the art. With this tiny scene first, the game's data goes into the install-time asset pack.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        public const string GameScene = "Game";

        private void Start() => SceneManager.LoadScene(GameScene);
    }
}
