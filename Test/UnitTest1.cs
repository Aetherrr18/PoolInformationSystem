using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.RegularExpressions;

namespace Test
{
    [TestClass]
    public class AuthorizationTests
    {
        // Вспомогательный метод проверки формата номера телефона
        private bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            return Regex.IsMatch(phone, @"^\+7\d{10}$");
        }

        // Вспомогательный метод генерации 6-значного кода
        private string GenerateCode()
        {
            return new Random().Next(100000, 1000000).ToString();
        }

        // Вспомогательный метод проверки срока действия кода
        private bool IsCodeValid(DateTime generatedAt, int lifetimeMinutes)
        {
            DateTime expiresAt = generatedAt.AddMinutes(lifetimeMinutes);
            return DateTime.Now <= expiresAt;
        }

        // Вспомогательный метод получения названия роли
        private string GetRoleName(int roleId)
        {
            if (roleId == 1) return "Клиент";
            if (roleId == 2) return "Кассир";
            if (roleId == 3) return "Администратор";
            return "Неизвестно";
        }


        //Тест 1. Проверяет, что корректный номер телефона в формате +7XXXXXXXXXX 
        [TestMethod]
        public void Login_ValidPhone_ReturnsTrue()
        {
            // Arrange
            string phone = "+79001234561";

            // Act
            bool result = IsValidPhone(phone);

            // Assert
            Assert.IsTrue(result, "Корректный номер телефона должен быть принят.");
        }

        // Тест 2. Проверяет, что номер телефона без префикса +7 отклоняется системой,
        [TestMethod]
        public void Login_InvalidPhone_WithoutPrefix_ReturnsFalse()
        {
            string phone = "9001234561";
            bool result = IsValidPhone(phone);
            Assert.IsFalse(result, "Номер без префикса +7 должен быть отклонён.");
        }

        // Тест 3. Проверяет, что номер телефона с недостаточным количеством цифр (менее 10)
        [TestMethod]
        public void Login_InvalidPhone_TooShort_ReturnsFalse()
        {
            string phone = "+790012345";
            bool result = IsValidPhone(phone);
            Assert.IsFalse(result, "Номер с недостаточным количеством цифр должен быть отклонён.");
        }

        // Тест 4. Проверяет, что метод генерации создаёт одноразовый код подтверждения,
        [TestMethod]
        public void GenerateCode_ReturnsSixDigits()
        {
            string code = GenerateCode();
            Assert.AreEqual(6, code.Length, "Код подтверждения должен содержать ровно 6 цифр.");
            Assert.IsTrue(Regex.IsMatch(code, @"^\d{6}$"), "Код должен состоять только из цифр.");
        }

        // Тест 5. Проверяет, что только что сгенерированный код считается действительным,
        [TestMethod]
        public void Code_Validation_NotExpired_ReturnsTrue()
        {
            DateTime generatedAt = DateTime.Now;
            int lifetimeMinutes = 10;
            bool result = IsCodeValid(generatedAt, lifetimeMinutes);
            Assert.IsTrue(result, "Только что сгенерированный код должен быть действителен.");
        }

        // Тест 6. Проверяет, что код, сгенерированный 15 минут назад при сроке действия 10 минут,
        [TestMethod]
        public void Code_Validation_Expired_ReturnsFalse()
        {
            DateTime generatedAt = DateTime.Now.AddMinutes(-15);
            int lifetimeMinutes = 10;
            bool result = IsCodeValid(generatedAt, lifetimeMinutes);
            Assert.IsFalse(result, "Просроченный код должен быть отклонён.");
        }

        // Тест 7. Проверяет корректность работы роли 1
        [TestMethod]
        public void Role_Check_ClientRole_ReturnsCorrectName()
        {
            int roleId = 1;
            string roleName = GetRoleName(roleId);
            Assert.AreEqual("Клиент", roleName, "Роль с ID=1 должна соответствовать 'Клиент'.");
        }

        // Тест 8. Проверяет корректность работы роли 2
        [TestMethod]
        public void Role_Check_CashierRole_ReturnsCorrectName()
        {
            int roleId = 2;
            string roleName = GetRoleName(roleId);
            Assert.AreEqual("Кассир", roleName, "Роль с ID=2 должна соответствовать 'Кассир'.");
        }

        // Тест 9. Проверяет корректность работы справочника ролм 3
        [TestMethod]
        public void Role_Check_AdminRole_ReturnsCorrectName()
        {
            int roleId = 3;
            string roleName = GetRoleName(roleId);
            Assert.AreEqual("Администратор", roleName, "Роль с ID=3 должна соответствовать 'Администратор'.");
        }

        // Тест 10. Проверяет, что пустая строка в качестве номера телефона отклоняется,
        [TestMethod]
        public void Login_EmptyPhone_ReturnsFalse()
        {
            string phone = "";
            bool result = IsValidPhone(phone);
            Assert.IsFalse(result, "Пустой номер телефона должен быть отклонён.");
        }

        // Тест 11. Проверяет, что номер телефона с буквами или спецсимволами отклоняется
        [TestMethod]
        public void Login_InvalidPhone_WithLetters_ReturnsFalse()
        {
            string phone = "+7900ABC4561";
            bool result = IsValidPhone(phone);
            Assert.IsFalse(result, "Номер с буквами должен быть отклонён.");
        }

        // Тест 12. Проверяет, что номер телефона с лишними цифрами (более 10 после +7) отклоняется
        [TestMethod]
        public void Login_InvalidPhone_TooLong_ReturnsFalse()
        {
            string phone = "+7900123456123";
            bool result = IsValidPhone(phone);
            Assert.IsFalse(result, "Номер с лишними цифрами должен быть отклонён.");
        }
    }
}