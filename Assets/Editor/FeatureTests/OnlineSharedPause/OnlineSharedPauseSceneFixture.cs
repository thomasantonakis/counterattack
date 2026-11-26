using System.Collections;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class OnlineSharedPauseSceneFixture
{
    public IEnumerator Run()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineGroundPassSceneAuthoring.ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        var board = Object.FindFirstObjectByType<OnlineGroundPassBoard>();
        Assert.That(board.sharedPausePanel, Is.Not.Null, "pause panel is authored in edit mode");
        Assert.That(board.authorityRoot.activeSelf, Is.False);
        var state = new OnlineGroundPassState { isSharedPaused = true, pauseRequester = "Home", sharedPauseRevision = 1,
            tokens = new OnlineGroundPassTokenState[0], legalActions = new string[0] };
        var scale = Time.timeScale;
        board.localTeam = OnlineTeam.Home; board.Display(state, "Requester");
        Assert.That(board.sharedPausePanel.activeSelf && board.requesterPauseMessage.activeSelf, Is.True);
        Assert.That(board.passivePauseMessage.activeSelf, Is.False);
        board.localTeam = OnlineTeam.Away; board.Display(state, "Opponent");
        Assert.That(board.sharedPausePanel.activeSelf && board.passivePauseMessage.activeSelf, Is.True);
        Assert.That(board.requesterPauseMessage.activeSelf, Is.False);
        Assert.That(board.passivePauseMessage.GetComponentsInChildren<Button>(true), Is.Empty);
        board.localStatsPanel.SetActive(false);
        Assert.That(board.sharedPausePanel.activeInHierarchy, Is.True, "personal stats visibility cannot hide shared pause");
        Assert.That(Time.timeScale, Is.EqualTo(scale), "guest presentation does not own gameplay time");
        state.isSharedPaused = false; state.pauseRequester = null; state.sharedPauseRevision = 2;
        board.Display(state, "Resumed");
        Assert.That(board.sharedPausePanel.activeSelf, Is.False);
        Assert.That(board.authorityRoot.activeSelf, Is.False);
    }
}
