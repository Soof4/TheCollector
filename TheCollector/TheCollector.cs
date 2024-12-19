using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Data.Sqlite;
using Terraria.ID;


namespace TheCollector;



[ApiVersion(2, 1)]
public class TheCollector : TerrariaPlugin
{
    public override string Name => "The Collector";
    public override string Description => "Detects unusual item activities";
    public override string Author => "Soofa";
    public override Version Version => new Version(1, 0, 0);
    private Dictionary<int, int> _onlinePlayers = new Dictionary<int, int>();
    private DateTime _lastTimeUpdateTime = DateTime.UtcNow;
    public DatabaseManager DbManager = new(new SqliteConnection("Data Source=" + Path.Combine(TShock.SavePath, "TheCollector.sqlite")));
    public CSVManager CvsManager = new CSVManager();
    public TheCollector(Main game) : base(game) { }


    public override void Initialize()
    {
        CvsManager.Initialize();

        GetDataHandlers.PlayerSlot += OnPlayerSlot;
        ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin);
        ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
    }

    private void OnGameUpdate(EventArgs args)
    {
        if ((DateTime.UtcNow - _lastTimeUpdateTime).TotalMinutes >= 1)
        {
            UpdateOnlineTimes();
        }
    }

    private void OnServerLeave(LeaveEventArgs args)
    {
        if (_onlinePlayers.ContainsKey(args.Who) && TShock.Players[args.Who] != null)
        {
            DbManager.SavePlayer(TShock.Players[args.Who].Name, _onlinePlayers[args.Who]);
            _onlinePlayers.Remove(args.Who);
        }
    }

    private void OnServerJoin(JoinEventArgs args)
    {
        if (!_onlinePlayers.ContainsKey(args.Who) && TShock.Players[args.Who] != null)
        {
            try
            {
                int mins = DbManager.GetPlayerTime(TShock.Players[args.Who].Name);
                _onlinePlayers.Add(args.Who, mins);
            }
            catch
            {
                DbManager.InsertPlayer(TShock.Players[args.Who].Name);
                _onlinePlayers.Add(args.Who, 0);
            }
        }
    }

    private void OnPlayerSlot(object? sender, GetDataHandlers.PlayerSlotEventArgs args)
    {
        if (args.Stack == 0 || !args.Player.IsLoggedIn || args.Player.Group.Name == TShock.Config.Settings.DefaultGuestGroupName) return;

        List<object> row = new List<object>() {
            _onlinePlayers[args.PlayerId],
            args.Slot,
            args.Type,
            args.Stack,
            ContentSamples.ItemsByType[args.Type].ammo != 0,
            ContentSamples.ItemsByType[args.Type].OriginalRarity,
            GetBossProgression()
        };

        CvsManager.Add(row);
    }

    private void UpdateOnlineTimes()
    {
        if ((DateTime.UtcNow - _lastTimeUpdateTime).TotalMinutes < 1) return;

        foreach (var kvp in _onlinePlayers)
        {
            _onlinePlayers[kvp.Key] += (int)(DateTime.UtcNow - _lastTimeUpdateTime).TotalMinutes;
            DbManager.SavePlayer(TShock.Players[kvp.Key].Name, _onlinePlayers[kvp.Key]);
        }

        _lastTimeUpdateTime = DateTime.UtcNow;
    }

    private int GetBossProgression()
    {
        if (NPC.downedMoonlord) return 17;
        if (NPC.downedAncientCultist) return 16;
        if (NPC.downedEmpressOfLight) return 15;
        if (NPC.downedFishron) return 14;
        if (NPC.downedGolemBoss) return 13;
        if (NPC.downedPlantBoss) return 12;
        if (NPC.downedMechBoss3) return 11;
        if (NPC.downedMechBoss2) return 10;
        if (NPC.downedMechBoss1) return 9;
        if (NPC.downedQueenSlime) return 8;
        if (Main.hardMode) return 7;
        if (NPC.downedQueenBee) return 6;
        if (NPC.downedBoss3) return 5;
        if (NPC.downedDeerclops) return 4;
        if (NPC.downedBoss2) return 3;
        if (NPC.downedBoss1) return 2;
        if (NPC.downedSlimeKing) return 1;
        return 0;
    }
}
