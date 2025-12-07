using UnityEngine;
using UnityEngine.EventSystems;

public class BlockingPanelHandler : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // This code will execute when the blocking panel is clicked.
        // You could add logic here to close a window, play a sound, etc.
    }
}