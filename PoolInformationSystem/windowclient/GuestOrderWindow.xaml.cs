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
    /// Логика взаимодействия для GuestOrderWindow.xaml
    /// </summary>
    public partial class GuestOrderWindow : Window
    {
        private readonly Subscriptions _sub;

        // 🔹 ID пользователя-гостя в БД (замените на реальный после выполнения SQL-скрипта)
        private const int GUEST_USER_ID = 0; // ← ЗАМЕНИТЕ НА РЕАЛЬНЫЙ UserId гостя!

        public GuestOrderWindow(Subscriptions subscription)
        {
            InitializeComponent();
            _sub = subscription;
            LoadData();
        }

        private void LoadData()
        {
            TbSelectedName.Text = _sub.NameSubscriptionId;

            // Загрузка способов оплаты
            var methods = AppConnect.modelBD.PaymentMethods.ToList();
            CbPaymentMethod.ItemsSource = methods;
            if (methods.Any()) CbPaymentMethod.SelectedIndex = 0;

            // Расчёт цены со скидкой (если есть)
            decimal price = _sub.Price;
            if (_sub.DiscountId.HasValue)
            {
                var disc = AppConnect.modelBD.Discounts.FirstOrDefault(d => d.DiscountId == _sub.DiscountId.Value);
                if (disc?.DiscountPercent > 0)
                    price -= price * (disc.DiscountPercent.Value / 100m);
            }
            TbTotalPrice.Text = $"{price:F2} ₽";
        }

        private void CbPaymentMethod_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (CbPaymentMethod.SelectedItem == null)
            {
                MessageBox.Show("Выберите способ оплаты.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 🔹 Проверяем, что гость реально существует в БД
                var guestUser = AppConnect.modelBD.Users.FirstOrDefault(u => u.UserId == GUEST_USER_ID);
                if (guestUser == null)
                {
                    MessageBox.Show(
                        "Ошибка: пользователь-гость не найден в базе данных.\n\n" +
                        "Выполните SQL-скрипт для создания гостя и обновите константу GUEST_USER_ID в коде.",
                        "Ошибка конфигурации",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                var paymentMethod = CbPaymentMethod.SelectedItem as PaymentMethods;
                decimal finalPrice = decimal.Parse(TbTotalPrice.Text.Replace("₽", "").Replace(" ", "").Trim());

                // Берём ID первой медицинской группы из справочника (обычно "Основная")
                int defaultMedicalGroupId = AppConnect.modelBD.MedicalGroups
                    .OrderBy(g => g.MedicalGroupId)
                    .Select(g => g.MedicalGroupId)
                    .FirstOrDefault();

                if (defaultMedicalGroupId == 0)
                {
                    MessageBox.Show(
                        "Ошибка: в базе данных нет ни одной медицинской группы.\n" +
                        "Добавьте хотя бы одну группу через панель администратора.",
                        "Ошибка конфигурации",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Получаем статус "Активен"
                var activeStatus = AppConnect.modelBD.SubscriptionStatuses
                    .FirstOrDefault(s => s.StatusId == 1);

                if (activeStatus == null)
                {
                    MessageBox.Show(
                        "Ошибка: в базе данных нет статуса 'Активен'.\n" +
                        "Добавьте статус через панель администратора.",
                        "Ошибка конфигурации",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                var activation = new ActivatedSubscriptions
                {
                    UserId = GUEST_USER_ID,  // 🔹 Теперь это реальный UserId из БД
                    SubscriptionId = _sub.SubscriptionId,
                    MedicalGroupId = defaultMedicalGroupId,
                    ActivationDate = DateTime.Today,
                    ExpiryDate = DateTime.Today.AddDays(_sub.DurationDays),
                    StatusId = activeStatus.StatusId,
                    PaymentMethodId = paymentMethod.MethodId,
                    PaidAmount = finalPrice,
                    VisitsRemaining = _sub.MaxVisits  // 🔹 Обязательно указываем количество посещений!
                };

                AppConnect.modelBD.ActivatedSubscriptions.Add(activation);
                AppConnect.modelBD.SaveChanges();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка оформления:\n{ex.Message}\n\n" +
                    $"Внутренняя ошибка: {ex.InnerException?.Message ?? "отсутствует"}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}