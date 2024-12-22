using System.Collections.Generic;
using Core.Models;
using GameLogic.Controllers;
using GameLogic.Models;
using NUnit.Framework;

namespace Tests
{
    class MemoryLayoutTests
    {
        [Test]
        public void OneWarriorArmy()
        {
            // 1. Arrange
            var army = new ArmyModel(1, 0);
            var armies = new List<ArmyModel> {army};
            var initController = new InitializeBattleModelController();

            // 2. Act
            MemoryLayoutModel[] memory = initController.CreateMemoryLayoutV2(armies);

            // 3. Assert
            Assert.That(memory.Length == 1, "One army should result in one MemoryLayoutModel instance.");
            Assert.That(memory[0].AllyIndex == 0);
            Assert.That(memory[0].AllyLength == 1);
        }

        [Test]
        public void OneWarriorOneArcherArmy()
        {
            // 1. Arrange
            var army = new ArmyModel(1, 1);
            var armies = new List<ArmyModel> {army};
            var initController = new InitializeBattleModelController();

            // 2. Act
            MemoryLayoutModel[] memory = initController.CreateMemoryLayoutV2(armies);

            // 3. Assert
            Assert.That(memory.Length == 1, "One army should result in one MemoryLayoutModel instance.");
            Assert.That(memory[0].AllyIndex == 0);
            Assert.That(memory[0].AllyLength == 2);
        }

        [Test]
        public void TwoArmies_1()
        {
            // 1. Arrange
            var army1 = new ArmyModel(1, 0);
            var army2 = new ArmyModel(0, 1);
            var armies = new List<ArmyModel> {army1, army2};
            var initController = new InitializeBattleModelController();

            // 2. Act
            MemoryLayoutModel[] memory = initController.CreateMemoryLayoutV2(armies);

            // 3. Assert
            Assert.That(memory.Length == 2, "One army with one warrior should result in only Memory Layout object.");
            Assert.That(memory[0].AllyIndex == 0);
            Assert.That(memory[0].AllyLength == 1);
        }

        [Test]
        public void ThreeArmies_2()
        {
            // 1. Arrange
            var oneWarriorArmy = new ArmyModel(1, 1);
            var armies = new List<ArmyModel> {oneWarriorArmy};
            var initController = new InitializeBattleModelController();

            // 2. Act
            MemoryLayoutModel[] memory = initController.CreateMemoryLayoutV2(armies);

            // 3. Assert
            Assert.That(memory.Length == 1, "One army with one warrior should result in only Memory Layout object.");
            Assert.That(memory[0].AllyIndex == 0);
            Assert.That(memory[0].AllyLength == 1);
        }
    }
}