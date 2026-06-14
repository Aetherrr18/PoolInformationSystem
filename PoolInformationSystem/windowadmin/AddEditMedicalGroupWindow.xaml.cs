using PoolInformationSystem.ApplicationData;
using System;
using System.Linq;
using System.Windows;

namespace PoolInformation.WindowAdmin
{
    /// <summary>
    /// Логика взаимодействия для AddEditMedicalGroupWindow.xaml
    /// </summary>
    public partial class AddEditMedicalGroupWindow : Window
    {
        private MedicalGroups _currentGroup = null;

        public AddEditMedicalGroupWindow(MedicalGroups group = null)
        {
            InitializeComponent();
            _currentGroup = group;

            if (_currentGroup != null)
            {
                // Режим редактирования: заполняем поле данными
                TbGroupName.Text = _currentGroup.GroupName;
                Title = "Редактирование медицинской группы";
            }
            else
            {
                // Режим добавления
                Title = "Добавление медицинской группы";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация: название не пустое
            string groupName = TbGroupName.Text.Trim();
            if (string.IsNullOrWhiteSpace(groupName))
            {
                MessageBox.Show("Введите название медицинской группы.",
                    "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbGroupName.Focus();
                return;
            }

            // Валидация: длина названия (макс. 20 символов, как в БД)
            if (groupName.Length > 20)
            {
                MessageBox.Show("Название группы не должно превышать 20 символов.",
                    "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Валидация: уникальность названия (если добавляем новую)
            if (_currentGroup == null)
            {
                bool exists = AppConnect.modelBD.MedicalGroups
                    .Any(g => g.GroupName.ToLower() == groupName.ToLower());
                if (exists)
                {
                    MessageBox.Show("Медицинская группа с таким названием уже существует.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                if (_currentGroup == null)
                {
                    // === ДОБАВЛЕНИЕ ===
                    _currentGroup = new MedicalGroups
                    {
                        GroupName = groupName
                    };
                    AppConnect.modelBD.MedicalGroups.Add(_currentGroup);
                }
                else
                {
                    // === РЕДАКТИРОВАНИЕ ===
                    // Проверка: не дублируется ли название с другой записью
                    bool nameExists = AppConnect.modelBD.MedicalGroups
                        .Any(g => g.GroupName.ToLower() == groupName.ToLower()
                               && g.MedicalGroupId != _currentGroup.MedicalGroupId);

                    if (nameExists)
                    {
                        MessageBox.Show("Медицинская группа с таким названием уже существует.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _currentGroup.GroupName = groupName;
                }

                AppConnect.modelBD.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}