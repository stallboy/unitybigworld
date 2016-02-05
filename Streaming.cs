using UnityEngine;

public class Streaming : MonoBehaviour
{
    public enum OperationState
    {
        Original,
        Splitted,
        Generated
    }

    public OperationState operationState = OperationState.Original;
    public string splitToDir = "Assets/Resources/startscene/";

    public bool enableSimulate;
    public Transform simulatePlayer;

    public void Start()
    {
        if (enableSimulate && operationState == OperationState.Splitted)
        {
            foreach (var layer in GetComponents<StreamingLayer>())
            {
                layer.SimulateStart();
            }
        }
    }

    public void Update()
    {
        if (simulatePlayer != null)
        {
            bool simulate = enableSimulate && operationState == OperationState.Splitted;
            if (simulate)
            {
                UpdatePos(simulatePlayer.position, true);
            }
            else if (operationState == OperationState.Generated)
            {
                UpdatePos(simulatePlayer.position, false);
            }
        }
    }

    public void UpdatePos(Vector3 pos, bool simulate)
    {
        foreach (var layer in GetComponents<StreamingLayer>())
        {
            layer.UpdatePos(pos, simulate);
        }
    }
}