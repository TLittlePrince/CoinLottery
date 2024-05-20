using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Permissions.Commands.Permissions;
using PlayerRoles;
using Random = UnityEngine.Random;

namespace CoinLottery
{
    public class EventHandlers
    {
        private Config Config { get; }
        private readonly ArrayList _playerList = new ArrayList(); // 游玩玩家列表

        private readonly ArrayList _deadPlayerList = new ArrayList(); // 已死亡玩家列表
        // private CustomCoinEvents _customCoinEvents = new CustomCoinEvents();

        public EventHandlers(Config config)
        {
            Config = config;
        }

        /**
         * 翻硬币时调用
         */
        public void OnFlippingCoin(FlippingCoinEventArgs ev)
        {
            if (Config.Debug)
            {
                Log.Info("OnFlippingCoin");
            }

            RandomMethods.RandomMethod(ev.Player, _playerList, _deadPlayerList);
        }

        /**
         * 人物重生时调用
         */
        public void OnSpawned(SpawnedEventArgs ev)
        {
            var player = ev.Player;
            if (_deadPlayerList.Contains(player))
            {
                _deadPlayerList.Remove(player);
            }

            if (_playerList.Contains(player)) return; // 如果玩家已经在玩家列表，则不添加玩家
            _playerList.Add(player); // 添加玩家至玩家列表
            if (Config.Debug)
            {
                Log.Debug($"{player.Nickname} OnSpawned");
            }
        }


        public void OnDied(DiedEventArgs ev)
        {
            var player = ev.Player;
            _playerList.Remove(player); // 死亡时移出玩家列表
            if (_deadPlayerList.Contains(player)) return;
            _deadPlayerList.Add(player);
            if (Config.Debug)
            {
                Log.Debug($"{player.Nickname} OnDied");
            }
        }
    }

    public static class BulkShowHint
    {
        private static Player _player;
        private static string _message = "";

        public static void SetPlayer(Player player)
        {
            _player = player;
        }
        
        public static void Add(string message, string split = "\n")
        {
            _message += message + split;
        }
        
        public static void Clear()
        {
            _message = "";
        }
        
        public static void Show()
        {
            _player.ShowHint(_message);
        }
    }

    public abstract class RandomMethods
    {
        public static void RandomMethod(Player player, ArrayList playerList, ArrayList deadPlayerList)
        {
            var rand = Random.Range(0, 1000);
            BulkShowHint.SetPlayer(player);
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
                new KeyValuePair<int, Action>(5, () => RandomTurnOffZoneLight(player)),
                new KeyValuePair<int, Action>(5, () => CustomCoinEvents.DropAllItems(player)),
                new KeyValuePair<int, Action>(10, () => RandomControlOther(player, deadPlayerList)),
                new KeyValuePair<int, Action>(410, () => GiveRandomItem(player)),
                new KeyValuePair<int, Action>(545, () => BulkShowHint.Add("什么事都没发生")),
            };
            var odds = 0;
            foreach (var eventOdd in eventOdds)
            {
                odds += eventOdd.Key;
                if (rand > odds) continue;
                eventOdd.Value.Invoke();
                break;
            }

            rand = Random.Range(0, 10);
            if (rand <= 3)
            {
                CustomCoinEvents.RemoveCoin(player);
            }
            BulkShowHint.Show();
            BulkShowHint.Clear();
        }

        /**
         * 随机成为SCP
         */
        private static void TurnToRandomScp(Player player)
        {
            var roleTypeIds = new[]
            {
                RoleTypeId.Scp049, RoleTypeId.Scp096, RoleTypeId.Scp106, RoleTypeId.Scp173,
                RoleTypeId.Scp939, RoleTypeId.Scp0492
            };
            var rand = Random.Range(0, 6);
            var roleTypeId = roleTypeIds[rand];
            CustomCoinEvents.TurnToRole(player, roleTypeId);
            if (roleTypeId != RoleTypeId.Scp0492) return;
            // 给武器和设置血量为500
            player.CurrentItem = player.AddItem(ItemType.GunCOM15);
            player.MaxHealth = 500;
            player.Health = 500;
        }

        /**
         * 随机给予物品
         */
        private static void GiveRandomItem(Player player)
        {
            var excludeItemIds = new[]
            {
                ItemType.ParticleDisruptor, ItemType.GunCom45, ItemType.Ammo9x19, ItemType.Ammo12gauge,
                ItemType.Ammo44cal, ItemType.Ammo556x45, ItemType.Ammo762x39, ItemType.KeycardO5, ItemType.MicroHID,
                ItemType.Jailbird, ItemType.SCP207, ItemType.SCP268, ItemType.GunFRMG0, ItemType.GunLogicer,
                ItemType.GunShotgun, ItemType.SCP1853, ItemType.SCP018
            };
            // excludeItemIds.Contains(ItemType.Adrenaline);
            var rand = (ItemType)Random.Range(0, 54);
            var times = 0;
            while (excludeItemIds.Contains(rand) && times < excludeItemIds.Length)
            {
                rand = (ItemType)Random.Range(0, 54);
                times++;
            }

            if (excludeItemIds.Contains(rand))
            {
                BulkShowHint.Add("头好痒，好像什么都没得到");
            }

            CustomCoinEvents.GiveItem(player, rand);
        }

