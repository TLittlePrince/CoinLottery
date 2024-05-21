using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using Random = UnityEngine.Random;

namespace CoinLottery
{
    public class EventHandlers
    {
        /** 配置 */
        private Config Config { get; }

        /** 游玩玩家列表 */
        private readonly ArrayList _playerList = new ArrayList();

        /** 死亡玩家列表 */
        private readonly ArrayList _deadPlayerList = new ArrayList();

        public EventHandlers(Config config)
        {
            Config = config;
        }

        /// <summary>
        /// 投硬币时调用
        /// </summary>
        /// <param name="ev">事件</param>
        public void OnFlippingCoin(FlippingCoinEventArgs ev)
        {
            if (Config.Debug)
            {
                Log.Info("OnFlippingCoin");
            }

            RandomMethods.RandomMethod(ev.Player, _playerList, _deadPlayerList);
        }

        /// <summary>
        /// 玩家生成时调用
        /// </summary>
        /// <param name="ev">事件</param>
        public void OnSpawned(SpawnedEventArgs ev)
        {
            var player = ev.Player;
            if (_deadPlayerList.Contains(player))
            {
                _deadPlayerList.Remove(player);
            }

            if (_playerList.Contains(player)) return; // 如果玩家已经在玩家列表，则不添加玩家
            _playerList.Add(player); // 添加玩家至玩家列表
            if (!Config.Debug) return;
            Log.Info($"OnSpawned: _playerList: {GetListString(_playerList)}");
            Log.Info($"OnSpawned: _deadPlayerList: {GetListString(_deadPlayerList)}");
            Log.Info($"OnSpawned: {player.Nickname} OnSpawned");
        }

        /// <summary>
        /// 玩家死亡时调用
        /// </summary>
        /// <param name="ev">事件</param>
        public void OnDied(DiedEventArgs ev)
        {
            var isSuccessful = PlayerOut(ev.Player);
            if (!Config.Debug) return;
            Log.Info($"OnDied 移除玩家: {isSuccessful}");
        }
        
        public void OnLeft(LeftEventArgs ev)
        {
            if (_playerList.Contains(ev.Player))
            {
                _playerList.Remove(ev.Player);
            }

            if (_deadPlayerList.Contains(ev.Player))
            {
                _deadPlayerList.Remove(ev.Player);
            }

            if (!Config.Debug) return;
            Log.Info($"OnLeft: _playerList: {GetListString(_playerList)}");
            Log.Info($"OnLeft: _deadPlayerList: {GetListString(_deadPlayerList)}");
        }
        
        private bool PlayerOut(Player player)
        {
            _playerList.Remove(player); // 死亡时移出玩家列表
            if (_deadPlayerList.Contains(player)) return false;
            _deadPlayerList.Add(player);
            if (!Config.Debug) return true;
            Log.Info($"PlayerOut: _playerList: {GetListString(_playerList)}");
            Log.Info($"PlayerOut: _deadPlayerList: {GetListString(_deadPlayerList)}");
            Log.Info($"PlayerOut: {player.Nickname}");
            return true;
        }
        
        private static string GetListString(ArrayList list)
        {
            return list.Cast<object>().Aggregate("", (current, player) => current + $"{((Player)player).Nickname}, ");
        }
    }

    /// <summary>
    /// 批量显示提示，用于在多个事件中一次性显示多个提示而不会被覆盖
    /// </summary>
    public static class BulkShowHint
    {
        private static Player _player;
        private static string _message = "";

        /// <summary>
        /// 设置显示提示的玩家
        /// </summary>
        /// <param name="player">要显示提示的玩家</param>
        public static void SetPlayer(Player player)
        {
            _player = player;
        }

        /// <summary>
        /// 添加提示
        /// </summary>
        /// <param name="message">提示信息</param>
        /// <param name="endSplit">结尾分隔符（默认换行）</param>
        public static void Add(string message, string endSplit = "\n")
        {
            _message += message + endSplit;
        }

        /// <summary>
        /// 清空加入的所有提示
        /// </summary>
        public static void Clear()
        {
            _message = "";
        }

