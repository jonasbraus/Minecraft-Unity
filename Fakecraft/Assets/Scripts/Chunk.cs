using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Chunk
{
    public ChunkCoord coord;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private GameObject chunkObject;

    private int index = 0;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();

    public byte[,,] voxelMap = new byte[VoxelData.chunk_width, VoxelData.chunk_height, VoxelData.chunk_width];

    private World world;

    private bool _isActive = false;
    private bool isThreadLocked = false;
    private bool isVoxelMapPopulated = false;

    public bool isEditable
    {
        get
        {
            if (isThreadLocked || !isVoxelMapPopulated)
            {
                return false;
            }

            return true;
        }
    }

    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    public Vector3 position;

    public Chunk(ChunkCoord coord, World world, bool generateOnLoad)
    {
        this.world = world;
        this.coord = coord;
        isActive = true;
        
        if (generateOnLoad)
        {
            Init();
        }
    }

    public void Init()
    {
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        meshRenderer.material = world.material;
        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position =
            new Vector3(coord.x * VoxelData.chunk_width, 0, coord.z * VoxelData.chunk_width);
        position = chunkObject.transform.position;
        chunkObject.name = coord.ToString();

        PopulateVoxelMap();
        UpdateChunk();
    }

    private void PopulateVoxelMap()
    {
        for (int y = 0; y < VoxelData.chunk_height; y++)
        {
            for (int x = 0; x < VoxelData.chunk_width; x++)
            {
                for (int z = 0; z < VoxelData.chunk_width; z++)
                {
                    voxelMap[x, y, z] = world.GetVoxel(new Vector3(x, y, z) + position);
                }
            }
        }

        isVoxelMapPopulated = true;
    }
    

    public void UpdateChunk()
    {
        Thread newThread = new Thread(_update);
        ClearMeshData();
        newThread.Start();
    }

    public void UpdateNotThreaded()
    {
        isThreadLocked = true;
        
        ClearMeshData();
        
        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            Vector3 pos = v.position -= position;
            voxelMap[(int) pos.x, (int) pos.y, (int) pos.z] = v.id;
        }

        for (int y = 0; y < VoxelData.chunk_height; y++)
        {
            for (int x = 0; x < VoxelData.chunk_width; x++)
            {
                for (int z = 0; z < VoxelData.chunk_width; z++)
                {
                    if (world.blockTypes[voxelMap[x, y, z]].isSolid)
                    {
                        AddVoxelDataToChunk(new Vector3(x, y, z));
                    }
                }
            }
        }

        CreateMesh();

        isThreadLocked = false;
    }

    private void _update()
    {
        isThreadLocked = true;
        
        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            Vector3 pos = v.position -= position;
            voxelMap[(int) pos.x, (int) pos.y, (int) pos.z] = v.id;
        }

        for (int y = 0; y < VoxelData.chunk_height; y++)
        {
            for (int x = 0; x < VoxelData.chunk_width; x++)
            {
                for (int z = 0; z < VoxelData.chunk_width; z++)
                {
                    if (world.blockTypes[voxelMap[x, y, z]].isSolid)
                    {
                        AddVoxelDataToChunk(new Vector3(x, y, z));
                    }
                }
            }
        }

        lock (world.chunksToDraw)
        {
            world.chunksToDraw.Enqueue(this);
        }

        isThreadLocked = false;
    }

    private void ClearMeshData()
    {
        meshFilter.mesh = null;
        index = 0;
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
    }

    public bool isActive
    {
        get
        {
            return _isActive; 
            
        }
        set
        {
            _isActive = value;
            if (chunkObject != null)
            {
                chunkObject.SetActive(value);
            }
        }
    }

    private bool IsVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x > VoxelData.chunk_width - 1 || y > VoxelData.chunk_height - 1 ||
            z > VoxelData.chunk_width - 1)
        {
            return false;
        }

        return true;
    }

    public void EditVoxel(Vector3 pos, byte newID)
    {
        int x = (int)(pos.x);
        int y = (int)(pos.y);
        int z = (int)(pos.z);

        x -= (int)(chunkObject.transform.position.x);
        z -= (int)(chunkObject.transform.position.z);

        if(voxelMap[x, y, z] != 1)
        {
            voxelMap[x, y, z] = newID;

            UpdateSurroundingVoxels(x, y, z);
            
            UpdateNotThreaded();
        }
    }

    private void UpdateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int p = 0; p < 6; p++)
        {
            Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[p];

            if (!IsVoxelInChunk((int) currentVoxel.x, (int) currentVoxel.y, (int) currentVoxel.z))
            {
                world.GetChunkFromVector3(currentVoxel + position).UpdateNotThreaded();
            }

        }
        
    }

    private bool CheckVoxel(Vector3 pos)
    {
        int x = (int)(pos.x);
        int y = (int)(pos.y);
        int z = (int)(pos.z);

        if (!IsVoxelInChunk(x, y, z))
        {
            return world.CheckForFaceDraw(pos + position);
        }

        if (world.blockTypes[voxelMap[x, y, z]].isTransparent)
        {
            return false;
        }

        return world.blockTypes[voxelMap[x, y, z]].isSolid;
    }

    public byte GetVoxelFromGlobalVector3(Vector3 pos)
    {
        int x = (int)(pos.x);
        int y = (int)(pos.y);
        int z = (int)(pos.z);

        x -= (int)(position.x);
        z -= (int)(position.z);

        return voxelMap[x, y, z];
    }

    private void AddVoxelDataToChunk(Vector3 pos)
    {
        for (byte p = 0; p < 6; p++)
        {
            if (!CheckVoxel(VoxelData.faceChecks[p] + pos))
            {
                vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]] + pos);
                vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]] + pos);
                vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]] + pos);
                vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]] + pos);

                AddTexture(world.blockTypes[voxelMap[(int) pos.x, (int) pos.y, (int) pos.z]].GetTextureID(p));

                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);
                triangles.Add(index + 2);
                triangles.Add(index + 1);
                triangles.Add(index + 3);

                index += 4;
            }
        }
    }

    public void CreateMesh()
    {
        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
    }

    private void AddTexture(int textureID)
    {
        float y = textureID / VoxelData.texture_atlas_size_in_blocks;
        float x = textureID - (y * VoxelData.texture_atlas_size_in_blocks);

        x *= VoxelData.normalized_block_texture_size;
        y *= VoxelData.normalized_block_texture_size;

        y = 1f - y - VoxelData.normalized_block_texture_size;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.normalized_block_texture_size));
        uvs.Add(new Vector2(x + VoxelData.normalized_block_texture_size, y));
        uvs.Add(new Vector2(x + VoxelData.normalized_block_texture_size, y + VoxelData.normalized_block_texture_size));
    }
}

public class ChunkCoord
{
    public int x, z;

    public ChunkCoord(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public ChunkCoord()
    {
        x = 0;
        z = 0;
    }

    public ChunkCoord(Vector3 pos)
    {
        int x = (int)(pos.x);
        int z = (int)(pos.z);

        this.x = x / VoxelData.chunk_width;
        this.z = z / VoxelData.chunk_width;
    }

    public override string ToString()
    {
        return x + ", " + z;
    }

    public bool Equals(ChunkCoord coord)
    {
        if (coord == null)
        {
            return false;
        }

        if (coord.x == x && coord.z == z)
        {
            return true;
        }

        return false;
    }
}