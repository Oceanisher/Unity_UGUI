using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 测试鼠标进入与退出UI事件调用
/// 主要看切出去的时候，还能否调用到PointExit事件
/// 结论：重新Focus屏幕的时候是能够调用的
/// </summary>
public class PointPassTestMono : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("OnPointerEnter");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("OnPointerExit");
    }
}
