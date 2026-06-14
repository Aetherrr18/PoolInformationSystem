using PoolInformationSystem.ApplicationData;
using PoolInformationSystem.WindowClient;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PoolInformation
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Users _currentUser = null;
        private int _wrongCodeAttempts = 0; // 🔹 Счётчик неверных попыток

        public MainWindow()
        {
            InitializeComponent();
            AppConnect.modelBD = SwimSubscriptionsDBEntities.GetContext();

            // Инициализация поля телефона "+7"
            LoginTextBox.Text = "+7";
            LoginTextBox.CaretIndex = 2;
            LoginTextBox.SelectionStart = 2;
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (ActionCode == AuthState.WaitingForLogin)
            {
                // Шаг 1: Получить код по номеру телефона
                string phone = LoginTextBox.Text.Trim();

                if (string.IsNullOrEmpty(phone))
                {
                    ShowMessage("Введите номер телефона.", true);
                    return;
                }

                // Проверка формата телефона
                if (!Regex.IsMatch(phone, @"^\+7\d{10}$"))
                {
                    ShowMessage("Номер телефона должен быть в формате: +71234567890", true);
                    return;
                }

                // Ищем пользователя по LoginUsers (там теперь хранится телефон)
                var user = AppConnect.modelBD.Users
                    .FirstOrDefault(u => u.LoginUsers == phone);

                if (user == null)
                {
                    ShowMessage("Пользователь с таким номером телефона не найден.\nПроверьте номер или зарегистрируйтесь.", true);
                    return;
                }

                _currentUser = user;
                GenerateAndSendCode();
            }
            else if (ActionCode == AuthState.WaitingForCode)
            {
                // Шаг 2: Ввести код и войти
                string inputCode = CodeTextBox.Text.Trim();
                if (string.IsNullOrEmpty(inputCode) || inputCode.Length != 6)
                {
                    ShowMessage("Введите 6-значный код.", true);
                    return;
                }

                var validCode = AppConnect.modelBD.LoginCodes
                    .FirstOrDefault(c => c.UserId == _currentUser.UserId &&
                                         c.Code == inputCode &&
                                         c.IsUsed == false &&
                                         c.ExpiresAt > DateTime.Now);

                if (validCode == null)
                {
                    // 🔹 НЕВЕРНЫЙ КОД: очищаем поле, генерируем НОВЫЙ код
                    _wrongCodeAttempts++;

                    CodeTextBox.Clear();
                    CodeTextBox.Focus();

                    // Генерируем новый код
                    GenerateAndSendCode();

                    ShowMessage(
                        $"Неверный или просроченный код. Попытка №{_wrongCodeAttempts}.\n" +
                        $"Сгенерирован новый код. Введите его выше.",
                        true);

                    // Показываем кнопку "Получить код заново"
                    ResendCodeButton.Visibility = Visibility.Visible;
                    return;
                }

                // Помечаем как использованный
                validCode.IsUsed = true;
                AppConnect.modelBD.SaveChanges();

                // Переход по ролям
                int roleId = _currentUser.RoleId;
                Window nextWindow = null;

                if (roleId == 1) // Клиент
                    nextWindow = new WindowClient.ClientMainWindow(_currentUser);
                else if (roleId == 2) // Кассир
                    nextWindow = new WindowCashier.CashierMainWindow(_currentUser);
                else if (roleId == 3) // Администратор
                    nextWindow = new WindowAdmin.AdminMainWindow();

                if (nextWindow != null)
                {
                    nextWindow.Show();
                    this.Close();
                }
                else
                {
                    ShowMessage("Неизвестная роль пользователя.", true);
                }
            }
        }

        /// <summary>
        /// Генерация и отображение нового 6-значного кода
        /// </summary>
        private void GenerateAndSendCode()
        {
            // Удаляем старые активные коды
            var oldCodes = AppConnect.modelBD.LoginCodes
                .Where(c => c.UserId == _currentUser.UserId && c.IsUsed == false)
                .ToList();
            AppConnect.modelBD.LoginCodes.RemoveRange(oldCodes);
            AppConnect.modelBD.SaveChanges();

            // Генерация нового 6-значного кода
            string code = new Random().Next(100000, 1000000).ToString();

            var loginCode = new LoginCodes
            {
                UserId = _currentUser.UserId,
                Code = code,
                GeneratedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(10),
                IsUsed = false
            };

            AppConnect.modelBD.LoginCodes.Add(loginCode);
            AppConnect.modelBD.SaveChanges();

            // 🔹 Показываем новый код пользователю
            ShowMessage($"Ваш одноразовый код: {code}\n(в реальной системе он пришёл бы по SMS на {_currentUser.Phone})", false);

            CodePanel.Visibility = Visibility.Visible;
            LoginPanel.Visibility = Visibility.Collapsed;
            ActionButton.Content = "ВОЙТИ";

            // Очищаем поле ввода и фокусируемся на нём
            CodeTextBox.Clear();
            CodeTextBox.Focus();

            // Показываем кнопку "Получить код заново"
            ResendCodeButton.Visibility = Visibility.Visible;
        }

        private void ResendCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser != null)
            {
                GenerateAndSendCode();
            }
        }

        private enum AuthState
        {
            WaitingForLogin,
            WaitingForCode
        }

        private AuthState ActionCode => CodePanel.Visibility == Visibility.Visible ? AuthState.WaitingForCode : AuthState.WaitingForLogin;

        private void ShowMessage(string text, bool isError)
        {
            MessageTextBlock.Text = text;
            MessageTextBlock.Foreground = isError ? Brushes.Red : Brushes.Green;
            MessageTextBlock.Visibility = Visibility.Visible;
        }

        private void RegistrationTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var regWindow = new WindowAuthorization.RegistrationWindow();
            regWindow.Show();
            this.Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TrialSubscriptionTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var guestWindow = new GuestMainWindow();
            guestWindow.Show();
            this.Close();
        }

        // === ОБРАБОТЧИКИ МАСКИ ТЕЛЕФОНА ===

        // Защита от удаления "+7"
        private void LoginTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;

            // Не даём стереть "+7"
            if (textBox.CaretIndex <= 2 && (e.Key == Key.Back || e.Key == Key.Delete))
            {
                e.Handled = true;
                return;
            }

            // Не даём вставлять что-то перед "+7"
            if (textBox.SelectionStart < 2 && textBox.SelectionLength > 0)
            {
                if (e.Key == Key.Back || e.Key == Key.Delete ||
                    (e.Key == Key.V && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
                {
                    e.Handled = true;
                }
            }
        }

        // Разрешаем вводить только цифры после "+7"
        private void LoginTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;

            // Если курсор в "+7" — запрещаем ввод
            if (textBox.CaretIndex < 2)
            {
                e.Handled = true;
                return;
            }

            // Только цифры
            if (!Regex.IsMatch(e.Text, @"^[0-9]+$"))
            {
                e.Handled = true;
                return;
            }

            // Ограничение длины: "+7" + 10 цифр = 12 символов
            string fullText = textBox.Text.Insert(textBox.CaretIndex, e.Text);
            if (fullText.Length > 12)
            {
                e.Handled = true;
            }
        }

        // Контроль вставки через Ctrl+V
        private void LoginTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                string digits = new string(text.Where(char.IsDigit).ToArray());

                var textBox = sender as TextBox;
                string fullText = "+7" + digits;

                if (fullText.Length > 12)
                    fullText = fullText.Substring(0, 12);

                textBox.Text = fullText;
                textBox.CaretIndex = fullText.Length;
                e.CancelCommand();
            }
        }
    }
}
