using PoolInformationSystem.ApplicationData;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace PoolInformation.WindowAdmin
{
    public partial class AddEditDiscountWindow : Window
    {

        private Discounts _currentDiscount = null;

        public AddEditDiscountWindow(Discounts discount = null)
        {
            InitializeComponent();
            _currentDiscount = discount;

            if (_currentDiscount != null)
            {
                // Режим редактирования: заполняем поля данными
                TbDiscountName.Text = _currentDiscount.DiscountName;
                TbDiscountPercent.Text = _currentDiscount.DiscountPercent?.ToString("0.00");
                Title = "Редактирование скидки";
            }
            else
            {
                // Режим добавления
                Title = "Добавление скидки";
            }
        }

        private void TbDiscountPercent_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры, точку и запятую
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9.,]$");
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация: название не пустое
            string discountName = TbDiscountName.Text.Trim();
            if (string.IsNullOrWhiteSpace(discountName))
            {
                MessageBox.Show("Введите название скидки.",
                    "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbDiscountName.Focus();
                return;
            }

            // Валидация: длина названия (макс. 100 символов, как в БД)
            if (discountName.Length > 100)
            {
                MessageBox.Show("Название скидки не должно превышать 100 символов.",
                    "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Валидация: процент скидки
            if (!decimal.TryParse(TbDiscountPercent.Text.Replace(',', '.'), out decimal discountPercent))
            {
                MessageBox.Show("Введите корректное значение процента (число).",
                    "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbDiscountPercent.Focus();
                return;
            }

            if (discountPercent < 0 || discountPercent > 100)
            {
                MessageBox.Show("Процент скидки должен быть в диапазоне от 0 до 100.",
                    "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbDiscountPercent.Focus();
                return;
            }

            // Валидация: уникальность названия (если добавляем новую)
            if (_currentDiscount == null)
            {
                bool exists = AppConnect.modelBD.Discounts
                    .Any(d => d.DiscountName.ToLower() == discountName.ToLower());
                if (exists)
                {
                    MessageBox.Show("Скидка с таким названием уже существует.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                if (_currentDiscount == null)
                {
                    // === ДОБАВЛЕНИЕ ===
                    _currentDiscount = new Discounts
                    {
                        DiscountName = discountName,
                        DiscountPercent = Math.Round(discountPercent, 2) // Округляем до 2 знаков
                    };
                    AppConnect.modelBD.Discounts.Add(_currentDiscount);
                }
                else
                {
                    // === РЕДАКТИРОВАНИЕ ===
                    // Проверка: не дублируется ли название с другой записью
                    bool nameExists = AppConnect.modelBD.Discounts
                        .Any(d => d.DiscountName.ToLower() == discountName.ToLower()
                               && d.DiscountId != _currentDiscount.DiscountId);

                    if (nameExists)
                    {
                        MessageBox.Show("Скидка с таким названием уже существует.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _currentDiscount.DiscountName = discountName;
                    _currentDiscount.DiscountPercent = Math.Round(discountPercent, 2);
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