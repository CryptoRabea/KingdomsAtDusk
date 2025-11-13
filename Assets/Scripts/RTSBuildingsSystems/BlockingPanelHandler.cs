using UnityEngine;
using UnityEngine.EventSystems;

public class BlockingPanelHandler : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // This code will execute when the blocking panel is clicked.
        Debug.Log("Blocking Panel Clicked! Clicks are not passing through.");
        // You could add logic here to close a window, play a sound, etc.
    }
}