        /**
         * 随机目标复制物品
         */
        private static void CopyRandomPlayerInventory(Player player, ArrayList playerList)
        {
            var rand = Random.Range(0, playerList.Count);
            var randPlayer = (Player)playerList[rand];
            CustomCoinEvents.CopyOthersInventory(player, randPlayer);
        }

        /**
         * 随机区域断电
         */
        private static void RandomTurnOffZoneLight(Player player)
        {
            var rand = Random.Range(1, 5);
            CustomCoinEvents.TurnOffLight(player, (MapGeneration.FacilityZone)rand);
        }

        /**
         * 被某人夺舍
         */
        private static void RandomControlOther(Player player, ArrayList deadPlayerList)
        {
            var playerCount = deadPlayerList.Count;
            var targetPlayer = (Player)deadPlayerList[Random.Range(0, playerCount)];
            var times = 0;
            while (!targetPlayer.IsOverwatchEnabled && times < playerCount)
            {
                targetPlayer = (Player)deadPlayerList[Random.Range(0, playerCount)];
                times++;
            }

            if (!targetPlayer.IsOverwatchEnabled)
            {
                BulkShowHint.Add("有人试图夺舍你，但是失败了");
            }

            CustomCoinEvents.ControlOther(player, targetPlayer);
        }
    }

    public abstract class CustomCoinEvents
    {
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

        private static readonly IDictionary ZoneZh = new Dictionary<MapGeneration.FacilityZone, string>
        {
            { MapGeneration.FacilityZone.LightContainment, "轻收容区" },
            { MapGeneration.FacilityZone.HeavyContainment, "重收容区" },
            { MapGeneration.FacilityZone.Entrance, "办公区大门和广播室" },
            { MapGeneration.FacilityZone.Surface, "地表" },
        };

        /**
         * 转换角色
         */
        public static void TurnToRole(Player player, RoleTypeId role)
        {
            var roleName = Enum.GetName(typeof(RoleTypeId), role);
            player.DropItems();
            player.RoleManager.ServerSetRole(role, RoleChangeReason.Respawn, RoleSpawnFlags.UseSpawnpoint);
            BulkShowHint.Add($"你已经变成了 <color=#FF0000>{roleName}</color>");
            Map.ShowHint($"{player.Nickname} 变成了 <color=#F4F245>{roleName}</color>");
            Log.Info($"{player.Nickname} 变成了 {roleName}");
        }

        /**
         * 给指定物品
         */
        public static void GiveItem(Player player, ItemType itemType)
        {
            var itemName = Enum.GetName(typeof(ItemType), itemType);
            player.AddItem(itemType);
            if (itemName != null) BulkShowHint.Add($"你获得了 <color=#F4F245>{Zh[itemName]}</color>");
        }

        /**
         * 复制指定人的背包
         */
        public static void CopyOthersInventory(Player player, Player targetPlayer)
        {
            player.ClearItems();
            foreach (var inventoryItem in targetPlayer.Inventory.UserInventory.Items)
            {
                player.AddItem((ItemType)inventoryItem.Key);
            }

            BulkShowHint.Add($"你复制了{targetPlayer.Nickname}的背包");
            targetPlayer.ShowHint($"{player.Nickname}复制了你的背包");
        }

        /**
         * 指定区域断电
         */
        public static void TurnOffLight(Player player, MapGeneration.FacilityZone zone)
        {
            BulkShowHint.Add($"在<color=#E04747>{ZoneZh[zone]}</color>启用关灯事件");
            var where = RoomLightController.Instances.Where(instance => instance.Room.Zone == zone);
            foreach (var instance in where)
            {
                instance.ServerFlickerLights(10.0f);
            }
        }

        /**
         * 物品掉落
         */
        public static void DropAllItems(Player player)
        {
            player.DropItems();
            BulkShowHint.Add("你的物品掉落了");
        }

        public static void RemoveCoin(Player player)
        {
            player.RemoveHeldItem();
            BulkShowHint.Add("硬币消失了");
        }

        /**
         * 被夺舍
         */
        public static void ControlOther(Player player, Player targetPlayer)
        {
            targetPlayer.RoleManager.ServerSetRole(player.Role.Type, RoleChangeReason.Respawn);
            targetPlayer.Teleport(player.Position);
            CopyOthersInventory(targetPlayer, player);
            player.ClearInventory();
            player.Kill($"被{targetPlayer.Nickname}夺舍了");
            targetPlayer.ShowHint($"你夺舍了{player.Nickname}的身体");
        }
    }
}