        /// <summary>
        /// 显示加入的所有提示
        /// </summary>
        public static void Show()
        {
            _player.ShowHint(_message);
        }
    }

    /// <summary>
    /// 随机事件
    /// </summary>
    public abstract class RandomMethods
    {
        /// <summary>
        /// 随机事件
        /// </summary>
        /// <param name="player">当前玩家（投硬币的）</param>
        /// <param name="playerList">活着的玩家列表</param>
        /// <param name="deadPlayerList">已死亡的玩家列表</param>
        public static void RandomMethod(Player player, ArrayList playerList, ArrayList deadPlayerList)
        {
            var rand = Random.Range(0, 1000); // 生成0-999的随机数
            BulkShowHint.SetPlayer(player); // 设置显示提示的玩家
            // 事件概率，概率单位为千分之一 例子： new KeyValuePair<int, Action>(概率, () => 要执行的事件),
            var eventOdds = new List<KeyValuePair<int, Action>>
            {
                new KeyValuePair<int, Action>(5, () => TurnToRandomScp(player)),
                new KeyValuePair<int, Action>(5, () => CopyRandomPlayerInventory(player, playerList)),
                new KeyValuePair<int, Action>(5, () => CustomCoinEvents.GiveItem(player, ItemType.SCP1853)),
                new KeyValuePair<int, Action>(5, () => CustomCoinEvents.GiveItem(player, ItemType.MicroHID)),
                new KeyValuePair<int, Action>(5, () => CustomCoinEvents.GiveItem(player, ItemType.SCP268)),
                new KeyValuePair<int, Action>(5, () => CustomCoinEvents.GiveItem(player, ItemType.KeycardO5)),
                new KeyValuePair<int, Action>(5, () => CustomCoinEvents.GiveItem(player, ItemType.Jailbird)),
                new KeyValuePair<int, Action>(5, () => CustomCoinEvents.GiveItem(player, ItemType.SCP207)),
                new KeyValuePair<int, Action>(5, RandomTurnOffZoneLight),
                new KeyValuePair<int, Action>(5, () => CustomCoinEvents.DropAllItems(player)),
                new KeyValuePair<int, Action>(10, () => RandomControlOther(player, deadPlayerList)),
                new KeyValuePair<int, Action>(410, () => GiveRandomItem(player)),
                new KeyValuePair<int, Action>(545, () => BulkShowHint.Add("什么事都没发生")),
            };
            // 遍历事件概率，执行事件
            var odds = 0;
            foreach (var eventOdd in eventOdds)
            {
                odds += eventOdd.Key; // 累加概率
                if (rand > odds) continue; // 如果随机数大于累加概率则继续循环
                eventOdd.Value.Invoke(); // 执行事件
                break; // 执行完事件后跳出循环
            }

            // 有一定概率移除硬币
            rand = Random.Range(0, 10);
            if (rand <= 3)
            {
                CustomCoinEvents.RemoveCoin(player);
            }

            // 显示前面加入的所有提示
            BulkShowHint.Show();
            BulkShowHint.Clear();
        }

        /// <summary>
        /// 变成随机SCP
        /// </summary>
        /// <param name="player">当前玩家（投硬币的）</param>
        private static void TurnToRandomScp(Player player)
        {
            // 可以变成的SCP角色id列表
            var roleTypeIds = new[]
            {
                RoleTypeId.Scp049, RoleTypeId.Scp096, RoleTypeId.Scp106, RoleTypeId.Scp173,
                RoleTypeId.Scp939, RoleTypeId.Scp0492
            };
            // var rand = Random.Range(0, 6);
            var roleTypeId = roleTypeIds.ToArray().GetRandomValue();
            CustomCoinEvents.TurnToRole(player, roleTypeId);
            if (roleTypeId != RoleTypeId.Scp0492) return;  // 如果不是SCP-049-2则不执行下面的代码
            // 给武器和设置血量为500
            player.CurrentItem = player.AddItem(ItemType.GunCOM15);
            player.MaxHealth = 500;
            player.Health = 500;
        }

