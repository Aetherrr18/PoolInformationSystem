using PoolInformationSystem.ApplicationData;
using System;
using System.Linq;
using System.Windows;

namespace PoolInformation.WindowAdmin
{
    /// <summary>
    /// Логика взаимодействия для AddEditSubscriptionTypeWindow.xaml
    /// </summary>
    public partial class AddEditSubscriptionTypeWindow : Window
    {

        private SubscriptionTypes _currentType = null;

        public AddEditSubscriptionTypeWindow(SubscriptionTypes type = null)
        {
            InitializeComponent();
            _currentType = type;

            if (_currentType != null)
            {
                // Режим редактирования: заполняем поля данными
                TbTypeName.Text = _currentType.TypeName;
                TbComment.Text = _currentType.Comment ?? "";
                Title = "Редактирование типа абонемента";
            }
            else
            {
                // Режим добавления
                Title = "Добавление типа абонемента";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация: название не пустое
            string typeName = TbTypeName.Text.Trim();
            if (string.IsNullOrWhiteSpace(typeName))
            {
                MessageBox.Show("Введите название типа абонемента.",
                    "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbTypeName.Focus();
                return;
            }

            // Валидация: длина названия (макс. 30 символов, как в БД)
            if (typeName.Length > 30)
            {
                MessageBox.Show("Название типа не должно превышать 30 символов.",
                    "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Валидация: длина комментария (макс. 200 символов, как в БД)
            string comment = TbComment.Text.Trim();
            if (comment.Length > 200)
            {
                MessageBox.Show("Комментарий не должен превышать 200 символов.",
                    "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Валидация: уникальность названия (если добавляем новый)
            if (_currentType == null)
            {
                bool exists = AppConnect.modelBD.SubscriptionTypes
                    .Any(t => t.TypeName.ToLower() == typeName.ToLower());
                if (exists)
                {
                    MessageBox.Show("Тип абонемента с таким названием уже существует.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                if (_currentType == null)
                {
                    // === ДОБАВЛЕНИЕ ===
                    _currentType = new SubscriptionTypes
                    {
                        TypeName = typeName,
                        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment
                    };
                    AppConnect.modelBD.SubscriptionTypes.Add(_currentType);
                }
                else
                {
                    // === РЕДАКТИРОВАНИЕ ===
                    // Проверка: не дублируется ли название с другой записью
                    bool nameExists = AppConnect.modelBD.SubscriptionTypes
                        .Any(t => t.TypeName.ToLower() == typeName.ToLower()
                               && t.TypeId != _currentType.TypeId);

                    if (nameExists)
                    {
                        MessageBox.Show("Тип абонемента с таким названием уже существует.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _currentType.TypeName = typeName;
                    _currentType.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment;
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