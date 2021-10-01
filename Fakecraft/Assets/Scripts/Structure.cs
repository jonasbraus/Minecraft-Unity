using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public static class Structure
{
    public static void MakeTree(Vector3 position, Queue<VoxelMod> modifications, int minTreeHeight, int maxTreeHeight)
    {
        int treeHeight = new Random().Next(minTreeHeight, maxTreeHeight);

        //tree trunk pass
        for (int y = 1; y < treeHeight; y++)
        {
            modifications.Enqueue(new VoxelMod(position + (Vector3.up * y), 6));
        }

        Vector3 leafesHeight = position + Vector3.up * treeHeight;

        //ground leafes pass
        modifications.Enqueue(new VoxelMod(leafesHeight + Vector3.forward - Vector3.up, 11));
        modifications.Enqueue(new VoxelMod(leafesHeight - Vector3.forward - Vector3.up, 11));
        modifications.Enqueue(new VoxelMod(leafesHeight + Vector3.right - Vector3.up, 11));
        modifications.Enqueue(new VoxelMod(leafesHeight - Vector3.right - Vector3.up, 11));
        
        //first layer leafes pass
        for (int i = -2; i <= 2; i++)
        {
            modifications.Enqueue(new VoxelMod(leafesHeight + Vector3.right * i, 11));
        }
        for (int i = -2; i <= 2; i++)
        {
            if(i != 0)
                modifications.Enqueue(new VoxelMod(leafesHeight + Vector3.forward * i, 11));
        }
        
        modifications.Enqueue(new VoxelMod(leafesHeight + Vector3.forward + Vector3.right, 11));
        modifications.Enqueue(new VoxelMod(leafesHeight - Vector3.forward + Vector3.right, 11));
        modifications.Enqueue(new VoxelMod(leafesHeight + Vector3.forward - Vector3.right, 11));
        modifications.Enqueue(new VoxelMod(leafesHeight - Vector3.forward - Vector3.right, 11));
        
        //second layer leafes pass
        modifications.Enqueue(new VoxelMod(leafesHeight + Vector3.forward + Vector3.up * 1, 11));
        modifications.Enqueue(new VoxelMod(leafesHeight - Vector3.forward + Vector3.up * 1, 11));
        modifications.Enqueue(new VoxelMod(leafesHeight + Vector3.right + Vector3.up * 1, 11));
        modifications.Enqueue(new VoxelMod(leafesHeight - Vector3.right + Vector3.up * 1, 11));
        
        //third layer leafes pass
        modifications.Enqueue(new VoxelMod(leafesHeight + Vector3.up * 2, 11));
        
    }
}
