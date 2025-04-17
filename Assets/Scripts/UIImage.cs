using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UIImage : MaskableGraphic, ILayoutElement
{
    private float m_CachedReferencePixelsPerUnit = 100f;

    [SerializeField]
    private Color32 m_OutlineColor = Color.black;
    [SerializeField]
    private Color32 m_ShadowColor = Color.black;

    [SerializeField]
    private float m_OutlineWidth = 0;

    [SerializeField]
    private float m_CornerRadius = 0;
    [SerializeField]
    private Vector2 m_ShadowOffset = Vector2.zero;
    [SerializeField]
    private float m_ShadowSpread = 0;

    [SerializeField]
    private bool m_Fill = true;
    [SerializeField]
    private bool m_Outline = false;
    [SerializeField]
    private bool m_Shadow = false;

    public bool fill
    {
        get
        {
            return m_Fill;
        }
        set
        {
            if (m_Fill != value)
            {
                m_Fill = value;
                SetVerticesDirty();
            }
        }
    }

    public bool outline
    {
        get
        {
            return m_Outline;
        }
        set
        {
            if (m_Outline != value)
            {
                m_Outline = value;
                SetVerticesDirty();
            }
        }
    }

    public bool shadow
    {
        get
        {
            return m_Shadow;
        }
        set
        {
            if (m_Shadow != value)
            {
                m_Shadow = value;
                SetVerticesDirty();
            }
        }
    }

    public Color outlineColor
    {
        get
        {
            return m_OutlineColor;
        }
        set
        {
            if (m_OutlineColor != value)
            {
                m_OutlineColor = value;
                SetVerticesDirty();
            }
        }
    }

    public Color shadowColor
    {
        get
        {
            return m_ShadowColor;
        }
        set
        {
            if (m_ShadowColor != value)
            {
                m_ShadowColor = value;
                SetVerticesDirty();
            }
        }
    }

    public float outlineWidth
    {
        get
        {
            return m_OutlineWidth;
        }
        set
        {
            if (m_OutlineWidth != value)
            {
                m_OutlineWidth = value;
                SetVerticesDirty();
            }
        }
    }

    public float cornerRadius
    {
        get
        {
            return m_CornerRadius;
        }
        set
        {
            if (m_CornerRadius != value)
            {
                m_CornerRadius = value;
                SetVerticesDirty();
            }
        }
    }

    public Vector2 shadowOffset
    {
        get
        {
            return m_ShadowOffset;
        }
        set
        {
            if (m_ShadowOffset != value)
            {
                m_ShadowOffset = value;
                SetVerticesDirty();
            }
        }
    }

    public float shadowSpread
    {
        get
        {
            return m_ShadowSpread;
        }
        set
        {
            if (m_ShadowSpread != value)
            {
                m_ShadowSpread = value;
                SetVerticesDirty();
            }
        }
    }


    public override Texture mainTexture
    {
        get
        {
            if (material != null && material.mainTexture != null)
            {
                return material.mainTexture;
            }

            return s_WhiteTexture;
        }
    }

    public float pixelsPerUnit
    {
        get
        {
            float num = 100f;

            if ((bool)canvas)
            {
                m_CachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;
            }

            return num / m_CachedReferencePixelsPerUnit;
        }
    }

    public override Material material
    {
        get
        {
            if (m_Material != null)
            {
                return m_Material;
            }

            return defaultMaterial;
        }
        set
        {
            base.material = value;
        }
    }

    public virtual float minWidth => 0f;

    public virtual float preferredWidth => 0f;

    public virtual float flexibleWidth => -1f;

    public virtual float minHeight => 0f;

    public virtual float preferredHeight => 0f;

    public virtual float flexibleHeight => -1f;

    public virtual int layoutPriority => 0;

    public void DisableSpriteOptimizations()
    {
        m_SkipLayoutUpdate = false;
        m_SkipMaterialUpdate = false;
    }

    protected UIImage()
    {
        useLegacyMeshGeneration = false;
    }

    public virtual void OnBeforeSerialize()
    {
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        SetVerticesDirty();
    }
#endif

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        GenerateSimpleSprite(toFill);
    }

    protected override void UpdateMaterial()
    {
        base.UpdateMaterial();

        canvasRenderer.SetAlphaTexture(null);
        return;
    }

    protected override void OnCanvasHierarchyChanged()
    {
        base.OnCanvasHierarchyChanged();
        if (canvas == null)
        {
            m_CachedReferencePixelsPerUnit = 100f;
        }
        else if (canvas.referencePixelsPerUnit != m_CachedReferencePixelsPerUnit)
        {
            m_CachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;
        }
    }

    private static readonly Vector4 s_DefaultTangent = new Vector4(1f, 0f, 0f, -1f);
    private static readonly Vector3 s_DefaultNormal = Vector3.back;

    private void GenerateSimpleSprite(VertexHelper vh)
    {
        Rect pixelAdjustedRect = GetPixelAdjustedRect();
        Vector4 vector = new(pixelAdjustedRect.x - pixelAdjustedRect.width * 0.5f, pixelAdjustedRect.y - pixelAdjustedRect.height * 0.5f, pixelAdjustedRect.x + pixelAdjustedRect.width * 1.5f, pixelAdjustedRect.y + pixelAdjustedRect.height * 1.5f);
        Color32 color = this.color;
        Color32 outlineColor = this.outlineColor;
        Color32 shadowColor = this.shadowColor;
        var outlineColorVec = new Vector4(outlineColor.r, outlineColor.g, outlineColor.b, outlineColor.a) / 255f;
        var shadowOffset = (Vector3)this.shadowOffset;
        var outlineWidth = Mathf.Min(m_OutlineWidth, pixelAdjustedRect.width * 0.5f, pixelAdjustedRect.height * 0.5f);
        var totalSpread = shadowSpread + (outline ? outlineWidth : 0);

        var additionalInfoVec = new Vector4(outlineWidth, m_CornerRadius, fill ? 1 : 0, outline ? 1 : 0);
        var shadowCornerRadius = m_CornerRadius + (outline ? outlineWidth * 0.75f : 0);
        var shadowAdditionalInfoVec = new Vector4(outlineWidth, shadowCornerRadius, 1, 0);

        vh.Clear();
        if (shadow)
        {
            vh.AddVert(new Vector3(vector.x, vector.y) + shadowOffset + 2 * new Vector3(-totalSpread, -totalSpread), shadowColor, new Vector4(0f, 0f, pixelAdjustedRect.width + totalSpread, pixelAdjustedRect.height + totalSpread), Vector4.zero, Vector4.zero, shadowAdditionalInfoVec, s_DefaultNormal, s_DefaultTangent);
            vh.AddVert(new Vector3(vector.x, vector.w) + shadowOffset + 2 * new Vector3(-totalSpread, totalSpread), shadowColor, new Vector4(0f, 1f, pixelAdjustedRect.width + totalSpread, pixelAdjustedRect.height + totalSpread), Vector4.zero, Vector4.zero, shadowAdditionalInfoVec, s_DefaultNormal, s_DefaultTangent);
            vh.AddVert(new Vector3(vector.z, vector.w) + shadowOffset + 2 * new Vector3(totalSpread, totalSpread), shadowColor, new Vector4(1f, 1f, pixelAdjustedRect.width + totalSpread, pixelAdjustedRect.height + totalSpread), Vector4.zero, Vector4.zero, shadowAdditionalInfoVec, s_DefaultNormal, s_DefaultTangent);
            vh.AddVert(new Vector3(vector.z, vector.y) + shadowOffset + 2 * new Vector3(totalSpread, -totalSpread), shadowColor, new Vector4(1f, 0f, pixelAdjustedRect.width + totalSpread, pixelAdjustedRect.height + totalSpread), Vector4.zero, Vector4.zero, shadowAdditionalInfoVec, s_DefaultNormal, s_DefaultTangent);
        }

        vh.AddVert(new Vector3(vector.x, vector.y), color, new Vector4(0f, 0f, pixelAdjustedRect.width, pixelAdjustedRect.height), Vector4.zero, outlineColorVec, additionalInfoVec, s_DefaultNormal, s_DefaultTangent);
        vh.AddVert(new Vector3(vector.x, vector.w), color, new Vector4(0f, 1f, pixelAdjustedRect.width, pixelAdjustedRect.height), Vector4.zero, outlineColorVec, additionalInfoVec, s_DefaultNormal, s_DefaultTangent);
        vh.AddVert(new Vector3(vector.z, vector.w), color, new Vector4(1f, 1f, pixelAdjustedRect.width, pixelAdjustedRect.height), Vector4.zero, outlineColorVec, additionalInfoVec, s_DefaultNormal, s_DefaultTangent);
        vh.AddVert(new Vector3(vector.z, vector.y), color, new Vector4(1f, 0f, pixelAdjustedRect.width, pixelAdjustedRect.height), Vector4.zero, outlineColorVec, additionalInfoVec, s_DefaultNormal, s_DefaultTangent);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);

        if (shadow)
        {
            vh.AddTriangle(4, 5, 6);
            vh.AddTriangle(6, 7, 4);
        }
    }

    protected override void OnDidApplyAnimationProperties()
    {
        SetMaterialDirty();
        SetVerticesDirty();
        SetRaycastDirty();
    }

    public void CalculateLayoutInputHorizontal()
    {
    }

    public void CalculateLayoutInputVertical()
    {
    }
}
