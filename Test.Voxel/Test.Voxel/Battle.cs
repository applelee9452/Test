using System;
using System.Collections.Generic;
using System.Linq;

using Test;

public class Battle
{
    public void DoBattle(string[] args)
    {
        // 1. 初始化原点管理器
        var earthSvoOrigin = new DynamicOriginManager(CoordinateType.SvoLocal, 1e8); // 地球SVO原点（阈值1亿米）
        var voidOrigin = new DynamicOriginManager(CoordinateType.SystemVoid, Constants.VoidOriginResetThreshold); // 虚空原点
        var marsSvoOrigin = new DynamicOriginManager(CoordinateType.SvoLocal, 1e8); // 火星SVO原点

        // 2. 创建主角飞船（从地球SVO出发）
        var playerShip = new Spaceship(
            "玩家飞船",
            GlobalId.Encode(15, 256, 1, 3, 0, 0), // 星区15,星系256,太阳系1,地球3
            CoordinateType.SvoLocal,
            earthSvoOrigin
        );
        Console.WriteLine("主角飞船初始位置：地球SVO局部坐标 (0, 0, 0)");

        // 3. 主角飞船起飞，进入虚空
        playerShip.Move(new Vector3d(0, 0, Constants.SvoCriticalHeight + 5000)); // 飞到105公里高度（过渡层）
        playerShip.EnterVoid();
        Console.WriteLine($"主角飞船进入虚空，位置：太阳系虚空坐标 ({playerShip.Position.X / Constants.AuToMeter:F2}AU, ...)");

        // 4. 主角飞船在虚空航行，前往火星
        var marsDirection = (GeoDatabase.Bodies[4].Center - GeoDatabase.Bodies[3].Center).Normalize(); // 地球到火星方向
        playerShip.Move(marsDirection * (0.5 * Constants.AuToMeter)); // 移动0.5AU
        Console.WriteLine($"主角飞船在虚空航行后，位置：({playerShip.Position.X / Constants.AuToMeter:F2}AU, ...)");

        // 5. 主角飞船进入火星SVO
        playerShip.EnterMarsSvo(marsSvoOrigin);
        Console.WriteLine($"主角飞船进入火星SVO，局部坐标：({playerShip.Position.X:F0}, {playerShip.Position.Z:F0})米");

        // 6. 模拟地球SVO与虚空交界处的100艘飞船大战
        SimulateBattle(earthSvoOrigin, voidOrigin);
    }

    // 模拟交界处100艘飞船大战
    public void SimulateBattle(DynamicOriginManager svoOrigin, DynamicOriginManager voidOrigin)
    {
        Console.WriteLine("\n=== 地球SVO与虚空交界处战斗模拟 ===");
        var ships = new List<Spaceship>();

        // 创建50艘SVO内飞船（地球低空）
        for (int i = 0; i < 50; i++)
        {
            var id = GlobalId.Encode(15, 256, 1, 3, i * 1000, 0); // 地球SVO内分散位置
            ships.Add(new Spaceship($"SVO飞船{i}", id, CoordinateType.SvoLocal, svoOrigin));
        }

        // 创建50艘虚空飞船（过渡层/虚空）
        for (int i = 0; i < 50; i++)
        {
            var id = GlobalId.Encode(15, 256, 1, 0, 0, 0); // 虚空ID
            var ship = new Spaceship($"虚空飞船{i}", id, CoordinateType.SystemVoid, voidOrigin);
            // 位置：地球SVO临界高度附近
            ship.Position = GeoDatabase.Bodies[3].Center + new Vector3d(0, 0, Constants.SvoCriticalHeight + i * 1000);
            ships.Add(ship);
        }

        // 战斗循环（10轮攻击）
        for (int round = 0; round < 10; round++)
        {
            Console.WriteLine($"\n--- 第{round + 1}轮攻击 ---");
            foreach (var attacker in ships)
            {
                if (!attacker.IsAlive) continue;
                // 随机攻击一个目标
                var target = ships[new Random().Next(ships.Count)];
                if (attacker != target && target.IsAlive)
                {
                    attacker.Attack(target);
                }
            }

            // 统计存活数
            var aliveCount = ships.Count(s => s.IsAlive);
            Console.WriteLine($"本轮结束，存活飞船：{aliveCount}/100");
            if (aliveCount <= 10) break; // 剩余太少时结束
        }
    }
}