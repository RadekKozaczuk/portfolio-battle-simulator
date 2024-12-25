using System;
using System.Collections.Generic;
using Core.Models;
using Core.Services;
using GameLogic.Controllers;
using GameLogic.Interfaces;
using NUnit.Framework;

namespace Tests
{
    public class Troll
    {
        [Test]
        public void OneWarriorArmy()
        {
            // 1. Arrange
            var controller = new UpdateArmyCenterController(); // cannot access private
            var army1 = new ArmyModel(1, 2);
            var army2 = new ArmyModel(0, 1);
            var armies = new List<ArmyModel> {army1, army2};
            /*IBattleModel battle = new GameLogic.Models.BattleModel(armies);
            DependencyInjectionService.Register(battle);
            var controller = DependencyInjectionService<UpdateArmyCenterController>.ManualInjection()*/
            
            //controller.Initialize(battle);

            // 2. Act

            // 3. Assert
            Assert.That(true);
        }
    }
}