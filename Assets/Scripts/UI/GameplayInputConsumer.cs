using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameplayInputConsumer : MonoBehaviour
{
    private static readonly HashSet<GameplayInputConsumer> ActiveModalConsumers = new();

    [SerializeField] private bool blocksGameplayWhileActive;
    [SerializeField] private bool consumesPointerOverSelf = true;

    public bool BlocksGameplayWhileActive
    {
        get => blocksGameplayWhileActive;
        set => blocksGameplayWhileActive = value;
    }

    public bool ConsumesPointerOverSelf
    {
        get => consumesPointerOverSelf;
        set => consumesPointerOverSelf = value;
    }

    public static bool IsAnyModalConsumerActive
    {
        get
        {
            ActiveModalConsumers.RemoveWhere(consumer => consumer == null || !consumer.isActiveAndEnabled);
            return ActiveModalConsumers.Count > 0;
        }
    }

    public static GameplayInputConsumer FindConsumer(GameObject gameObject)
    {
        return gameObject != null
            ? gameObject.GetComponentInParent<GameplayInputConsumer>()
            : null;
    }

    public static GameplayInputConsumer Ensure(
        GameObject gameObject,
        bool blocksGameplayWhileActive,
        bool consumesPointerOverSelf = true)
    {
        if (gameObject == null)
        {
            return null;
        }

        GameplayInputConsumer consumer = gameObject.GetComponent<GameplayInputConsumer>()
            ?? gameObject.AddComponent<GameplayInputConsumer>();
        consumer.blocksGameplayWhileActive = blocksGameplayWhileActive;
        consumer.consumesPointerOverSelf = consumesPointerOverSelf;
        consumer.RefreshModalRegistration();
        return consumer;
    }

    private void OnEnable()
    {
        RefreshModalRegistration();
    }

    private void OnDisable()
    {
        ActiveModalConsumers.Remove(this);
    }

    private void OnValidate()
    {
        RefreshModalRegistration();
    }

    private void RefreshModalRegistration()
    {
        if (!isActiveAndEnabled)
        {
            ActiveModalConsumers.Remove(this);
            return;
        }

        if (blocksGameplayWhileActive)
        {
            ActiveModalConsumers.Add(this);
        }
        else
        {
            ActiveModalConsumers.Remove(this);
        }
    }
}
