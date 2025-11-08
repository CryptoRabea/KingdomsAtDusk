using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Core.Events;
using RTS.Core.Services;
using System.Collections.Generic;
using System.Linq;

namespace RTS.UI
{
    public class NotificationUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeDuration = 0.5f;

    private Queue<string> notificationQueue = new Queue<string>();
    private bool isDisplaying = false;

    private void OnEnable()
    {
        EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        EventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
        EventBus.Subscribe<ResourcesSpentEvent>(OnResourcesSpent);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        EventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        EventBus.Unsubscribe<ResourcesSpentEvent>(OnResourcesSpent);
    }

    private void OnBuildingCompleted(BuildingCompletedEvent evt)
    {
        ShowNotification($"{evt.BuildingType} completed!");
    }

    private void OnWaveStarted(WaveStartedEvent evt)
    {
        ShowNotification($"Wave {evt.WaveNumber} incoming! {evt.EnemyCount} enemies!");
    }

    private void OnUnitDied(UnitDiedEvent evt)
    {
        if (!evt.WasEnemy)
        {
            ShowNotification("Unit lost!");
        }
    }

    private void OnResourcesSpent(ResourcesSpentEvent evt)
    {
        if (!evt.Success)
        {
            ShowNotification("Not enough resources!");
        }
    }

    public void ShowNotification(string message)
    {
        notificationQueue.Enqueue(message);

        if (!isDisplaying)
        {
            StartCoroutine(DisplayQueuedNotifications());
        }
    }

    private System.Collections.IEnumerator DisplayQueuedNotifications()
    {
        isDisplaying = true;

        while (notificationQueue.Count > 0)
        {
            string message = notificationQueue.Dequeue();

            if (notificationText != null)
            {
                notificationText.text = message;
            }

            yield return StartCoroutine(DisplayCoroutine());
        }

        isDisplaying = false;
    }

    private System.Collections.IEnumerator DisplayCoroutine()
    {
        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        // Display
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
    }
}
}