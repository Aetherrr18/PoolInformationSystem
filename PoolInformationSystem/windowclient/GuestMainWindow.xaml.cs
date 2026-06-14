using PoolInformation;
using PoolInformation.WindowClient;
using PoolInformationSystem.ApplicationData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PoolInformationSystem.WindowClient
{
    /// <summary>
    /// Логика взаимодействия для GuestMainWindow.xaml
    /// </summary>
    public partial class GuestMainWindow : Window
    {
        private List<Subscriptions> _guestSubscriptions = new List<Subscriptions>();

        public GuestMainWindow()
        {
            InitializeComponent();

            // Явная привязка обработчиков в коде (гарантирует работу на любой версии C#)
            BtnLogin.Click += BtnLogin_Click;
            BtnDetails1.Click += BtnDetails1_Click;
            BtnSubscribe1.Click += BtnSubscribe1_Click;

            LoadGuestSubscriptions();
        }

        private void LoadGuestSubscriptions()
        {
            try
            {
                // Фильтрация только нужных абонементов
                _guestSubscriptions = AppConnect.modelBD.Subscriptions
                    .Where(s => s.NameSubscriptionId.ToLower().Contains("старт в плавание") ||
                                s.NameSubscriptionId.ToLower().Contains("гостевой визит"))
                    .Take(2).ToList();

                for (int i = 0; i < _guestSubscriptions.Count; i++)
                {
                    var sub = _guestSubscriptions[i];
                    var type = AppConnect.modelBD.SubscriptionTypes.FirstOrDefault(t => t.TypeId == sub.TypeId);

                    string discountText = "–";
                    if (sub.DiscountId.HasValue)
                    {
                        var d = AppConnect.modelBD.Discounts.FirstOrDefault(x => x.DiscountId == sub.DiscountId.Value);
                        discountText = d != null ? d.DiscountName : "–";
                    }

                    string visits = sub.MaxVisits >= 999 ? "Безлимит" : sub.MaxVisits.ToString();
                    string duration = FormatDuration(sub.DurationDays);

                    if (i == 0)
                    {
                        TbName1.Text = sub.NameSubscriptionId;
                        TbType1.Text = type != null ? type.TypeName : "–";
                        TbVisits1.Text = visits;
                        TbDuration1.Text = duration;
                        TbDiscount1.Text = discountText;
                        TbPrice1.Text = string.Format("{0:C}", sub.Price);
                        Card1.Tag = sub;
                    }
                }

                Card1.Visibility = _guestSubscriptions.Count >= 1 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ Заменено на C# 7.3-совместимый синтаксис
        private string FormatDuration(int days)
        {
            if (days == 1) return "1 день";
            if (days >= 2 && days <= 4) return string.Format("{0} дня", days);
            return string.Format("{0} дней", days);
        }

        private void BtnDetails1_Click(object sender, RoutedEventArgs e) => ShowDetails(Card1);
        private void BtnSubscribe1_Click(object sender, RoutedEventArgs e) => OpenOrder(Card1);

        private void ShowDetails(Border card)
        {
            var sub = card.Tag as Subscriptions;
            if (sub != null) new SubscriptionDetailsWindow(sub).ShowDialog();
        }

        private void OpenOrder(Border card)
        {
            var sub = card.Tag as Subscriptions;
            if (sub != null)
            {
                try
                {
                    var orderWin = new GuestOrderWindow(sub);
                    orderWin.Owner = this;
                    if (orderWin.ShowDialog() == true)
                        MessageBox.Show("Пробный абонемент успешно оформлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка оформления:\n{ex.Message}\n\n" +
                        $"Внутренняя ошибка: {ex.InnerException?.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var auth = new MainWindow();
            auth.Show();
            this.Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}