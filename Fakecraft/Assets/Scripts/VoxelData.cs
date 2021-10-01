using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelData
{
    public static readonly int chunk_width = 16, chunk_height = 128;
    public static readonly int texture_atlas_size_in_blocks = 16;

    public static float normalized_block_texture_size
    {
        get
        {
            return 1f / (float)texture_atlas_size_in_blocks;
        }
    }

    public static readonly int world_size = 100;

    public static int world_size_inVoxels
    {
        get
        {
            return world_size * chunk_width;
        }
    }

    public static readonly int view_distance = 8;
    
    public static readonly Vector3[] voxelVerts =
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(1, 1, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 0, 1),
        new Vector3(1, 0, 1),
        new Vector3(1, 1, 1),
        new Vector3(0, 1, 1),
    };

    public static readonly Vector3[] faceChecks =
    {
        new Vector3(0, 0, -1),
        new Vector3(0, 0, 1),
        new Vector3(0, 1, 0),
        new Vector3(0, -1, 0),
        new Vector3(-1, 0, 0),
        new Vector3(1, 0, 0),
    };

    public static readonly int[,] voxelTris =
    {
        //back, front, top, bottom, left, right
        //0, 1, 2, 2, 1, 3
        {0, 3, 1, 2}, //back face 
        {5, 6, 4, 7}, //front face
        {3, 7, 2, 6}, //top face
        {1, 5, 0, 4}, //bottom face
        {4, 7, 0, 3}, //left face
        {1, 2, 5, 6}, //right face
    };
}
