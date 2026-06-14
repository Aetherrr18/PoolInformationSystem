using PoolInformationSystem.ApplicationData;
using System.Linq;
using System.Windows;

namespace PoolInformation.WindowClient
{
    /// <summary>
    /// Логика взаимодействия для SubscriptionDetailsWindow.xaml
    /// </summary>
    public partial class SubscriptionDetailsWindow : Window
    {
        private readonly Subscriptions _subscription;

        public SubscriptionDetailsWindow(Subscriptions subscription)
        {
            InitializeComponent();
            _subscription = subscription;
            LoadSubscriptionDetails();
        }

        private void LoadSubscriptionDetails()
        {
            // Название
            TbSubscriptionName.Text = _subscription.NameSubscriptionId;

            // Тип абонемента
            var subscriptionType = AppConnect.modelBD.SubscriptionTypes
                .FirstOrDefault(t => t.TypeId == _subscription.TypeId);
            TbSubscriptionType.Text = subscriptionType?.TypeName ?? "Не указан";

            // Количество занятий
            TbMaxVisits.Text = _subscription.MaxVisits >= 999
                ? "Безлимитное посещение"
                : $"{_subscription.MaxVisits} занятий";

            // Срок действия
            TbDuration.Text = FormatDuration(_subscription.DurationDays);

            // Цена
            TbPrice.Text = $"{_subscription.Price:F2} ₽";

            // Скидка
            if (_subscription.DiscountId.HasValue)
            {
                var discount = AppConnect.modelBD.Discounts
                    .FirstOrDefault(d => d.DiscountId == _subscription.DiscountId.Value);

                if (discount != null && discount.DiscountPercent.HasValue)
                {
                    TbDiscount.Text = $"{discount.DiscountPercent}% ({discount.DiscountName})";

                    // Расчет цены со скидкой
                    decimal discountAmount = _subscription.Price * (discount.DiscountPercent.Value / 100);
                    decimal finalPrice = _subscription.Price - discountAmount;
                    TbFinalPrice.Text = $"{finalPrice:F2} ₽";
                }
                else
                {
                    TbDiscount.Text = "–";
                    TbFinalPrice.Text = $"{_subscription.Price:F2} ₽";
                }
            }
            else
            {
                TbDiscount.Text = "–";
                TbFinalPrice.Text = $"{_subscription.Price:F2} ₽";
            }

            // Комментарий
            TbComment.Text = string.IsNullOrWhiteSpace(_subscription.Comment)
                ? "Дополнительная информация отсутствует"
                : _subscription.Comment;
        }

        private string FormatDuration(int days)
        {
            if (days == 1) return "1 день";
            if (days >= 2 && days <= 4) return $"{days} дня";
            if (days >= 5 && days <= 30) return $"{days} дней";
            if (days > 30 && days < 365)
            {
                int months = days / 30;
                return months == 1 ? "1 месяц" : $"{months} месяцев";
            }
            if (days >= 365)
            {
                int years = days / 365;
                return years == 1 ? "1 год" : $"{years} года";
            }
            return $"{days} дней";
        }

        private void BtnSubscribe_Click(object sender, RoutedEventArgs e)
        {
            // Здесь можно открыть форму оформления абонемента
            MessageBox.Show($"Переход к оформлению: {_subscription.NameSubscriptionId}",
                "Оформление", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}