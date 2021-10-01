using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "Minecraft/BiomeAttributes")]
public class BiomeAttributes : ScriptableObject
{
    public string biomeName;

    public int solidGroundHeight;
    public int terrainHeight;
    public float scale;

    [Header("Trees")] 
    public float treeZoneScale = 1.3f;
    public float treeZoneThreshold = .6f;
    public float treePlacementScale = 15f;
    public float treePlacementThreshold = .8f;
    public int maxTreeHeight = 12;
    public int minTreeHeight = 5;

    public Lode[] lodes;
}

[System.Serializable]
public class Lode
{
    public string lostName;
    public byte blockID;
    public int minHeight;
    public int maxHeight;

    public float scale;
    public float threshold;
    public float noiseOffset;
}

