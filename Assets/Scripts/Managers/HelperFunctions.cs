using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class HelperFunctions : MonoBehaviour
{
    public async Task StartCoroutineAndWait(IEnumerator coroutine)
    {
        bool isDone = false;
        StartCoroutine(WrapCoroutine(coroutine, () => isDone = true));
        // await Task.Run(() => { while (!isDone) {await Task.Yield();} }); // Wait until coroutine completes
        // Poll with delay to avoid blocking and allow coroutine to finish
        while (!isDone)
        {
            await Task.Yield(); // Non-blocking, allows other tasks to run
        }
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

    public string PrintListNamesOneLine<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
            return "(empty)";

        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            if (item == null)
            {
                builder.Append("(null)");
            }
            else
            {
                var nameProperty = item.GetType().GetProperty("name");
                if (nameProperty != null)
                {
                    var nameValue = nameProperty.GetValue(item);
                    builder.Append(nameValue != null ? nameValue.ToString() : "(unnamed)");
                }
                else
                {
                    builder.Append(item.ToString()); // fallback if no 'name' exists
                }
            }

            if (i < list.Count - 1)
                builder.Append(", ");
        }
        return builder.ToString();
    }
}