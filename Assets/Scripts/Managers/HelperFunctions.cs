using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class HelperFunctions : MonoBehaviour
{
    private readonly HashSet<TaskCompletionSource<bool>> pendingCoroutines = new();

    public Task StartCoroutineAndWait(IEnumerator coroutine)
    {
        var completion = new TaskCompletionSource<bool>();
        pendingCoroutines.Add(completion);
        StartCoroutine(WrapCoroutine(coroutine, completion));
        return completion.Task;
    }

    private IEnumerator WrapCoroutine(IEnumerator coroutine, TaskCompletionSource<bool> completion)
    {
        yield return StartCoroutine(coroutine);
        pendingCoroutines.Remove(completion);
        completion.TrySetResult(true);
    }

    private void OnDestroy()
    {
        // A stopped/destroyed coroutine host must not leave async gameplay callers
        // polling forever. This also releases failed framework fixtures safely.
        var interrupted = new List<TaskCompletionSource<bool>>(pendingCoroutines);
        pendingCoroutines.Clear();
        foreach (var completion in interrupted) completion.TrySetCanceled();
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
