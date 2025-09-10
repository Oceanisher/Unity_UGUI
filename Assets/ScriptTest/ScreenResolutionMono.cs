using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 直接修改显示器分辨率
/// </summary>
public class ScreenResolutionMono : MonoBehaviour
{
    [Header("宽")]
    public int Width;
    [Header("高")]
    public int Height;
    
    // Start is called before the first frame update
    void Start()
    {
        //设置分辨率
        Screen.SetResolution(Width, Height, Screen.fullScreen);
        //设置窗口模式
        Screen.fullScreenMode = FullScreenMode.Windowed;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
