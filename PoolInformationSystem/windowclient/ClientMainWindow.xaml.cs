using PoolInformationSystem.ApplicationData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PoolInformation.WindowClient
{
    /// <summary>
    /// Логика взаимодействия для ClientMainWindow.xaml
    /// </summary>
    public partial class ClientMainWindow : Window
    {
        private List<Subscriptions> _subscriptions = new List<Subscriptions>();
        private Users _currentUser;

        public ClientMainWindow(Users currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            DisplayClientName();
            LoadSubscriptions();
        }

        private void DisplayClientName()
        {
            if (_currentUser != null)
            {
                string initials = "";
                if (!string.IsNullOrWhiteSpace(_currentUser.FirstName))
                    initials += _currentUser.FirstName[0] + ".";
                if (!string.IsNullOrWhiteSpace(_currentUser.Patronymic))
                    initials += _currentUser.Patronymic[0] + ".";

                TbClientName.Text = $"{_currentUser.LastName} {initials}".Trim();
            }
            else
            {
                TbClientName.Text = "Клиент";
            }
        }

        private void LoadSubscriptions()
        {
            try
            {
                _subscriptions = AppConnect.modelBD.Subscriptions.ToList();
                var displayed = _subscriptions.Take(5).ToList();

                // ✅ Получаем список активных абонементов ТЕКУЩЕГО пользователя
                var userActiveSubscriptions = AppConnect.modelBD.ActivatedSubscriptions
                    .Where(a => a.UserId == _currentUser.UserId
                           && a.StatusId == 1
                           && a.ExpiryDate >= DateTime.Today)
                    .Select(a => a.SubscriptionId)
                    .ToList();

                for (int i = 0; i < displayed.Count; i++)
                {
                    var sub = displayed[i];
                    var cardIndex = i + 1;

                    var type = AppConnect.modelBD.SubscriptionTypes
                        .FirstOrDefault(t => t.TypeId == sub.TypeId);

                    string discountText = "–";
                    if (sub.DiscountId.HasValue)
                    {
                        var discount = AppConnect.modelBD.Discounts
                            .FirstOrDefault(d => d.DiscountId == sub.DiscountId.Value);
                        discountText = discount?.DiscountName ?? "–";
                    }

                    string visitsText = sub.MaxVisits >= 999 ? "Безлимит" : sub.MaxVisits.ToString();
                    string durationText = FormatDuration(sub.DurationDays);

                    // ✅ Проверяем, оформлен ли этот абонемент ТЕКУЩИМ пользователем
                    bool isPurchased = userActiveSubscriptions.Contains(sub.SubscriptionId);

                    switch (cardIndex)
                    {
                        case 1:
                            TbName1.Text = sub.NameSubscriptionId;
                            TbType1.Text = type?.TypeName ?? "–";
                            TbVisits1.Text = visitsText;
                            TbDuration1.Text = durationText;
                            TbDiscount1.Text = discountText;
                            TbPrice1.Text = $"{sub.Price:C}";
                            Card1.Tag = sub;
                            PurchasedBanner1.Visibility = isPurchased ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case 2:
                            TbName2.Text = sub.NameSubscriptionId;
                            TbType2.Text = type?.TypeName ?? "–";
                            TbVisits2.Text = visitsText;
                            TbDuration2.Text = durationText;
                            TbDiscount2.Text = discountText;
                            TbPrice2.Text = $"{sub.Price:C}";
                            Card2.Tag = sub;
                            PurchasedBanner2.Visibility = isPurchased ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case 3:
                            TbName3.Text = sub.NameSubscriptionId;
                            TbType3.Text = type?.TypeName ?? "–";
                            TbVisits3.Text = visitsText;
                            TbDuration3.Text = durationText;
                            TbDiscount3.Text = discountText;
                            TbPrice3.Text = $"{sub.Price:C}";
                            Card3.Tag = sub;
                            PurchasedBanner3.Visibility = isPurchased ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case 4:
                            TbName4.Text = sub.NameSubscriptionId;
                            TbType4.Text = type?.TypeName ?? "–";
                            TbVisits4.Text = visitsText;
                            TbDuration4.Text = durationText;
                            TbDiscount4.Text = discountText;
                            TbPrice4.Text = $"{sub.Price:C}";
                            Card4.Tag = sub;
                            PurchasedBanner4.Visibility = isPurchased ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case 5:
                            TbName5.Text = sub.NameSubscriptionId;
                            TbType5.Text = type?.TypeName ?? "–";
                            TbVisits5.Text = visitsText;
                            TbDuration5.Text = durationText;
                            TbDiscount5.Text = discountText;
                            TbPrice5.Text = $"{sub.Price:C}";
                            Card5.Tag = sub;
                            PurchasedBanner5.Visibility = isPurchased ? Visibility.Visible : Visibility.Collapsed;
                            break;
                    }
                }

                Card1.Visibility = _subscriptions.Count >= 1 ? Visibility.Visible : Visibility.Collapsed;
                Card2.Visibility = _subscriptions.Count >= 2 ? Visibility.Visible : Visibility.Collapsed;
                Card3.Visibility = _subscriptions.Count >= 3 ? Visibility.Visible : Visibility.Collapsed;
                Card4.Visibility = _subscriptions.Count >= 4 ? Visibility.Visible : Visibility.Collapsed;
                Card5.Visibility = _subscriptions.Count >= 5 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки абонементов:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatDuration(int days)
        {
            if (days == 1) return "1 день";
            if (days >= 2 && days <= 4) return $"{days} дня";
            return $"{days} дней";
        }

        // === КНОПКИ ПОДРОБНЕЕ ===
        private void BtnDetails1_Click(object sender, RoutedEventArgs e) => ShowDetails(Card1);
        private void BtnDetails2_Click(object sender, RoutedEventArgs e) => ShowDetails(Card2);
        private void BtnDetails3_Click(object sender, RoutedEventArgs e) => ShowDetails(Card3);
        private void BtnDetails4_Click(object sender, RoutedEventArgs e) => ShowDetails(Card4);
        private void BtnDetails5_Click(object sender, RoutedEventArgs e) => ShowDetails(Card5);

        // === КНОПКИ ОФОРМИТЬ ===
        private void BtnSubscribe1_Click(object sender, RoutedEventArgs e) => StartSubscription(Card1);
        private void BtnSubscribe2_Click(object sender, RoutedEventArgs e) => StartSubscription(Card2);
        private void BtnSubscribe3_Click(object sender, RoutedEventArgs e) => StartSubscription(Card3);
        private void BtnSubscribe4_Click(object sender, RoutedEventArgs e) => StartSubscription(Card4);
        private void BtnSubscribe5_Click(object sender, RoutedEventArgs e) => StartSubscription(Card5);

        private void ShowDetails(Border card)
        {
            var subscription = card.Tag as Subscriptions;
            if (subscription != null)
            {
                var detailsWindow = new SubscriptionDetailsWindow(subscription);
                detailsWindow.ShowDialog();
            }
        }

        private void StartSubscription(Border card)
        {
            var subscription = card.Tag as Subscriptions;
            if (subscription != null)
            {
                // 🔹 КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: передаём текущего пользователя в окно оформления
                var orderWindow = new SubscriptionOrderWindow(subscription, _currentUser);
                orderWindow.Owner = this;
                if (orderWindow.ShowDialog() == true)
                {
                    MessageBox.Show("Абонемент оформлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // ✅ Перезагружаем список, чтобы появилось сообщение о покупке
                    LoadSubscriptions();
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы действительно хотите выйти из личного кабинета?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
        }
    }
}