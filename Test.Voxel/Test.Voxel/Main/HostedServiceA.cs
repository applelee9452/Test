using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Test;

public class HostedServiceA : IHostedService
{
    ILogger Logger { get; set; }

    public HostedServiceA(ILogger<HostedServiceA> logger)
    {
        Logger = logger;
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("HostedServiceA.StartAsync()");

        // 创建一个支持LOD0到LOD9的稀疏体素八叉树
        // 根节点范围从(0,0,0)到(1024,1024,1024)
        var svo = new SparseVoxelOctree(9, new Vector3Int(0, 0, 0), new Vector3Int(1024, 1024, 1024));

        // 插入一些测试体素
        InsertTestVoxels(svo);

        // 获取不同LOD级别的体素
        var voxelLOD0 = svo.GetVoxel(new Vector3Int(50, 50, 50), 0);
        var voxelLOD5 = svo.GetVoxel(new Vector3Int(50, 50, 50), 5);
        var voxelLOD9 = svo.GetVoxel(new Vector3Int(50, 50, 50), 9);

        Console.WriteLine($"LOD0体素: {voxelLOD0?.MaterialId}");
        Console.WriteLine($"LOD5体素: {voxelLOD5?.MaterialId}");
        Console.WriteLine($"LOD9体素: {voxelLOD9?.MaterialId}");

        // 简化到LOD5
        svo.SimplifyToLOD(5);
        Console.WriteLine("已简化到LOD5");

        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("HostedServiceA.StopAsync()");

        return Task.CompletedTask;
    }

    private static void InsertTestVoxels(SparseVoxelOctree svo)
    {
        // 在不同位置插入不同LOD级别的体素
        for (int x = 0; x < 200; x += 20)
        {
            for (int y = 0; y < 200; y += 20)
            {
                for (int z = 0; z < 200; z += 20)
                {
                    // 随机LOD级别(0-9)和材质ID(0-4)
                    int lodLevel = (x + y + z) % 10;
                    int materialId = (x / 20 + y / 20 + z / 20) % 5;

                    var voxel = new Voxel(materialId, lodLevel);
                    svo.Insert(new Vector3Int(x, y, z), voxel);
                }
            }
        }
    }
}