        /// <summary>
        /// 随机给玩家物品
        /// </summary>
        /// <param name="player">当前玩家（投硬币的）</param>
        private static void GiveRandomItem(Player player)
        {
            // 排除的物品id列表
            var excludeItemIds = new[]
            {
                ItemType.ParticleDisruptor, ItemType.GunCom45, ItemType.Ammo9x19, ItemType.Ammo12gauge,
                ItemType.Ammo44cal, ItemType.Ammo556x45, ItemType.Ammo762x39, ItemType.KeycardO5, ItemType.MicroHID,
                ItemType.Jailbird, ItemType.SCP207, ItemType.SCP268, ItemType.GunFRMG0, ItemType.GunLogicer,
                ItemType.GunShotgun, ItemType.SCP1853, ItemType.SCP018
            };
            var rand = (ItemType)Random.Range(0, 54);
            var times = 0;  // 防止死循环
            // 随机物品id，直到不在排除的物品id列表中
            while (excludeItemIds.Contains(rand) && times < excludeItemIds.Length)
            {
                rand = (ItemType)Random.Range(0, 54);
                times++;
            }

            // 如果随机物品id在排除的物品id列表中，则提示什么都没得到，否则给物品
            if (excludeItemIds.Contains(rand))
            {
                BulkShowHint.Add("头好痒，好像什么都没得到");
                Log.Info("GiveRandomItem: 随机物品获取失败");
                return;
            }

            CustomCoinEvents.GiveItem(player, rand);
        }

        /// <summary>
        /// 复制随机玩家的背包
        /// </summary>
        /// <param name="player">当前玩家（投硬币的）</param>
        /// <param name="simplePlayerList">要被随机选取的玩家列表</param>
        private static void CopyRandomPlayerInventory(Player player, ArrayList simplePlayerList)
        {
            var playerListCount = simplePlayerList.Count;  // 玩家数量
            /*var rand = Random.Range(0, playerListCount);
            var randPlayer = (Player)simplePlayerList[rand];*/
            var randPlayer = (Player)simplePlayerList.ToArray().GetRandomValue();
            var times = 0;  // 防止死循环
            // 随机玩家，直到不是当前玩家
            while (randPlayer.Id == player.Id && times < playerListCount)
            {
                // rand = Random.Range(0, playerListCount);
                // randPlayer = (Player)simplePlayerList[rand];
                randPlayer = (Player)simplePlayerList.ToArray().GetRandomValue();
                times++;
            }

            // 如果随机目标是当前玩家，则提示什么都没发生，否则复制randPlayer的物品
            if (randPlayer.Id == player.Id)
            {
                BulkShowHint.Add("你感受到了一阵空间波动，但什么都没发生");
                Log.Info("CopyRandomPlayerInventory: 随机目标复制物品失败");
                return;
            }

            CustomCoinEvents.CopyOthersInventory(player, randPlayer);
        }

        /// <summary>
        /// 随机关闭区域灯光
        /// </summary>
        private static void RandomTurnOffZoneLight()
        {
            var rand = Random.Range(1, 5);
            CustomCoinEvents.TurnOffLight((MapGeneration.FacilityZone)rand);
        }

        /// <summary>
        /// 随机控制（夺舍）其他玩家
        /// </summary>
        /// <param name="player">当前玩家（投硬币的）</param>
        /// <param name="simplePlayerList">要被随机选取的玩家列表</param>
        private static void RandomControlOther(Player player, ArrayList simplePlayerList)
        {
            var playerCount = simplePlayerList.Count;
            if (playerCount == 0)
            {
                BulkShowHint.Add("你试图夺舍，但是没有目标");
                return;
            }

            var targetPlayer = (Player)simplePlayerList.ToArray().GetRandomValue();
            var times = 0;
            
            // 随机玩家，直到玩家为观察者或者遍历完所有玩家
            while (!targetPlayer.IsOverwatchEnabled && times < playerCount)
            {
                targetPlayer = (Player)simplePlayerList.ToArray().GetRandomValue();
                times++;
            }

            // 如果玩家为观察者，则提示夺舍失败，否则控制（夺舍）玩家
            if (!targetPlayer.IsOverwatchEnabled)
            {
                BulkShowHint.Add("有人试图夺舍你，但是失败了");
                return;
            }

            CustomCoinEvents.ControlOther(player, targetPlayer);
        }
    }

