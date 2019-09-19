﻿using Ow.Game.Events;
using Ow.Game.Movements;
using Ow.Game.Objects.Players.Managers;
using Ow.Game.Ticks;
using Ow.Managers;
using Ow.Net.netty.commands;
using Ow.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ow.Game.Objects.Mines
{
    abstract class Mine : Tick
    {
        public const int RANGE = 200;
        public const int ACTIVATION_TIME = 1750;

        public int TickId { get; set; }
        public int MineTypeId { get; set; }
        public string Hash { get; set; }
        public Player Player { get; set; }
        public Spacemap Spacemap { get; set; }
        public Position Position { get; set; }
        public DateTime activationTime = new DateTime();
        public bool Lance { get; set; }
        public bool Pulse { get; set; }
        public bool Detonation { get; set; }
        public bool Active = true;
        public int ExplodeRange = 275;

        public Mine(Player player, Spacemap spacemap, Position position, int mineTypeId)
        {
            Player = player;
            Spacemap = spacemap;
            Position = position;
            MineTypeId = mineTypeId;
            Lance = Player.Settings.InGameSettings.selectedFormation == DroneManager.LANCE_FORMATION;
            Pulse = false;
            //ExplodeRange += Maths.GetPercentage(ExplodeRange, SettingsManager.GetSkillPercentage("Detonation 2", Player.SkillTree.Detonation2));
            Detonation = (Player.SkillTree.Detonation1 + Player.SkillTree.Detonation2 == 5);
            Hash = Randoms.GenerateHash(16);
            Spacemap.Mines.TryAdd(Hash, this);
            activationTime = DateTime.Now;

            Program.TickManager.AddTick(this, out var tickId);
            TickId = tickId;
        }

        public abstract void Action(Player player);

        public void Explode()
        {
            foreach (var character in Spacemap.Characters.Values)
            {
                if (character is Player player && player.Position.DistanceTo(Position) < ExplodeRange)
                {
                    if (!Duel.InDuel(player) || (Duel.InDuel(player) && player.Storage.Duel?.GetOpponent(player) == Player))
                        Action(player);
                }
            }
        }

        public void Tick()
        {
            if (Active && activationTime.AddMinutes(3) < DateTime.Now)
                Remove();
        }

        public void Remove()
        {
            Active = false;
            var mine = this;

            foreach (var gameSession in GameManager.GameSessions?.Values)
            {
                var player = gameSession.Player;

                if (player.Storage.InRangeMines.ContainsKey(Hash))
                    player.Storage.InRangeMines.TryRemove(Hash, out mine);
            }

            Spacemap.Mines.TryRemove(Hash, out mine);
            GameManager.SendPacketToMap(Spacemap.Id, $"0|n|MIN|{Hash}");
            Program.TickManager.RemoveTick(this);
        }

        public byte[] GetMineCreateCommand()
        {
            return MineCreateCommand.write(Hash, Pulse, Detonation, MineTypeId, Position.Y, Position.X);
        }
    }
}
