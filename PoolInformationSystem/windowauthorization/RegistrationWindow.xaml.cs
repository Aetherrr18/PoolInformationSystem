using PoolInformationSystem.ApplicationData;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PoolInformation.WindowAuthorization
{
    /// <summary>
    /// Логика взаимодействия для RegistrationWindow.xaml
    /// </summary>
    public partial class RegistrationWindow : Window
    {
        private string _generatedCode = null;
        private Users _tempUser = null;
        private int _wrongCodeAttempts = 0; // Счётчик неверных попыток

        public RegistrationWindow()
        {
            InitializeComponent();
            LoadGenders();

            // Инициализация телефона с "+7"
            PhoneTextBox.Text = "+7";
            PhoneTextBox.CaretIndex = 2;
            PhoneTextBox.SelectionStart = 2;

            // Кнопка изначально НЕАКТИВНА до принятия согласия
            ActionButton.IsEnabled = false;
        }

        private void LoadGenders()
        {
            var genders = AppConnect.modelBD.Genders.ToList();
            GenderComboBox.ItemsSource = genders;
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (ActionCode == AuthState.WaitingForData)
            {
                // Шаг 1: Валидация данных и отправка кода
                if (!ValidateUserData(out var user))
                    return;

                _tempUser = user;
                GenerateAndSendCode();
            }
            else if (ActionCode == AuthState.WaitingForCode)
            {
                // Шаг 2: Проверка введённого кода
                string inputCode = CodeInputBox.Text.Trim();

                if (string.IsNullOrEmpty(inputCode) || inputCode.Length != 6)
                {
                    ShowMessage("Введите 6-значный код.", true);
                    return;
                }

                if (inputCode != _generatedCode)
                {
                    // 🔹 НЕВЕРНЫЙ КОД: очищаем поле, генерируем НОВЫЙ код, показываем его
                    _wrongCodeAttempts++;

                    CodeInputBox.Clear();
                    CodeInputBox.Focus();

                    // Генерируем новый код
                    GenerateAndSendCode();

                    ShowMessage(
                        $"Неверный код подтверждения. Попытка №{_wrongCodeAttempts}.\n" +
                        $"Сгенерирован новый код. Введите его выше.",
                        true);

                    // Показываем кнопку "Получить код заново"
                    ResendCodeButton.Visibility = Visibility.Visible;
                    return;
                }

                // ✅ Код верный — сохраняем пользователя
                try
                {
                    AppConnect.modelBD.Users.Add(_tempUser);
                    AppConnect.modelBD.SaveChanges();

                    // Запись в LoginCodes (для истории)
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

                    MessageBox.Show(
                        "Регистрация успешно завершена!\n" +
                        "Теперь для входа используйте свой номер телефона как логин.\n" +
                        "Вы будете перенаправлены на страницу авторизации.",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    GoToLogin();
                }
                catch (Exception ex)
                {
                    ShowMessage($"Ошибка при сохранении: {ex.Message}", true);
                }
            }
        }

        /// <summary>
        /// Генерация и отображение нового 6-значного кода
        /// </summary>
        private void GenerateAndSendCode()
        {
            _generatedCode = new Random().Next(100000, 1000000).ToString();

            // Показываем панель ввода кода
            CodePanel.Visibility = Visibility.Visible;
            ActionButton.Content = "ПОДТВЕРДИТЬ КОД";

            // Очищаем поле ввода и фокусируемся на нём
            CodeInputBox.Clear();
            CodeInputBox.Focus();

            // Показываем кнопку "Получить код заново"
            ResendCodeButton.Visibility = Visibility.Visible;

            // Имитация SMS
            ShowMessage(
                $"Ваш одноразовый код: {_generatedCode}\n" +
                $"(имитация SMS на {_tempUser.Phone})\n\n" +
                $"Введите код в поле выше и нажмите «ПОДТВЕРДИТЬ КОД».",
                false);
        }

        /// <summary>
        /// Обработчик кнопки "ПОЛУЧИТЬ КОД ЗАНОВО"
        /// </summary>
        private void ResendCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_tempUser == null)
            {
                ShowMessage("Ошибка: данные пользователя не найдены. Начните регистрацию заново.", true);
                return;
            }

            // Генерируем новый код
            GenerateAndSendCode();

            ShowMessage(
                $"Сгенерирован новый код: {_generatedCode}\n" +
                $"(имитация SMS на {_tempUser.Phone})",
                false);
        }

        private bool ValidateUserData(out Users user)
        {
            user = null;

            string phone = PhoneTextBox.Text?.Trim();
            string lastName = LastNameTextBox.Text?.Trim();
            string firstName = FirstNameTextBox.Text?.Trim();
            string patronymic = PatronymicTextBox.Text?.Trim();
            DateTime? birthDate = BirthDatePicker.SelectedDate;
            var gender = GenderComboBox.SelectedItem as Genders;

            // === Валидация телефона (он же логин) ===
            if (string.IsNullOrWhiteSpace(phone))
            {
                ShowMessage("Номер телефона обязателен.", true);
                return false;
            }

            if (!Regex.IsMatch(phone, @"^\+7\d{10}$"))
            {
                ShowMessage("Телефон должен быть в формате: +71234567890", true);
                return false;
            }

            // Уникальность телефона как логина
            if (AppConnect.modelBD.Users.Any(u => u.LoginUsers == phone))
            {
                ShowMessage("Пользователь с таким номером телефона уже зарегистрирован.", true);
                return false;
            }

            // === Валидация ФИО ===
            if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName))
            {
                ShowMessage("Фамилия и имя обязательны.", true);
                return false;
            }

            if (!IsValidRussianName(lastName, "Фамилия"))
                return false;
            if (!IsValidRussianName(firstName, "Имя"))
                return false;
            if (!string.IsNullOrWhiteSpace(patronymic) && !IsValidRussianName(patronymic, "Отчество"))
                return false;

            // === Дата рождения ===
            if (birthDate == null)
            {
                ShowMessage("Дата рождения обязательна.", true);
                return false;
            }

            // === Пол ===
            if (gender == null)
            {
                ShowMessage("Пол обязателен.", true);
                return false;
            }

            // === Создание временного объекта ===
            // ТЕЛЕФОН ПИШЕМ И В Phone, И В LoginUsers (он теперь = логин)
            user = new Users
            {
                LoginUsers = phone,   // ключевое изменение
                LastName = lastName,
                FirstName = firstName,
                Patronymic = string.IsNullOrWhiteSpace(patronymic) ? null : patronymic,
                BirthDate = birthDate.Value,
                GenderId = gender.GenderId,
                Phone = phone,
                RoleId = 1 // Клиент
            };

            return true;
        }

        private bool IsValidRussianName(string value, string fieldName)
        {
            if (!Regex.IsMatch(value, @"^[А-ЯЁ][а-яё\s\-']*$"))
            {
                ShowMessage($"{fieldName} должен начинаться с заглавной русской буквы и содержать только русские символы, пробелы, дефисы и апострофы.", true);
                return false;
            }

            if (Regex.IsMatch(value, @"[0-9!@#$%^&*()+=\[\]{};:,.<>?\\|]"))
            {
                ShowMessage($"{fieldName} не должен содержать цифры и специальные символы.", true);
                return false;
            }

            if (value.Length > 50)
            {
                ShowMessage($"{fieldName} не может превышать 50 символов.", true);
                return false;
            }

            return true;
        }

        private enum AuthState
        {
            WaitingForData,
            WaitingForCode
        }

        private AuthState ActionCode => CodePanel.Visibility == Visibility.Visible
            ? AuthState.WaitingForCode
            : AuthState.WaitingForData;

        private void ShowMessage(string text, bool isError)
        {
            MessageTextBlock.Text = text;
            MessageTextBlock.Foreground = isError
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Green;
            MessageTextBlock.Visibility = Visibility.Visible;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            GoToLogin();
        }

        private void GoToLogin()
        {
            var loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Защита телефона: нельзя удалить "+7", только цифры после
        private void PhoneTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox.CaretIndex <= 2 && (e.Key == Key.Back || e.Key == Key.Delete))
            {
                e.Handled = true;
            }
        }

        private void PhoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string fullText = textBox.Text.Insert(textBox.CaretIndex, e.Text);

            if (fullText.Length > 12)
            {
                e.Handled = true;
                return;
            }

            if (fullText.Length > 2 && !char.IsDigit(e.Text[0]))
            {
                e.Handled = true;
            }
        }

        // Обработчик изменения состояния чекбокса согласия
        private void ChkConsent_StateChanged(object sender, RoutedEventArgs e)
        {
            ActionButton.IsEnabled = ChkConsent.IsChecked == true;
        }

        // Обработчик клика по ссылке "[Ознакомиться с условиями...]"
        private void TbConsentLink_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var consentWindow = new PersonalDataConsentWindow();
            consentWindow.Owner = this;
            consentWindow.ShowDialog();
        }
    }
}