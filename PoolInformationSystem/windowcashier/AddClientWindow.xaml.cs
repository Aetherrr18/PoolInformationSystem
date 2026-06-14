using PoolInformationSystem.ApplicationData;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PoolInformation.WindowCashier
{
    /// <summary>
    /// Логика взаимодействия для AddClientWindow.xaml
    /// </summary>
    public partial class AddClientWindow : Window
    {
        private string _generatedCode;
        private Users _tempUser;
        private bool _isCodeSent;
        private int _wrongCodeAttempts = 0;

        public AddClientWindow()
        {
            InitializeComponent();
            LoadGenders();
            InitPhone();
        }

        private void LoadGenders()
        {
            CbGender.ItemsSource = AppConnect.modelBD.Genders.ToList();
        }

        private void InitPhone()
        {
            TbPhone.Text = "+7";
            TbPhone.CaretIndex = 2;
            TbPhone.SelectionStart = 2;
        }

        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCodeSent)
            {
                if (!ValidateAndPrepareUser())
                    return;

                GenerateAndShowCode();
                _isCodeSent = true;
            }
            else
            {
                if (!VerifyAndSaveUser())
                    return;

                DialogResult = true;
                Close();
            }
        }

        private bool ValidateAndPrepareUser()
        {
            string phone = TbPhone.Text?.Trim();
            string lastName = TbLastName.Text?.Trim();
            string firstName = TbFirstName.Text?.Trim();
            string patronymic = TbPatronymic.Text?.Trim();
            DateTime? birthDate = DpBirthDate.SelectedDate;
            var gender = CbGender.SelectedItem as Genders;

            if (string.IsNullOrWhiteSpace(phone))
            { ShowMessage("Введите номер телефона."); return false; }

            if (!Regex.IsMatch(phone, @"^\+7\d{10}$"))
            { ShowMessage("Телефон должен быть в формате: +71234567890"); return false; }

            if (AppConnect.modelBD.Users.Any(u => u.LoginUsers == phone))
            { ShowMessage("Клиент с таким номером телефона уже зарегистрирован."); return false; }

            if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName))
            { ShowMessage("Фамилия и имя обязательны."); return false; }
            if (!IsValidRussianName(lastName, "Фамилия")) return false;
            if (!IsValidRussianName(firstName, "Имя")) return false;
            if (!string.IsNullOrWhiteSpace(patronymic) && !IsValidRussianName(patronymic, "Отчество")) return false;

            if (birthDate == null)
            { ShowMessage("Выберите дату рождения."); return false; }
            if (birthDate > DateTime.Today.AddYears(-5))
            { ShowMessage("Некорректная дата рождения."); return false; }

            if (gender == null)
            { ShowMessage("Выберите пол."); return false; }

            _tempUser = new Users
            {
                LoginUsers = phone,
                LastName = lastName,
                FirstName = firstName,
                Patronymic = string.IsNullOrWhiteSpace(patronymic) ? null : patronymic,
                BirthDate = birthDate.Value,
                GenderId = gender.GenderId,
                Phone = phone,
                RoleId = 1
            };

            return true;
        }

        private bool IsValidRussianName(string value, string fieldName)
        {
            if (!Regex.IsMatch(value, @"^[А-ЯЁ][а-яё\s\-']*$"))
            { ShowMessage($"{fieldName}: с заглавной буквы, только русские символы."); return false; }
            if (Regex.IsMatch(value, @"[0-9!@#$%^&*()+=\[\]{};:,.<>?\\|]"))
            { ShowMessage($"{fieldName}: без цифр и спецсимволов."); return false; }
            if (value.Length > 50)
            { ShowMessage($"{fieldName}: не более 50 символов."); return false; }
            return true;
        }

        private void GenerateAndShowCode()
        {
            _generatedCode = new Random().Next(100000, 999999).ToString();

            CodePanel.Visibility = Visibility.Visible;
            TbCodeHint.Text = $"Код отправлен на {_tempUser.Phone}\n(тест: {_generatedCode})";
            TbCodeHint.Foreground = System.Windows.Media.Brushes.DarkGreen;

            BtnAction.Content = "ПОДТВЕРДИТЬ";
            BtnResendCode.Visibility = Visibility.Visible;

            TbCode.Clear();
            TbCode.Focus();
        }

        private bool VerifyAndSaveUser()
        {
            string inputCode = TbCode.Text.Trim();
            if (string.IsNullOrEmpty(inputCode) || inputCode.Length != 6)
            { ShowMessage("Введите 6-значный код."); return false; }

            if (inputCode != _generatedCode)
            {
                _wrongCodeAttempts++;

                TbCode.Clear();
                TbCode.Focus();

                GenerateAndShowCode();

                ShowMessage($"Неверный код. Попытка №{_wrongCodeAttempts}.\n" +
                            $"Сгенерирован новый код. Введите его выше.");
                return false;
            }

            try
            {
                AppConnect.modelBD.Users.Add(_tempUser);
                AppConnect.modelBD.SaveChanges();

                var loginCode = new LoginCodes
                {
                    UserId = _tempUser.UserId,
                    Code = _generatedCode,
                    GeneratedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddMinutes(10),
                    IsUsed = true
                };
                AppConnect.modelBD.LoginCodes.Add(loginCode);
                AppConnect.modelBD.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка: {ex.Message}");
                return false;
            }
        }

        private void BtnResendCode_Click(object sender, RoutedEventArgs e)
        {
            if (_tempUser == null)
            {
                ShowMessage("Ошибка: данные клиента не заполнены.");
                return;
            }

            GenerateAndShowCode();
            ShowMessage($"Сгенерирован новый код для {_tempUser.Phone}", false);
        }

        private void ShowMessage(string text, bool isError = true)
        {
            TbMessage.Text = text;
            TbMessage.Visibility = Visibility.Visible;
            TbMessage.Foreground = isError
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Green;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]$");
        }

        private void TbPhone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox tb)
            {
                string full = tb.Text.Insert(tb.CaretIndex, e.Text);
                if (full.Length > 12) { e.Handled = true; return; }
                if (full.Length > 2 && !char.IsDigit(e.Text[0])) { e.Handled = true; }
            }
        }

        private void TbPhone_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (tb.CaretIndex <= 2 && (e.Key == Key.Back || e.Key == Key.Delete))
                    e.Handled = true;
            }
        }
    }
}