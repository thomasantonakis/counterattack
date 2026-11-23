using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CounterAttack.Online
{
    public sealed class OnlineModeMenu : MonoBehaviour
    {
        public const string SceneName = "OnlineModeMenu";
        public TMP_InputField lobbyCode;
        public TMP_Text status;

        private static string pendingJoinCode;

        public void Host()
        {
            if (SessionIsClosing()) return;
            OpenSetup(null);
        }

        public void Join()
        {
            if (SessionIsClosing()) return;
            if (!TryReadLobbyCode(lobbyCode.text, out var code))
            {
                status.text = "Enter the 32-character lobby code shared by the host.";
                lobbyCode.ActivateInputField();
                return;
            }

            OpenSetup(code);
        }

        public void Back() => SceneManager.LoadScene("MainMenu");

        public void Exit() => Application.Quit();

        public static bool TryReadLobbyCode(string input, out string code)
        {
            code = input?.Trim();
            return OnlineFrontendSessionPolicy.ValidLobbyCode(code);
        }

        public static string TakeJoinCode()
        {
            var code = pendingJoinCode;
            pendingJoinCode = null;
            return code;
        }

        private bool SessionIsClosing()
        {
            if (OnlineFrontendSession.Current == null) return false;
            status.text = "The previous session is closing. Try again in a moment.";
            return true;
        }

        private static void OpenSetup(string code)
        {
            ApplicationManager.EnsureInstanceExists();
            ApplicationManager.Instance.SetSelectedRoomGameMode(ApplicationManager.OnlineGameMode);
            pendingJoinCode = code;
            SceneManager.LoadScene("CreateNewGameScene");
        }
    }
}