    /// <summary>
    /// 自定义硬币事件
    /// </summary>
    public abstract class CustomCoinEvents
    {
        /** 物品中文名 */
        private static readonly IDictionary Zh = new Dictionary<string, string>
        {
            { "KeycardJanitor", "清洁工钥匙卡" },
            { "KeycardScientist", "科学家钥匙卡" },
            { "KeycardResearchCoordinator", "研究主管钥匙卡" },
            { "KeycardZoneManager", "区域总监钥匙卡" },
            { "KeycardGuard", "警卫钥匙卡" },
            { "KeycardMTFPrivate", "MTF列兵钥匙卡" },
            { "KeycardContainmentEngineer", "收容工程师钥匙卡" },
            { "KeycardMTFOperative", "MTF特工钥匙卡" },
            { "KeycardMTFCaptain", "MTF指挥官钥匙卡" },
            { "KeycardFacilityManager", "设施总监钥匙卡" },
            { "KeycardChaosInsurgency", "混沌卡" },
            { "KeycardO5", "黑卡" },
            { "Radio", "对讲机" },
            { "GunCOM15", "Com-15" },
            { "Medkit", "急救包" },
            { "Flashlight", "手电筒" },
            { "MicroHID", "电炮" },
            { "SCP500", "万能药" },
            { "SCP207", "可口可乐" },
            { "GunE11SR", "MTF特工步枪" },
            { "GunCrossvec", "维克托冲锋枪" },
            { "GunFSP9", "警卫冲锋枪" },
            { "GunLogicer", "混沌轻机枪" },
            { "GrenadeHE", "高爆手雷" },
            { "GrenadeFlash", "闪光弹" },
            { "GunCOM18", "Com-18" },
            { "SCP018", "弹力球" },
            { "SCP268", "帽子" },
            { "Adrenaline", "肾上腺素" },
            { "Painkillers", "止痛药" },
            { "Coin", "幸运硬币!" },
            { "ArmorLight", "轻甲" },
            { "ArmorCombat", "中甲" },
            { "ArmorHeavy", "重甲" },
            { "GunRevolver", "左轮手枪" },
            { "GunAK", "AK47步枪" },
            { "GunShotgun", "霰弹枪" },
            { "SCP330", "糖" },
            { "SCP2176", "鬼灯" },
            { "SCP244a", "冰壶A" },
            { "SCP244b", "冰壶B" },
            { "SCP1853", "洗手液" },
            { "ParticleDisruptor", "分子裂解者" },
            { "GunCom45", "Com-45" },
            { "SCP1576", "死者交谈" },
            { "Jailbird", "囚鸟" },
            { "AntiSCP207", "可口可乐-樱桃味" },
            { "GunFRMG0", "狗官枪" },
            { "GunA7", "A7步枪" },
            { "Lantern", "手提灯" },
        };

        /** 区域中文名 */
        private static readonly IDictionary ZoneZh = new Dictionary<MapGeneration.FacilityZone, string>
        {
            { MapGeneration.FacilityZone.LightContainment, "轻收容区" },
            { MapGeneration.FacilityZone.HeavyContainment, "重收容区" },
            { MapGeneration.FacilityZone.Entrance, "办公区大门和广播室" },
            { MapGeneration.FacilityZone.Surface, "地表" },
        };

        /// <summary>
        /// 变成指定角色
        /// </summary>
        /// <param name="player">当前玩家（投硬币的）</param>
        /// <param name="role">变成的角色id</param>
        public static void TurnToRole(Player player, RoleTypeId role)
        {
            var roleName = Enum.GetName(typeof(RoleTypeId), role);  // 获取角色名
            DropAllItems(player);  // 丢弃所有物品
            player.RoleManager.ServerSetRole(role, RoleChangeReason.Respawn, RoleSpawnFlags.UseSpawnpoint);  // 变成指定角色
            BulkShowHint.Add($"你已经变成了 <color=#FF0000>{roleName}</color>");
            Map.ShowHint($"{player.Nickname} 变成了 <color=#F4F245>{roleName}</color>");
            Log.Info($"TurnToRole: {player.Nickname} 变成了 {roleName}");
        }

