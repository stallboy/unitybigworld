using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreamingLayer : MonoBehaviour
{
    public Transform layerRoot;
    public int cellSize = 64;

    public int cellCntX;
    public int cellCntZ;
    public Bounds layerBounds;

    public List<int> notEmptyCells = new List<int>();

    public bool showCellGizmos;
    public float gizmosPadding = 2f;
    public float gizmosY = 10;
    public Color gizmosColor = Color.gray;

    private readonly List<int> holdingCells = new List<int>();
    private int currentInCell = -1;

    private readonly List<int> see = new List<int>();
    private readonly List<int> see1 = new List<int>();
    private readonly List<int> toload = new List<int>();
    private readonly List<int> tounload = new List<int>();

    private string splitToDir;
    private bool isSplitInResourcesDir;

    void Start()
    {
        var streaming = GetComponent<Streaming>();
        splitToDir = streaming.splitToDir + layerRoot.name + "/";
        if (splitToDir.StartsWith("Assets/Resources/"))
        {
            isSplitInResourcesDir = true;
            splitToDir = splitToDir.Substring(17);
        }
    }

    public void SimulateStart()
    {
        foreach (var c in notEmptyCells)
        {
            layerRoot.Find(tocell(c)).gameObject.SetActive(false);
        }
    }

    public void UpdatePos(Vector3 pos, bool simulate)
    {
        int x, z, idx;
        findcell(pos, out x, out z, out idx);

        if (currentInCell == idx)
            return;

        currentInCell = idx;

        see.Clear();
        collectcell(x, z, see);
        collectcell(x - 1, z - 1, see);
        collectcell(x - 1, z, see);
        collectcell(x - 1, z + 1, see);
        collectcell(x, z - 1, see);
        collectcell(x, z + 1, see);
        collectcell(x + 1, z - 1, see);
        collectcell(x + 1, z, see);
        collectcell(x + 1, z + 1, see);

        see1.Clear();
        see1.AddRange(see);
        collectcell(x - 2, z - 2, see1);
        collectcell(x - 2, z - 1, see1);
        collectcell(x - 2, z, see1);
        collectcell(x - 2, z + 1, see1);
        collectcell(x - 2, z + 2, see1);

        collectcell(x + 2, z - 2, see1);
        collectcell(x + 2, z - 1, see1);
        collectcell(x + 2, z, see1);
        collectcell(x + 2, z + 1, see1);
        collectcell(x + 2, z + 2, see1);

        collectcell(x - 2, z - 2, see1);
        collectcell(x - 1, z - 2, see1);
        collectcell(x, z - 2, see1);
        collectcell(x + 1, z - 2, see1);
        collectcell(x + 2, z - 2, see1);

        collectcell(x - 2, z + 2, see1);
        collectcell(x - 1, z + 2, see1);
        collectcell(x, z + 2, see1);
        collectcell(x + 1, z + 2, see1);
        collectcell(x + 2, z + 2, see1);

        toload.Clear();
        var e = see.GetEnumerator();
        while (e.MoveNext())
        {
            int i = e.Current;
            if (!holdingCells.Contains(i))
            {
                toload.Add(i);
            }
        }

        tounload.Clear();
        e = holdingCells.GetEnumerator();
        while (e.MoveNext())
        {
            int i = e.Current;
            if (!see1.Contains(i))
            {
                tounload.Add(i);
            }
        }

        e = toload.GetEnumerator();
        while (e.MoveNext())
        {
            int i = e.Current;
            SwapIn(i, simulate);
            holdingCells.Add(i);
        }

        e = tounload.GetEnumerator();
        while (e.MoveNext())
        {
            int i = e.Current;
            SwapOut(i, simulate);
            holdingCells.Remove(i);
        }

        if (tounload.Count > 0)
        {
            Resources.UnloadUnusedAssets(); // TODO， hard to fine control memory use in unity
        }
    }

    private void SwapIn(int idx, bool simulate)
    {
        if (simulate)
        {
            layerRoot.Find(tocell(idx)).gameObject.SetActive(true);
        }
        else
        {
            StartCoroutine(load(idx));
        }
    }

    private IEnumerator load(int idx)
    {
        string cellname = tocell(idx);
        var localPath = splitToDir + cellname;
        if (isSplitInResourcesDir)
        {
            var req = Resources.LoadAsync(localPath);
            yield return req;
            GameObject obj = (GameObject) Instantiate(req.asset);
            obj.name = cellname;
            obj.transform.parent = layerRoot;
        }
        else
        {
            //TODO
        }
    }


    private void SwapOut(int idx, bool simulate)
    {
        if (simulate)
        {
            var go = layerRoot.Find(tocell(idx)).gameObject;
            go.SetActive(false);
        }
        else
        {
            var go = layerRoot.Find(tocell(idx)).gameObject;
            Destroy(go);
        }
    }

    private void collectcell(int x, int z, List<int> subscenes)
    {
        if (x >= 0 && x < cellCntX && z >= 0 && z < cellCntZ)
        {
            int idx = x + z*cellCntX;
            if (notEmptyCells.Contains(idx))
            {
                subscenes.Add(idx);
            }
        }
    }

    private void findcell(Vector3 pos, out int x, out int z, out int idx)
    {
        x = (int) (pos.x - layerBounds.min.x)/cellSize;
        z = (int) (pos.z - layerBounds.min.z)/cellSize;
        idx = x + z*cellCntX;
    }

    private string tocell(int idx)
    {
        var z = idx/cellCntX;
        var x = idx%cellCntX;
        return string.Format("x{0}_z{1}", x, z);
    }

    public void EditorSplit()
    {
        bound(layerRoot, ref layerBounds);

        cellCntX = ((int) layerBounds.extents.x*2 + cellSize - 1)/cellSize;
        cellCntZ = ((int) layerBounds.extents.z*2 + cellSize - 1)/cellSize;
        Dictionary<int, HashSet<GameObject>> cellOwns = new Dictionary<int, HashSet<GameObject>>();

        foreach (Transform c in layerRoot)
        {
            int x, z, idx;
            findcell(c.position, out x, out z, out idx);
            HashSet<GameObject> cellOwn;
            if (false == cellOwns.TryGetValue(idx, out cellOwn))
            {
                cellOwn = new HashSet<GameObject>();
                cellOwns.Add(idx, cellOwn);
            }

            cellOwn.Add(c.gameObject);
        }


        foreach (var e in cellOwns)
        {
            var cellname = tocell(e.Key);
            notEmptyCells.Add(e.Key);

            var cellobj = new GameObject(cellname);
            cellobj.transform.SetParent(layerRoot);
            foreach (var own in e.Value)
            {
                own.transform.SetParent(cellobj.transform);
            }
        }
    }


    void OnDrawGizmos()
    {
        if (showCellGizmos)
        {
            foreach (var idx in notEmptyCells)
            {
                Gizmos.color = currentInCell == idx
                    ? Color.blue
                    : (holdingCells.Contains(idx) ? Color.green : gizmosColor);
                draw(idx);
            }

            if (currentInCell != -1 && !notEmptyCells.Contains(currentInCell))
            {
                Gizmos.color = Color.blue;
                draw(currentInCell);
            }
        }
    }

    private void draw(int idx)
    {
        var z = idx/cellCntX;
        var x = idx%cellCntX;
        var r = new Rect(layerBounds.min.x + x*cellSize, layerBounds.min.z + z*cellSize, cellSize, cellSize);

        Gizmos.DrawLine(new Vector3(r.xMin + gizmosPadding, gizmosY, r.yMin + gizmosPadding),
            new Vector3(r.xMin + gizmosPadding, gizmosY, r.yMax - gizmosPadding));
        Gizmos.DrawLine(new Vector3(r.xMin + gizmosPadding, gizmosY, r.yMin + gizmosPadding),
            new Vector3(r.xMax - gizmosPadding, gizmosY, r.yMin + gizmosPadding));
        Gizmos.DrawLine(new Vector3(r.xMax - gizmosPadding, gizmosY, r.yMax - gizmosPadding),
            new Vector3(r.xMin + gizmosPadding, gizmosY, r.yMax - gizmosPadding));
        Gizmos.DrawLine(new Vector3(r.xMax - gizmosPadding, gizmosY, r.yMax - gizmosPadding),
            new Vector3(r.xMax - gizmosPadding, gizmosY, r.yMin + gizmosPadding));
    }

    private static void bound(Transform t, ref Bounds bd)
    {
        var r = t.GetComponent<Renderer>();
        if (r != null)
        {
            bd.Encapsulate(r.bounds);
        }

        var e = t.GetEnumerator();
        while (e.MoveNext())
        {
            bound((Transform) e.Current, ref bd);
        }
    }
}