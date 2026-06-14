using PoolInformationSystem.ApplicationData;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PoolInformation.WindowAdmin
{
    /// <summary>
    /// Логика взаимодействия для AddEditUserWindow.xaml
    /// </summary>
    public partial class AddEditUserWindow : Window
    {
        private Users _currentUser = null;

        public AddEditUserWindow(Users user = null)
        {
            InitializeComponent();
            _currentUser = user;
            LoadComboBoxes();

            if (_currentUser != null)
            {
                // Режим редактирования
                TbPhone.Text = _currentUser.Phone ?? "+7";
                TbLastName.Text = _currentUser.LastName;
                TbFirstName.Text = _currentUser.FirstName;
                TbPatronymic.Text = _currentUser.Patronymic ?? "";
                DpBirthDate.SelectedDate = _currentUser.BirthDate;
                CbGender.SelectedValue = _currentUser.GenderId;
                CbRole.SelectedValue = _currentUser.RoleId;
            }
            else
            {
                // Режим добавления — инициализируем телефон "+7"
                TbPhone.Text = "+7";
            }

            TbPhone.CaretIndex = TbPhone.Text.Length;
        }

        private void LoadComboBoxes()
        {
            var genders = AppConnect.modelBD.Genders.ToList();
            CbGender.ItemsSource = genders;

            // Только кассиры и администраторы
            var roles = AppConnect.modelBD.Roles
                .Where(r => r.RoleId == 2 || r.RoleId == 3)
                .ToList();
            CbRole.ItemsSource = roles;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string phone = TbPhone.Text?.Trim();

            // === Валидация ===
            if (string.IsNullOrWhiteSpace(phone))
            { ShowMessage("Введите номер телефона."); return; }

            if (!Regex.IsMatch(phone, @"^\+7\d{10}$"))
            { ShowMessage("Телефон должен быть в формате: +71234567890"); return; }

            if (string.IsNullOrWhiteSpace(TbLastName.Text))
            { ShowMessage("Введите фамилию."); return; }

            if (string.IsNullOrWhiteSpace(TbFirstName.Text))
            { ShowMessage("Введите имя."); return; }

            if (DpBirthDate.SelectedDate == null)
            { ShowMessage("Выберите дату рождения."); return; }

            if (CbGender.SelectedItem == null)
            { ShowMessage("Выберите пол."); return; }

            if (CbRole.SelectedItem == null)
            { ShowMessage("Выберите роль."); return; }

            // === Проверка уникальности телефона как логина ===
            // При редактировании исключаем текущего пользователя
            bool phoneExists;
            if (_currentUser == null)
            {
                phoneExists = AppConnect.modelBD.Users.Any(u => u.LoginUsers == phone);
            }
            else
            {
                phoneExists = AppConnect.modelBD.Users.Any(u => u.LoginUsers == phone && u.UserId != _currentUser.UserId);
            }

            if (phoneExists)
            {
                ShowMessage("Пользователь с таким номером телефона уже зарегистрирован.");
                return;
            }

            try
            {
                if (_currentUser == null)
                {
                    // 🔹 ДОБАВЛЕНИЕ: телефон пишем и в Phone, и в LoginUsers
                    _currentUser = new Users
                    {
                        LoginUsers = phone,   // 🔑 Телефон = логин
                        LastName = TbLastName.Text.Trim(),
                        FirstName = TbFirstName.Text.Trim(),
                        Patronymic = string.IsNullOrWhiteSpace(TbPatronymic.Text) ? null : TbPatronymic.Text.Trim(),
                        BirthDate = DpBirthDate.SelectedDate.Value,
                        GenderId = (int)CbGender.SelectedValue,
                        Phone = phone,
                        RoleId = (int)CbRole.SelectedValue
                    };
                    AppConnect.modelBD.Users.Add(_currentUser);
                }
                else
                {
                    // 🔹 ОБНОВЛЕНИЕ: телефон обновляется и в Phone, и в LoginUsers
                    _currentUser.LoginUsers = phone;  // 🔑 Телефон = логин
                    _currentUser.LastName = TbLastName.Text.Trim();
                    _currentUser.FirstName = TbFirstName.Text.Trim();
                    _currentUser.Patronymic = string.IsNullOrWhiteSpace(TbPatronymic.Text) ? null : TbPatronymic.Text.Trim();
                    _currentUser.BirthDate = DpBirthDate.SelectedDate.Value;
                    _currentUser.GenderId = (int)CbGender.SelectedValue;
                    _currentUser.Phone = phone;
                    _currentUser.RoleId = (int)CbRole.SelectedValue;
                }

                AppConnect.modelBD.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка сохранения:\n{ex.Message}");
            }
        }

        private void ShowMessage(string text)
        {
            TbMessage.Text = text;
            TbMessage.Visibility = Visibility.Visible;
            TbMessage.Foreground = System.Windows.Media.Brushes.Red;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // === МАСКА ВВОДА ТЕЛЕФОНА ===

        // Защита от удаления "+7"
        private void TbPhone_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb.CaretIndex <= 2 && (e.Key == Key.Back || e.Key == Key.Delete))
                e.Handled = true;
        }

        // Разрешаем вводить только цифры после "+7"
        private void TbPhone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = sender as TextBox;

            // Если курсор в "+7" — запрещаем ввод
            if (tb.CaretIndex < 2)
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
            string fullText = tb.Text.Insert(tb.CaretIndex, e.Text);
            if (fullText.Length > 12)
            {
                e.Handled = true;
            }
        }
    }
}