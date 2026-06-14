using PoolInformationSystem.ApplicationData;
using System;
using System.Linq;
using System.Windows;

namespace PoolInformation.WindowClient
{
    /// <summary>
    /// Логика взаимодействия для SubscriptionOrderWindow.xaml
    /// </summary>
    public partial class SubscriptionOrderWindow : Window
    {
        private readonly Subscriptions _selectedSubscription;
        private readonly Users _currentUser;
        private decimal _basePrice;
        private decimal _subscriptionDiscountPercent = 0;
        private decimal _extraDiscountPercent = 0;

        public SubscriptionOrderWindow(Subscriptions subscription, Users currentUser)
        {
            InitializeComponent();
            _selectedSubscription = subscription;
            _currentUser = currentUser;
            LoadData();
            UpdateCalculation();
        }

        private void LoadData()
        {
            var medicalGroups = AppConnect.modelBD.MedicalGroups.ToList();
            CbMedicalGroup.ItemsSource = medicalGroups;

            var extraDiscounts = AppConnect.modelBD.Discounts
                .Where(d => d.DiscountName.Contains("Студенческая") ||
                            d.DiscountName.Contains("Пенсионная") ||
                            d.DiscountName.Contains("Семейная") ||
                            d.DiscountName.Contains("Без скидки"))
                .ToList();
            CbExtraDiscount.ItemsSource = extraDiscounts;
            CbExtraDiscount.SelectionChanged += CbExtraDiscount_SelectionChanged;

            var noDiscount = extraDiscounts.FirstOrDefault(d => d.DiscountName == "Без скидки");
            if (noDiscount != null)
                CbExtraDiscount.SelectedItem = noDiscount;
            else
                CbExtraDiscount.SelectedIndex = 0;

            var cardMethod = AppConnect.modelBD.PaymentMethods.FirstOrDefault(pm => pm.MethodName == "Карта");
            if (cardMethod != null)
            {
                CbPaymentMethod.ItemsSource = new[] { cardMethod };
                CbPaymentMethod.SelectedIndex = 0;
            }

            _basePrice = _selectedSubscription.Price;
            TbBasePrice.Text = $"{_basePrice:C}";

            if (_selectedSubscription.DiscountId.HasValue)
            {
                var disc = AppConnect.modelBD.Discounts
                    .FirstOrDefault(d => d.DiscountId == _selectedSubscription.DiscountId.Value);
                if (disc != null && disc.DiscountPercent.HasValue)
                {
                    _subscriptionDiscountPercent = disc.DiscountPercent.Value;
                    TbSubscriptionDiscount.Text = $"{_subscriptionDiscountPercent}% ({disc.DiscountName})";
                }
            }
        }

        private void CbExtraDiscount_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateCalculation();
        }

        private void UpdateCalculation()
        {
            _extraDiscountPercent = 0;
            if (CbExtraDiscount.SelectedItem is Discounts extraDisc && extraDisc.DiscountPercent.HasValue)
            {
                _extraDiscountPercent = extraDisc.DiscountPercent.Value;
                TbExtraDiscount.Text = $"{_extraDiscountPercent}% ({extraDisc.DiscountName})";
            }
            else
            {
                TbExtraDiscount.Text = "–";
            }

            decimal total = _basePrice;
            total -= total * (_subscriptionDiscountPercent / 100m);
            total -= total * (_extraDiscountPercent / 100m);
            TbTotalPrice.Text = $"{total:C}";
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (CbMedicalGroup.SelectedItem == null)
            {
                MessageBox.Show("Выберите медицинскую группу.");
                return;
            }

            if (_currentUser == null)
            {
                MessageBox.Show("Ошибка: не определён текущий пользователь.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var activation = new ActivatedSubscriptions
                {
                    UserId = _currentUser.UserId,
                    SubscriptionId = _selectedSubscription.SubscriptionId,
                    MedicalGroupId = (int)CbMedicalGroup.SelectedValue,
                    ActivationDate = DateTime.Today,
                    ExpiryDate = DateTime.Today.AddDays(_selectedSubscription.DurationDays),
                    StatusId = 1,
                    PaymentMethodId = (int)CbPaymentMethod.SelectedValue,
                    PaidAmount = decimal.Parse(TbTotalPrice.Text.Replace("₽", "").Replace(" ", "").Trim()),
                    VisitsRemaining = _selectedSubscription.MaxVisits  // 🔹 КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ!
                };

                AppConnect.modelBD.ActivatedSubscriptions.Add(activation);
                AppConnect.modelBD.SaveChanges();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка оформления:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}