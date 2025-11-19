using System;

public class SvoTest
{
    public void Test(string[] args)
    {
        // 初始化地球SVO原点管理器
        var earthSvoOrigin = new DynamicOriginManager(CoordinateType.SvoLocal, 1e8);

        // 初始化地球SVO管理器
        var earthSvo = new EarthSvoManager(earthSvoOrigin);

        // 创建飞船并放置在地球SVO内
        var playerShip = new Spaceship(
            "玩家飞船",
            GlobalId.Encode(15, 256, 1, 3, 0, 0), // 地球SVO内坐标
            CoordinateType.SvoLocal,
            earthSvoOrigin
        );
        playerShip.Position = new Vector3d(50000, 100, 50000); // X=50km, Z=50km, 海拔100米

        // 加载飞船所在的Cell
        var cellId = earthSvo.GetCellId(playerShip.Position);
        earthSvo.LoadCell(cellId);

        // 检测碰撞
        bool isColliding = SvoCollisionDetector.CheckCollision(playerShip, earthSvo);
        Console.WriteLine($"飞船是否与地球体素碰撞：{isColliding}"); // 输出：False（海拔100米 > 海洋0米）

        // 飞船下降到海拔-50米（低于海平面）
        playerShip.Position = new Vector3d(50000, -50, 50000);
        isColliding = SvoCollisionDetector.CheckCollision(playerShip, earthSvo);
        Console.WriteLine($"飞船是否与地球体素碰撞：{isColliding}"); // 输出：True（碰撞海洋体素）
    }
}