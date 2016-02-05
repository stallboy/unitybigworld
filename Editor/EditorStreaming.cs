using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof (Streaming))]
public class EditorStreaming : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var streaming = (Streaming) target;

        switch (streaming.operationState)
        {
            case Streaming.OperationState.Original:
                if (GUILayout.Button("Split", GUILayout.Width(100)))
                {
                    Split();
                }
                break;
            case Streaming.OperationState.Splitted:
                if (GUILayout.Button("Generate", GUILayout.Width(100)))
                {
                    Generate();
                }
                break;
            case Streaming.OperationState.Generated:
                break;
        }
    }

    private void Split()
    {
        var streaming = (Streaming) target;
        foreach (var layer in streaming.GetComponents<StreamingLayer>())
        {
            layer.EditorSplit();
        }
        streaming.operationState = Streaming.OperationState.Splitted;
    }

    private void Generate()
    {
        var streaming = (Streaming) target;
        foreach (var layer in streaming.GetComponents<StreamingLayer>())
        {
            string dir = streaming.splitToDir + layer.layerRoot.name + "/";
            Directory.CreateDirectory(dir);

            List<GameObject> objs = new List<GameObject>();
            foreach (Transform c in layer.layerRoot)
            {
                string localPath = dir + c.gameObject.name + ".prefab";
                Debug.Log("generate " + localPath);
                var prefab = PrefabUtility.CreateEmptyPrefab(localPath);
                PrefabUtility.ReplacePrefab(c.gameObject, prefab, ReplacePrefabOptions.Default);
                objs.Add(c.gameObject);
            }

            foreach (GameObject obj in objs)
            {
                DestroyImmediate(obj);
            }
        }
        streaming.operationState = Streaming.OperationState.Generated;
    }
}