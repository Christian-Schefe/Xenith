using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UIImage : MaskableGraphic, ILayoutElement
{
    private float m_CachedReferencePixelsPerUnit = 100f;

    [SerializeField]
    private Color32 m_OutlineColor = Color.black;

    [SerializeField]
    private float m_OutlineWidth = 0;

    [SerializeField]
    private float m_CornerRadius = 0;

    [SerializeField]
    private bool m_Fill = true;
    [SerializeField]
    private bool m_Outline = false;

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

    protected override void Awake()
    {
        base.Awake();
        canvasRenderer.cullTransparentMesh = false;
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
        if (m_Material == null)
        {
            m_Material = Resources.Load<Material>("Shaders/UIImageMaterial");
        }
        canvasRenderer.cullTransparentMesh = false;
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
        bool hasOutline = outline && m_OutlineWidth > 0 && m_OutlineColor.a > 0;
        bool hasFill = fill && this.color.a > 0;
        Color32 color = hasFill ? this.color : Color.clear;
        Color32 outlineColor = hasOutline ? this.outlineColor : Color.clear;

        var outlineColorVec = new Vector4(outlineColor.r, outlineColor.g, outlineColor.b, outlineColor.a) / 255f;
        var outlineWidth = Mathf.Min(m_OutlineWidth, pixelAdjustedRect.width * 0.5f, pixelAdjustedRect.height * 0.5f);
        var cornerRadius = Mathf.Min(m_CornerRadius, pixelAdjustedRect.width * 0.5f, pixelAdjustedRect.height * 0.5f);
        var additionalInfoVec = new Vector4(hasOutline ? outlineWidth : 0, cornerRadius, 0, 0);

        vh.Clear();

        vh.AddVert(new Vector3(pixelAdjustedRect.xMin, pixelAdjustedRect.yMin), color, new Vector4(0f, 0f, pixelAdjustedRect.width, pixelAdjustedRect.height), Vector4.zero, outlineColorVec, additionalInfoVec, s_DefaultNormal, s_DefaultTangent);
        vh.AddVert(new Vector3(pixelAdjustedRect.xMin, pixelAdjustedRect.yMax), color, new Vector4(0f, 1f, pixelAdjustedRect.width, pixelAdjustedRect.height), Vector4.zero, outlineColorVec, additionalInfoVec, s_DefaultNormal, s_DefaultTangent);
        vh.AddVert(new Vector3(pixelAdjustedRect.xMax, pixelAdjustedRect.yMax), color, new Vector4(1f, 1f, pixelAdjustedRect.width, pixelAdjustedRect.height), Vector4.zero, outlineColorVec, additionalInfoVec, s_DefaultNormal, s_DefaultTangent);
        vh.AddVert(new Vector3(pixelAdjustedRect.xMax, pixelAdjustedRect.yMin), color, new Vector4(1f, 0f, pixelAdjustedRect.width, pixelAdjustedRect.height), Vector4.zero, outlineColorVec, additionalInfoVec, s_DefaultNormal, s_DefaultTangent);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
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