        /// <summary>
        /// 给玩家物品
        /// </summary>
        /// <param name="player">当前玩家（投硬币的）</param>
        /// <param name="itemType">给的物品类型</param>
        public static void GiveItem(Player player, ItemType itemType)
        {
            var itemName = Enum.GetName(typeof(ItemType), itemType);  // 获取物品名
            player.AddItem(itemType);  // 给物品
            if (itemName == null) return;  // 如果物品名为空则不执行下面的代码
            BulkShowHint.Add($"你获得了 <color=#F4F245>{Zh[itemName]}</color>");
            Log.Info($"GiveItem: {player.Nickname} 获得了 {Zh[itemName]}");
        }

        /// <summary>
        /// 复制指定玩家的背包
        /// </summary>
        /// <param name="player">当前玩家（投硬币的）</param>
        /// <param name="targetPlayer">目标玩家（被复制的）</param>
        public static void CopyOthersInventory(Player player, Player targetPlayer)
        {
            player.ClearItems();  // 清空所有物品
            
            // 复制目标玩家的物品给当前玩家
            foreach (var inventoryItem in targetPlayer.Inventory.UserInventory.Items)
            {
                player.AddItem((ItemType)inventoryItem.Key);
            }

            BulkShowHint.Add($"你复制了{targetPlayer.Nickname}的背包");
            targetPlayer.ShowHint($"{player.Nickname}复制了你的背包");
            Log.Info($"CopyOthersInventory: {player.Nickname} 复制了 {targetPlayer.Nickname} 的背包");
        }

        /// <summary>
        /// 关闭指定区域的灯光
        /// </summary>
        /// <param name="zone">指定的区域</param>
        public static void TurnOffLight(MapGeneration.FacilityZone zone)
        {
            var where = RoomLightController.Instances.Where(instance => instance.Room.Zone == zone);  // 获取指定区域的灯光
            
            // 关闭指定区域的灯光
            foreach (var instance in where)
            {
                instance.ServerFlickerLights(10.0f);
            }

            BulkShowHint.Add($"在<color=#E04747>{ZoneZh[zone]}</color>启用关灯事件");
            Log.Info($"TurnOffLight: 在 {ZoneZh[zone]} 启用关灯事件");
        }

        /// <summary>
        /// 让玩家掉落所有物品
        /// </summary>
        /// <param name="player">当前玩家（投硬币的）</param>
        public static void DropAllItems(Player player)
        {
            player.DropItems();  // 丢弃所有物品
            BulkShowHint.Add("你的物品掉落了");
            Log.Info($"DropAllItems: {player.Nickname} 的物品掉落了");
        }

        /// <summary>
        /// 移除玩家手中的硬币
        /// </summary>
        /// <param name="player">当前玩家（投硬币的）</param>
        public static void RemoveCoin(Player player)
        {
            player.RemoveHeldItem();  // 移除手中的物品
            BulkShowHint.Add("硬币消失了");
            Log.Info("RemoveCoin: 硬币消失了");
        }

        /// <summary>
        /// 指定玩家夺舍自己
        /// </summary>
        /// <param name="player">当前玩家（投硬币的）</param>
        /// <param name="targetPlayer">目标玩家</param>
        public static void ControlOther(Player player, Player targetPlayer)
        {
            targetPlayer.RoleManager.ServerSetRole(player.Role.Type, RoleChangeReason.Respawn);  // 让目标玩家变成当前玩家的角色
            targetPlayer.Teleport(player.Position);  // 传送目标玩家到当前玩家的位置
            CopyOthersInventory(targetPlayer, player);  // 复制当前玩家的背包给目标玩家
            player.ClearInventory();  // 清空当前玩家的背包
            player.Kill($"被{targetPlayer.Nickname}夺舍了");  // 杀死当前玩家
            targetPlayer.ShowHint($"你夺舍了{player.Nickname}的身体");  // 提示目标玩家夺舍成功
            Log.Info($"ControlOther: {player.Nickname} 被 {targetPlayer.Nickname} 夺舍了");  // 记录日志
        }
    }
}