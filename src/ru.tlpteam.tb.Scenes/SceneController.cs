using System;
using ru.tlpteam.tb.Runtime.Engine;
using ru.tlpteam.Debug;

namespace ru.tlpteam.tb.Scenes
{
    public static class SceneController
    {
        // Делегат для смены сцены — движок подписывается на него
        public static Action<string>? OnSceneChangeRequest;

        /// <summary>
        /// Метод для смены сцены из юзер-скриптов
        /// </summary>
        /// <param name="sceneName">Имя сцены, которую нужно загрузить</param>
        public static void LoadScene(string sceneName)
        {
            if (OnSceneChangeRequest != null)
            {
                OnSceneChangeRequest.Invoke(sceneName);
            }
            else
            {
                throw new InvalidOperationException("Scene change handler not set in engine!");
            }
        }
    }
}
