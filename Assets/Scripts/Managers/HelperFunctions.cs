using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Threading.Tasks;

public class HelperFunctions : MonoBehaviour
{
    public async Task StartCoroutineAndWait(IEnumerator coroutine)
    {
        bool isDone = false;
        StartCoroutine(WrapCoroutine(coroutine, () => isDone = true));
        await Task.Run(() => { while (!isDone) { } }); // Wait until coroutine completes
    }

    private IEnumerator WrapCoroutine(IEnumerator coroutine, System.Action onComplete)
    {
        yield return StartCoroutine(coroutine);
        onComplete?.Invoke();
    }

    public (int _roll, bool _isJackPot) DiceRoll()
    {
        bool isJackpot = false;
        int roll = Random.Range(1, 7);
        if (roll == 6)
        {
            int secondRoll = Random.Range(1, 3);
            isJackpot = secondRoll == 2;
        }
        return (roll, isJackpot);
    }
}