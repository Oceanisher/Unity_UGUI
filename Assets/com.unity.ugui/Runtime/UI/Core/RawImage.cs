using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    /// <summary>
    /// Displays a Texture2D for the UI System.
    /// 显示原始纹理的图形组件，而不是只能显示Sprite精灵
    /// 如果不想生成图集Atlas、不想使用精灵，那么可以用该组件
    /// 每个RawImage组件都会带来一次额外的DC，所以需要限制使用，最好是只用在大背景、或者临时可见的内容上
    /// </summary>
    /// <remarks>
    /// If you don't have or don't wish to create an atlas, you can simply use this script to draw a texture.
    /// Keep in mind though that this will create an extra draw call with each RawImage present, so it's
    /// best to use it only for backgrounds or temporary visible graphics.
    /// </remarks>

    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Raw Image", 12)]
    public class RawImage : MaskableGraphic
    {
        //使用的纹理
        [FormerlySerializedAs("m_Tex")]
        [SerializeField] Texture m_Texture;
        //对纹理使用的UV区域
        //注意，假如x=0.5,width=1，那么此时UV坐标的xMin=0.5，xMax=1.5
        //此时UV坐标超出纹理区域了，就要看纹理自己的环绕方式。
        //假如纹理的环绕方式
        [SerializeField] Rect m_UVRect = new Rect(0f, 0f, 1f, 1f);

        protected RawImage()
        {
            useLegacyMeshGeneration = false;
        }

        /// <summary>
        /// Returns the texture used to draw this Graphic.
        /// 使用的主纹理
        /// 有纹理优先使用纹理，没有的话从材质里找，材质里也没有那就返回默认的白色纹理
        /// </summary>
        public override Texture mainTexture
        {
            get
            {
                if (m_Texture == null)
                {
                    if (material != null && material.mainTexture != null)
                    {
                        return material.mainTexture;
                    }
                    return s_WhiteTexture;
                }

                return m_Texture;
            }
        }

        /// <summary>
        /// The RawImage's texture to be used.
        /// 设置纹理
        /// 每次变更都会引起布局与图形重建
        /// </summary>
        /// <remarks>
        /// Use this to alter or return the Texture the RawImage displays. The Raw Image can display any Texture whereas an Image component can only show a Sprite Texture.
        /// Note : Keep in mind that using a RawImage creates an extra draw call with each RawImage present, so it's best to use it only for backgrounds or temporary visible graphics.Note: Keep in mind that using a RawImage creates an extra draw call with each RawImage present, so it's best to use it only for backgrounds or temporary visible graphics.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// //Create a new RawImage by going to Create>UI>Raw Image in the hierarchy.
        /// //Attach this script to the RawImage GameObject.
        ///
        /// using UnityEngine;
        /// using UnityEngine.UI;
        ///
        /// public class RawImageTexture : MonoBehaviour
        /// {
        ///     RawImage m_RawImage;
        ///     //Select a Texture in the Inspector to change to
        ///     public Texture m_Texture;
        ///
        ///     void Start()
        ///     {
        ///         //Fetch the RawImage component from the GameObject
        ///         m_RawImage = GetComponent<RawImage>();
        ///         //Change the Texture to be the one you define in the Inspector
        ///         m_RawImage.texture = m_Texture;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Texture texture
        {
            get
            {
                return m_Texture;
            }
            set
            {
                if (m_Texture == value)
                    return;

                m_Texture = value;
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        /// <summary>
        /// UV rectangle used by the texture.
        /// 设置纹理的UV
        /// 会造成图形重建
        /// </summary>
        public Rect uvRect
        {
            get
            {
                return m_UVRect;
            }
            set
            {
                if (m_UVRect == value)
                    return;
                m_UVRect = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Adjust the scale of the Graphic to make it pixel-perfect.
        /// 调整Trs大小，使其与纹理一致
        /// 实际上是纹理的尺寸 * uvRect的大小
        /// </summary>
        /// <remarks>
        /// This means setting the RawImage's RectTransform.sizeDelta  to be equal to the Texture dimensions.
        /// </remarks>
        public override void SetNativeSize()
        {
            Texture tex = mainTexture;
            if (tex != null)
            {
                int w = Mathf.RoundToInt(tex.width * uvRect.width);
                int h = Mathf.RoundToInt(tex.height * uvRect.height);
                rectTransform.anchorMax = rectTransform.anchorMin;
                rectTransform.sizeDelta = new Vector2(w, h);
            }
        }

        /// <summary>
        /// 生成Mesh
        /// </summary>
        /// <param name="vh"></param>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            Texture tex = mainTexture;
            vh.Clear();
            if (tex != null)
            {
                //获取像素对齐后的Trs区域
                var r = GetPixelAdjustedRect();
                //绘制区域的左下右上坐标
                var v = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);
                //这里是纹理的长宽乘以纹素大小，正好都等于1
                //纹素大小 texelSize.x = 1/tex.width ，所以这里scaleX是等于1
                //DeepSeek对于此处多此一举的解释是防御性编程，或者为了应对未来乘积不是1的情况
                var scaleX = tex.width * tex.texelSize.x;
                var scaleY = tex.height * tex.texelSize.y;
                {
                    var color32 = color;
                    vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(m_UVRect.xMin * scaleX, m_UVRect.yMin * scaleY));
                    vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(m_UVRect.xMin * scaleX, m_UVRect.yMax * scaleY));
                    vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(m_UVRect.xMax * scaleX, m_UVRect.yMax * scaleY));
                    vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(m_UVRect.xMax * scaleX, m_UVRect.yMin * scaleY));

                    vh.AddTriangle(0, 1, 2);
                    vh.AddTriangle(2, 3, 0);
                }
            }
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetMaterialDirty();
            SetVerticesDirty();
            SetRaycastDirty();
        }
    }
}
