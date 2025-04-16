using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum LineJoin
{
    Round,
    Bevel,
    Miter
}

public enum LineCap
{
    Round,
    Square,
    Flat
}

public class UILine : MaskableGraphic
{
    [SerializeField] private List<Vector2> points = new List<Vector2>();
    [SerializeField] private float width = 1f;
    [SerializeField] private LineJoin lineJoin = LineJoin.Round;
    [SerializeField] private LineCap lineCap = LineCap.Round;
    [SerializeField] private float miterLimit = 10f;
    [SerializeField] private bool closed = false;

    public float Width
    {
        get => width;
        set
        {
            if (width != value)
            {
                width = value;
                SetVerticesDirty();
            }
        }
    }

    public LineJoin LineJoin
    {
        get => lineJoin;
        set
        {
            if (lineJoin != value)
            {
                lineJoin = value;
                SetVerticesDirty();
            }
        }
    }

    public LineCap LineCap
    {
        get => lineCap;
        set
        {
            if (lineCap != value)
            {
                lineCap = value;
                SetVerticesDirty();
            }
        }
    }

    public float MiterLimit
    {
        get => miterLimit;
        set
        {
            if (miterLimit != value)
            {
                miterLimit = value;
                SetVerticesDirty();
            }
        }
    }

    public bool Closed
    {
        get => closed;
        set
        {
            if (closed != value)
            {
                closed = value;
                SetVerticesDirty();
            }
        }
    }

