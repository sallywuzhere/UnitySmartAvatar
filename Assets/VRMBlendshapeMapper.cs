using System;
using System.Collections.Generic;
using uLipSync;
using UnityEngine;

public enum VRMBlendshape
{
    A, I, U, E, O, Neutral
}

public struct ARKitBlendshapeKVP
{
    public string name;
    public int value;
}

public class VRMBlendshapeMapper : MonoBehaviour
{
    [Tooltip("The uLipSync component whose phoneme events drive this mapper. " +
             "If left empty, the mapper will search for one on this GameObject or its parents.")]
    [SerializeField] private uLipSync.uLipSync lipSyncSource;

    [Tooltip("Higher = smoother (laggier). Lower = snappier (responsive). 0 = no smoothing.")]
    [Range(0f, 0.95f)] public float smoothing = 0.7f;

    private SkinnedMeshRenderer[] skinnedMeshRenderers;

    // Cache of last frame's applied FACS weights. We lerp from these toward the new targets
    // each update to eliminate jitter from raw phoneme fluctuations.
    private readonly Dictionary<string, float> _prevWeights = new Dictionary<string, float>();

    private Dictionary<VRMBlendshape, List<ARKitBlendshapeKVP>> vrmToARKitMappings =
        new Dictionary<VRMBlendshape, List<ARKitBlendshapeKVP>>()
        {
            {
                VRMBlendshape.A, new List<ARKitBlendshapeKVP>()
                {
                    new ARKitBlendshapeKVP { name = "jawOpen", value = 57 },
                    new ARKitBlendshapeKVP { name = "mouthFunnel", value = 18 }
                }
            },
            {
                VRMBlendshape.E, new List<ARKitBlendshapeKVP>()
                {
                    new ARKitBlendshapeKVP { name = "jawOpen", value = 35 },
                    new ARKitBlendshapeKVP { name = "mouthSmileLeft", value = 16 },
                    new ARKitBlendshapeKVP { name = "mouthSmileRight", value = 16 },
                    new ARKitBlendshapeKVP { name = "mouthUpperUpLeft", value = 35 },
                    new ARKitBlendshapeKVP { name = "mouthUpperUpRight", value = 35 }
                }
            },
            {
                VRMBlendshape.I, new List<ARKitBlendshapeKVP>()
                {
                    new ARKitBlendshapeKVP { name = "jawOpen", value = 15 },
                    new ARKitBlendshapeKVP { name = "mouthDimpleLeft", value = 20 },
                    new ARKitBlendshapeKVP { name = "mouthDimpleRight", value = 20 },
                    new ARKitBlendshapeKVP { name = "mouthLowerDownLeft", value = 20 },
                    new ARKitBlendshapeKVP { name = "mouthLowerDownRight", value = 20 },
                    new ARKitBlendshapeKVP { name = "mouthShrugUpper", value = 62 },
                    new ARKitBlendshapeKVP { name = "mouthSmileLeft", value = 42 },
                    new ARKitBlendshapeKVP { name = "mouthSmileRight", value = 42 },
                    new ARKitBlendshapeKVP { name = "mouthStretchLeft", value = 42 },
                    new ARKitBlendshapeKVP { name = "mouthStretchRight", value = 42 }
                }
            },
            {
                VRMBlendshape.O, new List<ARKitBlendshapeKVP>()
                {
                    new ARKitBlendshapeKVP { name = "jawOpen", value = 80 },
                    new ARKitBlendshapeKVP { name = "mouthPucker", value = 47 }
                }
            },
            {
                VRMBlendshape.U, new List<ARKitBlendshapeKVP>()
                {
                    new ARKitBlendshapeKVP { name = "jawOpen", value = 10 },
                    new ARKitBlendshapeKVP { name = "mouthFunnel", value = 100 }
                }
            }
        };
    
    private void Awake()
    {
        skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        if (lipSyncSource == null)
        {
            lipSyncSource = GetComponentInParent<uLipSync.uLipSync>();
        }
    }

    private void OnEnable()
    {
        lipSyncSource.onLipSyncUpdate.AddListener(OnLipSyncUpdate);
    }

    private void OnDisable()
    {
        lipSyncSource.onLipSyncUpdate.RemoveListener(OnLipSyncUpdate);
    }

    public void OnLipSyncUpdate(LipSyncInfo info)
    {
        // Step 1: compute this frame's target weight per FACS shape by ADDING up
        // contributions from every active phoneme (fixes the overwriting bug).
        var targets = new Dictionary<string, float>();
        foreach (var kvp in info.phonemeRatios)
        {
            if (!Enum.TryParse(kvp.Key, out VRMBlendshape vrm)) continue;
            if (!vrmToARKitMappings.TryGetValue(vrm, out var mappings)) continue;

            float phonemeWeight = kvp.Value * info.volume;
            foreach (var m in mappings)
            {
                targets.TryGetValue(m.name, out float current);
                targets[m.name] = current + m.value * phonemeWeight;
            }
        }

        // Step 2: include any shapes that were active last frame so they decay to 0,
        // otherwise they'd stay stuck at their last value.
        foreach (var kvp in _prevWeights)
        {
            if (!targets.ContainsKey(kvp.Key)) targets[kvp.Key] = 0f;
        }

        // Step 3: lerp from previous frame toward target, cache, and apply to meshes.
        foreach (var kvp in targets)
        {
            _prevWeights.TryGetValue(kvp.Key, out float prev);
            float smoothed = Mathf.Lerp(prev, kvp.Value, 1f - smoothing);
            _prevWeights[kvp.Key] = smoothed;

            foreach (var smr in skinnedMeshRenderers)
            {
                int idx = GetBlendshapeIndex(smr, kvp.Key);
                if (idx >= 0) smr.SetBlendShapeWeight(idx, smoothed);
            }
        }
    }

    private int GetBlendshapeIndex(SkinnedMeshRenderer smr, string name)
    {
        for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
        {
            if (smr.sharedMesh.GetBlendShapeName(i).ToLower().Contains(name.ToLower()))
            {
                //Debug.Log($"Blendshape {name} found in {smr.sharedMesh.GetBlendShapeName(i)}", smr.gameObject);
                return i;
            }
        }
        return -1;
    }
}
