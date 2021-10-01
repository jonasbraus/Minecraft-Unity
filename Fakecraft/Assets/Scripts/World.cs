using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class World : MonoBehaviour
{
    public int seed;
    public BiomeAttributes biome;
    
    public Transform player;
    public Vector3 spawnPosition;
    public Material material;
    public BlockType[] blockTypes;

    private Chunk[,] chunks = new Chunk[VoxelData.world_size, VoxelData.world_size];
    private List<ChunkCoord> activeChunks = new List<ChunkCoord>();

    private ChunkCoord playerLastChunkCoord;

    private List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    public List<Chunk> chunksToUpdate = new List<Chunk>();
    private bool isCreatingChunks = false;

    private int noiseOffset;

    private Queue<VoxelMod> modifications = new Queue<VoxelMod>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();
    private bool applyingModifications = false;

    private bool _inUI = false;

    public bool inUI
    {
        get
        {
            return _inUI;
        }

        set
        {
            _inUI = value;
        }
    }
    
    private void Start()
    {
        seed = (int)Random.Range(100, 10000);
        noiseOffset = seed;
        
        spawnPosition = new Vector3(VoxelData.world_size_inVoxels / 2f, VoxelData.chunk_height - 5, VoxelData.world_size_inVoxels / 2f);
        player.position = spawnPosition;
        playerLastChunkCoord = GetChunkCoordFromVector3(spawnPosition);
        GenerateWorld();
    }

    private void Update()
    {
        if(!GetChunkCoordFromVector3(player.position).Equals(playerLastChunkCoord))
        {
            CheckViewDistance();
        }

        if (modifications.Count > 0)
        {
            ApplyModifications();
        }

        if (chunksToCreate.Count > 0)
        {
            CreateChunk();
        }

        if (chunksToDraw.Count > 0)
        {
            DrawChunks();
        }

        if (chunksToUpdate.Count > 0)
        {
            UpdateChunks();
        }
    }

    private void DrawChunks()
    {
        if(chunksToDraw.Peek().isEditable)
        {
            chunksToDraw.Dequeue().CreateMesh();
        }
    }

    private void ApplyModifications()
    {
        int count = 0;

        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();

            ChunkCoord c = GetChunkCoordFromVector3(v.position);

            if (chunks[c.x, c.z] == null)
            {
                chunks[c.x, c.z] = new Chunk(c, this, true);
                activeChunks.Add(c);
            }
            
            chunks[c.x, c.z].modifications.Enqueue(v);

            if (!chunksToUpdate.Contains(chunks[c.x, c.z]))
            {
                chunksToUpdate.Add(chunks[c.x, c.z]);
            }

            count++;
            if (count > 20)
            {
                return;
            }
        }
    }

    private void CreateChunk()
    {
        
        chunks[chunksToCreate[0].x, chunksToCreate[0].z].Init();
        activeChunks.Add(new ChunkCoord(chunksToCreate[0].x, chunksToCreate[0].z));
        chunksToCreate.RemoveAt(0);
        
    }

    private void UpdateChunks()
    {
        bool updated = false;
        int index = 0;

        while (!updated && index < chunksToUpdate.Count - 1)
        {
            if (chunksToUpdate[0].isEditable)
            {
                chunksToUpdate[0].UpdateChunk();
                updated = true;
                chunksToUpdate.RemoveAt(0);
            }
            else
            {
                index++;
            }
        }
    }
    
    private void GenerateWorld()
    {
        for (int x = (int) (VoxelData.world_size / 2f - VoxelData.view_distance); x < (int) (VoxelData.world_size / 2f + VoxelData.view_distance); x++)
        {
            for (int z = (int) (VoxelData.world_size / 2f - VoxelData.view_distance); z < (int) (VoxelData.world_size / 2f + VoxelData.view_distance); z++)
            {
               
                    chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, false);
                    chunksToCreate.Add(new ChunkCoord(x, z));
            }
        }

        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();

            ChunkCoord c = GetChunkCoordFromVector3(v.position);

            if (chunks[c.x, c.z] == null)
            {
                chunks[c.x, c.z] = new Chunk(c, this, true);
                activeChunks.Add(c);
            }
            
            chunks[c.x, c.z].modifications.Enqueue(v);

            if (!chunksToUpdate.Contains(chunks[c.x, c.z]))
            {
                chunksToUpdate.Add(chunks[c.x, c.z]);
            } 
        }

        while (chunksToUpdate.Count > 0)
        {
            if (chunksToUpdate[0].isActive)
            {
                chunksToUpdate[0].UpdateChunk();
                chunksToUpdate.RemoveAt(0);
            }
        }
        
        int terrainHeight = Mathf.FloorToInt(Noise.Get2DPerlin(new Vector2((int)player.position.x, (int)player.position.z), noiseOffset, biome.scale) * biome.terrainHeight + biome.solidGroundHeight);
        player.position = new Vector3((int) player.position.x, terrainHeight + 1, (int) player.position.z);

        while (GetVoxel(player.position) != 0 && GetVoxel(player.position + Vector3.up) != 0)
        {
            player.position += Vector3.forward;
        }
    }

    private ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.chunk_width);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunk_width);

        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.chunk_width);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunk_width);

        return chunks[x, z];
    }
    
    private void CheckViewDistance()
    {
        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for (int x = coord.x - VoxelData.view_distance; x < coord.x + VoxelData.view_distance; x++)
        {
            for (int z = coord.z - VoxelData.view_distance; z < coord.z + VoxelData.view_distance; z++)
            {
                if (IsChunkInWorld(new ChunkCoord(x, z)))
                {
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, false);
                        chunksToCreate.Add(new ChunkCoord(x, z));
                    }
                    else if(!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                        activeChunks.Add(new ChunkCoord(x, z));
                    }
                }
                
                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(new ChunkCoord(x, z)))
                    {
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }

        foreach (ChunkCoord c in previouslyActiveChunks)
        {
            activeChunks.Remove(c);
            chunks[c.x, c.z].isActive = false;
        }
    }

    public bool CheckForVoxel(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);
        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.chunk_height)
        {
            return false;
        }

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isEditable)
        {
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;
        }

        return blockTypes[GetVoxel(pos)].isSolid;
    }
    
    public bool CheckForFaceDraw(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);
        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.chunk_height)
        {
            return false;
        }

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isEditable)
        {
            if (blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isTransparent)
            {
                return false;
            }
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;
        }

        return blockTypes[GetVoxel(pos)].isSolid;
    }

    public byte GetVoxel(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);
        
        /***immutable pass***/
        
        //if bottom block of chunk, return bedrock
        if (y == 0)
        {
            return 1;
        }
        
        /***basic terrain pass***/
        int terrainHeight = Mathf.FloorToInt(Noise.Get2DPerlin(new Vector2(x, z), noiseOffset, biome.scale) * biome.terrainHeight + biome.solidGroundHeight);
        if (y <= terrainHeight)
        {
            byte voxelValue = 0;
            
            if (y == terrainHeight)
            {
                voxelValue = 3;
            }
            else if (y >= terrainHeight - 3)
            {
                voxelValue = 5;
            }
            else
            {
                voxelValue = 2;
            }

            /*second pass*/
            if (voxelValue == 2)
            {
                foreach (Lode l in biome.lodes)
                {
                    if (y > l.minHeight && y < l.maxHeight)
                    {
                        if (Noise.Get3DPerlin(pos, l.noiseOffset, l.scale, l.threshold))
                        {
                            voxelValue = l.blockID;
                        }
                    }
                }
            }
            
            /***tree pass***/
            if (y == terrainHeight)
            {
                if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.treeZoneScale) > biome.treeZoneThreshold)
                {
                    if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.treePlacementScale) > biome.treePlacementThreshold)
                    {
                        Structure.MakeTree(pos, modifications, biome.minTreeHeight, biome.maxTreeHeight);
                    }
                }
            }
            
            return voxelValue;
        }

        return 0;
    }

    private bool IsChunkInWorld(ChunkCoord coord)
    {
        if (coord.x > 0 && coord.z > 0 && coord.x < VoxelData.world_size - 1 && coord.z < VoxelData.world_size)
        {
            return true;
        }

        return false;
    }

    private bool IsVoxelInWorld(Vector3 pos)
    {
        if (pos.x > 0 && pos.y > 0 && pos.z > 0 && pos.x < VoxelData.world_size_inVoxels && pos.y < VoxelData.chunk_height && pos.z < VoxelData.world_size_inVoxels)
        {
            return true;
        }

        return false;
    }
}

[System.Serializable]
public class BlockType
{
    public string name;
    public bool isSolid;
    public bool isTransparent;
    public Sprite icon;

    public int textureBack, textureFront, textureTop, textureBottom, textureLeft, textureRight;

    public int GetTextureID(byte faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return textureBack;
            case 1:
                return textureFront;
            case 2:
                return textureTop;
            case 3:
                return textureBottom;
            case 4:
                return textureLeft;
            case 5:
                return textureRight;
            default:
                Debug.Log("The given faceIndex of " + faceIndex + " is not valid face. Only use faceIndex from 0 - 5!");
                return -1;
        }
    }
}

public class VoxelMod
{
    public Vector3 position;
    public byte id;

    public VoxelMod()
    {
        position = new Vector3();
        id = 0;
    }
    
    public VoxelMod(Vector3 position, byte id)
    {
        this.position = position;
        this.id = id;
    }
}
