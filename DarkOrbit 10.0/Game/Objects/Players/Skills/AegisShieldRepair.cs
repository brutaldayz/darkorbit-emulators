﻿using Ow.Game.Objects.Players.Managers;
using Ow.Managers;
using Ow.Net.netty.commands;
using Ow.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ow.Game.Objects.Players.Skills
{
    class AegisShieldRepair : Skill
    {
        public override string LootId { get => SkillManager.AEGIS_SHIELD_REPAIR; }

        public override int Duration { get => TimeManager.AEGIS_SHIELD_REPAIR_DURATION; }
        public override int Cooldown { get => TimeManager.AEGIS_SHIELD_REPAIR_COOLDOWN; }

        public AegisShieldRepair(Player player) : base(player) { }

        public List<int> targetIds = new List<int>();

        public override void Tick()
        {
            if (Active)
            {
                if (cooldown.AddMilliseconds(Duration) < DateTime.Now)
                    Disable();
                else
                    ExecuteHeal();
            }
        }

        public override void Send()
        {
            var aegisIds = new List<int> { 49, 157, 158 };

            if (aegisIds.Contains(Player.Ship.Id) && (cooldown.AddMilliseconds(Duration + Cooldown) < DateTime.Now || Player.Storage.GodMode))
            {
                Active = true;

                var target = Player.Selected;

                if (target != null)
                    targetIds.Add(target.Id);

                Player.SendCooldown(LootId, Duration, true);
                cooldown = DateTime.Now;
            }
        }

        public override void Disable()
        {
            Active = false;
            targetIds.Clear();
            Player.SendCooldown(LootId, Cooldown);

            var abilityStopCommand = AbilityStopCommand.write(104, Player.Id, targetIds);
            var abilityEffectDeActivationCommand = AbilityEffectDeActivationCommand.write(104, Player.Id, targetIds);

            foreach (var id in targetIds)
            {
                var player = GameManager.GetPlayerById(id);

                player.SendCommand(abilityStopCommand);
                player.SendCommand(abilityEffectDeActivationCommand);
            }

            Player.SendCommand(abilityStopCommand);
            Player.SendCommand(abilityEffectDeActivationCommand);
        }

        public DateTime HealTime = new DateTime();
        public void ExecuteHeal()
        {
            if (HealTime.AddSeconds(1) < DateTime.Now)
            {
                var abilityEffectActivationCommand = AbilityEffectActivationCommand.write(104, Player.Id, targetIds);

                Player.SendCommand(abilityEffectActivationCommand);
                Player.SendCommandToInRangePlayers(abilityEffectActivationCommand);

                Player.Heal(15000, 0, HealType.SHIELD);

                foreach (var id in targetIds)
                {
                    var player = GameManager.GetPlayerById(id);

                    if (player.Position.DistanceTo(Player.Position) < 700)
                        player.Heal(25000, Player.Id, HealType.SHIELD);
                }

                HealTime = DateTime.Now;
            }
        }
    }
}
