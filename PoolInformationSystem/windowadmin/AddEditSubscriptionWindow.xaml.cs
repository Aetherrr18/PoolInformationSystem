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
    /// Логика взаимодействия для AddEditSubscriptionWindow.xaml
    /// </summary>
    public partial class AddEditSubscriptionWindow : Window
    {
        private Subscriptions _currentSubscription = null;

        public AddEditSubscriptionWindow(Subscriptions subscription = null)
        {
            InitializeComponent();
            _currentSubscription = subscription;

            LoadComboBoxes();

            if (_currentSubscription != null)
            {
                // Режим редактирования
                TbName.Text = _currentSubscription.NameSubscriptionId;
                CbType.SelectedValue = _currentSubscription.TypeId;
                TbCommentToType.Text = _currentSubscription.Comment; // если это комментарий к типу
                TbMaxVisits.Text = _currentSubscription.MaxVisits.ToString();
                TbPrice.Text = _currentSubscription.Price.ToString("F2");
                TbDurationDays.Text = _currentSubscription.DurationDays.ToString();
                CbDiscount.SelectedValue = _currentSubscription.DiscountId;
                TbComment.Text = _currentSubscription.Comment;
            }
        }

        private void LoadComboBoxes()
        {
            var types = AppConnect.modelBD.SubscriptionTypes.ToList();
            CbType.ItemsSource = types;

            var discounts = AppConnect.modelBD.Discounts.ToList();
            CbDiscount.ItemsSource = discounts;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(TbName.Text))
            {
                MessageBox.Show("Введите название абонемента.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CbType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип абонемента.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TbMaxVisits.Text, out int maxVisits) || maxVisits <= 0)
            {
                MessageBox.Show("Количество занятий должно быть положительным числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TbPrice.Text.Replace(",", "."), out decimal price) || price <= 0)
            {
                MessageBox.Show("Цена должна быть положительным числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TbDurationDays.Text, out int duration) || duration <= 0)
            {
                MessageBox.Show("Продолжительность должна быть положительным числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CbDiscount.SelectedItem == null)
            {
                MessageBox.Show("Выберите скидку.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_currentSubscription == null)
                {
                    // Добавление
                    _currentSubscription = new Subscriptions
                    {
                        NameSubscriptionId = TbName.Text.Trim(),
                        TypeId = (int)CbType.SelectedValue,
                        MaxVisits = maxVisits,
                        Price = price,
                        DurationDays = duration,
                        DiscountId = (int?)CbDiscount.SelectedValue,
                        Comment = TbComment.Text.Trim()
                    };
                    AppConnect.modelBD.Subscriptions.Add(_currentSubscription);
                }
                else
                {
                    // Обновление
                    _currentSubscription.NameSubscriptionId = TbName.Text.Trim();
                    _currentSubscription.TypeId = (int)CbType.SelectedValue;
                    _currentSubscription.MaxVisits = maxVisits;
                    _currentSubscription.Price = price;
                    _currentSubscription.DurationDays = duration;
                    _currentSubscription.DiscountId = (int?)CbDiscount.SelectedValue;
                    _currentSubscription.Comment = TbComment.Text.Trim();
                }

                AppConnect.modelBD.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Только цифры
        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Цифры и одна точка/запятая
        private void DecimalValidation(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string fullText = textBox.Text.Insert(textBox.CaretIndex, e.Text);
            Regex regex = new Regex(@"^[0-9]*(?:[.,][0-9]{0,2})?$");
            e.Handled = !regex.IsMatch(fullText);
        }
    }
}