    public void SetPositions(Vector2[] newPoints)
    {
        points.Clear();
        points.AddRange(newPoints);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        GenerateLine(vh);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif

    private void GenerateLine(VertexHelper vh)
    {
        if (points.Count < 2)
        {
            return;
        }
        var finalPoints = points.ToList();
        if (closed)
        {
            if (finalPoints.Count > 0)
            {
                finalPoints.Add(finalPoints[0]);
            }
        }

        float totalDistance = 0f;
        for (int i = 1; i < finalPoints.Count; i++)
        {
            totalDistance += Vector2.Distance(finalPoints[i - 1], finalPoints[i]);
        }
        float distance = 0f;

        for (int i = 1; i < finalPoints.Count; i++)
        {
            var p1 = finalPoints[i - 1];
            var p2 = finalPoints[i];
            var dir = (p2 - p1).normalized;
            var perp = RotatedCW(dir);
            var halfWidth = width / 2f;
            var v1 = p1 + perp * halfWidth;
            var v2 = p1 - perp * halfWidth;
            var v3 = p2 + perp * halfWidth;
            var v4 = p2 - perp * halfWidth;

            var fromUV = new Vector2(distance / totalDistance, 0);
            distance += Vector2.Distance(p1, p2);
            var toUV = new Vector2(distance / totalDistance, 0);
            var uvs = new Vector2[] { fromUV, fromUV, toUV, toUV };

            AddQuad(vh, new Vector2[] { v1, v2, v3, v4 }, uvs);


            if (i < finalPoints.Count - 1)
            {
                var p3 = finalPoints[i + 1];
                var nextDir = (p3 - p2).normalized;
                var nextPerp = RotatedCW(nextDir);
                var nextV1 = p2 + nextPerp * halfWidth;
                var nextV2 = p2 - nextPerp * halfWidth;
                var turnsLeft = Vector3.Cross(dir, nextDir).z > 0;

                var outerFrom = turnsLeft ? v3 : v4;
                var outerTo = turnsLeft ? nextV1 : nextV2;
                GenerateJoin(vh, lineJoin, turnsLeft, outerFrom, outerTo, p2, distance / totalDistance);
            }
        }

        if (closed)
        {
            var p1 = finalPoints[^2];
            var p2 = finalPoints[0];
            var dir = (p2 - p1).normalized;
            var perp = RotatedCW(dir);
            var halfWidth = width / 2f;
            var v3 = p2 + perp * halfWidth;
            var v4 = p2 - perp * halfWidth;

            var p3 = finalPoints[1];
            var nextDir = (p3 - p2).normalized;
            var nextPerp = RotatedCW(nextDir);
            var nextV1 = p2 + nextPerp * halfWidth;
            var nextV2 = p2 - nextPerp * halfWidth;
            var turnsLeft = Vector3.Cross(dir, nextDir).z > 0;

            var outerFrom = turnsLeft ? v3 : v4;
            var outerTo = turnsLeft ? nextV1 : nextV2;
            GenerateJoin(vh, lineJoin, turnsLeft, outerFrom, outerTo, p2, distance / totalDistance);
        }
        else
        {
            GenerateCap(vh, lineCap, finalPoints[0], (finalPoints[1] - finalPoints[0]).normalized, 0);
            GenerateCap(vh, lineCap, finalPoints[^1], (finalPoints[^2] - finalPoints[^1]).normalized, 1);
        }
    }

    private void GenerateJoin(VertexHelper vh, LineJoin join, bool turnsLeft, Vector2 outerFrom, Vector2 outerTo, Vector2 p, float relativeDistance)
    {
        var uv = new Vector2(relativeDistance, 0);
        var uvs = new Vector2[] { uv, uv, uv };

        if (join == LineJoin.Miter)
        {
            var dir1 = outerFrom - p;
            var dir2 = outerTo - p;
            var perp1 = RotatedCW(dir1);
            var perp2 = RotatedCW(dir2);
            if (TryIntersect(outerFrom, perp1, outerTo, perp2, out var intersection))
            {
                var miterLength = Vector2.Distance(p, intersection) / width;
                if (miterLength <= miterLimit)
                {
                    AddTriangle(vh, new Vector2[] { outerFrom, outerTo, p }, uvs);
                    AddTriangle(vh, new Vector2[] { outerFrom, outerTo, intersection }, uvs);
                }
                else
                {
                    join = LineJoin.Bevel;
                }
            }
            else
            {
                join = LineJoin.Bevel;
            }
        }

        if (join == LineJoin.Round)
        {
            var dir1 = outerFrom - p;
            var dir2 = outerTo - p;
            var angle = Vector2.Angle(dir1, dir2) * Mathf.Deg2Rad;
            var radius = Vector2.Distance(outerFrom, p);
            var stepDir = turnsLeft ? 1 : -1;
            var stepCount = 10;
            var startAngle = Mathf.Atan2(dir1.y, dir1.x);
            var angleStep = angle / stepCount;
            var vertices = new Vector2[stepCount + 1];
            vertices[0] = outerFrom;
            vertices[stepCount] = outerTo;
            for (int i = 1; i < stepCount; i++)
            {
                var angleOffset = startAngle + angleStep * i * stepDir;
                vertices[i] = p + new Vector2(Mathf.Cos(angleOffset), Mathf.Sin(angleOffset)) * radius;
            }
            for (int i = 0; i < stepCount; i++)
            {
                var triangle = new Vector2[] { vertices[i], vertices[i + 1], p };
                AddTriangle(vh, triangle, uvs);
            }
        }
        else if (join == LineJoin.Bevel)
        {
            AddTriangle(vh, new Vector2[] { outerFrom, outerTo, p }, uvs);
        }
    }

    private void GenerateCap(VertexHelper vh, LineCap cap, Vector2 p, Vector2 dir, float relativeDistance)
    {
        if (cap == LineCap.Round)
        {
            var perp = RotatedCW(dir);
            GenerateJoin(vh, LineJoin.Round, false, p + perp * width / 2f, p - perp * width / 2f, p, relativeDistance);
        }
        else if (cap == LineCap.Square)
        {
            var perp = RotatedCW(dir);
            var uv = new Vector2(relativeDistance, 0);
            var uvs = new Vector2[] { uv, uv, uv, uv };
            var v1 = p + perp * width / 2f;
            var v2 = p - perp * width / 2f;
            AddQuad(vh, new Vector2[] { v1, v2, v2 - dir * width / 2f, v1 - dir * width / 2f }, uvs);
        }
        else if (cap == LineCap.Flat)
        {
            // Flat cap doesn't need to do anything
        }
    }

    private List<int> SortPointsConvex(List<Vector2> points)
    {
        // Calculate centroid
        Vector2 centroid = Vector2.zero;
        foreach (var point in points)
        {
            centroid += point;
        }
        centroid /= points.Count;

        // Sort points by angle around the centroid
        var vertices = points.Select((p, i) => (p, i)).ToList();
        vertices.Sort((a, b) =>
        {
            float angleA = Mathf.Atan2(a.p.y - centroid.y, a.p.x - centroid.x);
            float angleB = Mathf.Atan2(b.p.y - centroid.y, b.p.x - centroid.x);
            return angleA.CompareTo(angleB);
        });

        return vertices.Select(v => v.i).ToList();
    }

    private Vector2 RotatedCW(Vector2 v)
    {
        return new Vector2(v.y, -v.x);
    }

    private (int, int, int) ReorderWinding(Vector2[] v, int a, int b, int c)
    {
        var ab = (v[b] - v[a]).normalized;
        var ac = (v[c] - v[a]).normalized;
        var cross = Vector3.Cross(ab, ac);
        if (cross.z > 0)
        {
            return (a, c, b);
        }
        return (a, b, c);
    }

    private bool TryIntersect(Vector2 p1, Vector2 dir1, Vector2 p2, Vector2 dir2, out Vector2 intersection)
    {
        float x1 = p1.x;
        float y1 = p1.y;
        float x2 = p1.x + dir1.x;
        float y2 = p1.y + dir1.y;
        float x3 = p2.x;
        float y3 = p2.y;
        float x4 = p2.x + dir2.x;
        float y4 = p2.y + dir2.y;

        float denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
        if (denom == 0)
        {
            intersection = Vector2.zero;
            return false;
        }

        float t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
        intersection = new Vector2(x1 + t * (x2 - x1), y1 + t * (y2 - y1));
        return true;
    }

    private void AddTriangle(VertexHelper vh, Vector2[] triangle, Vector2[] uvs)
    {
        var tr = ReorderWinding(triangle, 0, 1, 2);
        var currentVertCount = vh.currentVertCount;
        vh.AddVert(triangle[0], color, uvs[0]);
        vh.AddVert(triangle[1], color, uvs[1]);
        vh.AddVert(triangle[2], color, uvs[2]);
        vh.AddTriangle(currentVertCount + tr.Item1, currentVertCount + tr.Item2, currentVertCount + tr.Item3);
    }

    private void AddQuad(VertexHelper vh, Vector2[] quad, Vector2[] uvs)
    {
        var vertexIndices = SortPointsConvex(quad.ToList()).ToArray();
        var tr1 = ReorderWinding(quad, vertexIndices[0], vertexIndices[1], vertexIndices[2]);
        var tr2 = ReorderWinding(quad, vertexIndices[0], vertexIndices[2], vertexIndices[3]);

        var currentVertCount = vh.currentVertCount;
        vh.AddVert(quad[0], color, uvs[0]);
        vh.AddVert(quad[1], color, uvs[1]);
        vh.AddVert(quad[2], color, uvs[2]);
        vh.AddVert(quad[3], color, uvs[3]);
        vh.AddTriangle(currentVertCount + tr1.Item1, currentVertCount + tr1.Item2, currentVertCount + tr1.Item3);
        vh.AddTriangle(currentVertCount + tr2.Item1, currentVertCount + tr2.Item2, currentVertCount + tr2.Item3);
    }
}
