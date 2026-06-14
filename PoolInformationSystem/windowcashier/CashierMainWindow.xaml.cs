using PoolInformationSystem.ApplicationData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PoolInformation.WindowCashier
{
    /// <summary>
    /// Логика взаимодействия для CashierMainWindow.xaml
    /// </summary>
    public partial class CashierMainWindow : Window
    {
        private readonly Users _currentCashier;
        private Users _selectedClient;
        private Subscriptions _selectedSubscription;
        private decimal _basePrice;
        private decimal _subscriptionDiscountPercent = 0;
        private decimal _extraDiscountPercent = 0;

        private List<ActivatedSubscriptionInfo> _clientSubscriptions = new List<ActivatedSubscriptionInfo>();
        private ActivatedSubscriptions _selectedClientSubscription;

        public CashierMainWindow(Users cashier)
        {
            InitializeComponent();
            _currentCashier = cashier;
            DisplayCashierName();
            InitializeSearchField();
            LoadData();
            ValidateForm();
        }

        public class ActivatedSubscriptionInfo
        {
            public ActivatedSubscriptions Activation { get; set; }
            public string SubscriptionName { get; set; }
            public int VisitsRemaining { get; set; }
            public int MaxVisits { get; set; }
            public DateTime ExpiryDate { get; set; }
            public DateTime ActivationDate { get; set; }

            public string DisplayText
            {
                get
                {
                    string visitsInfo = MaxVisits >= 999
                        ? "Безлимит"
                        : $"{VisitsRemaining}/{MaxVisits} посещ.";
                    return $"{SubscriptionName} — {visitsInfo} (до {ExpiryDate:dd.MM.yyyy})";
                }
            }
        }

        private void DisplayCashierName()
        {
            if (_currentCashier != null)
            {
                string initials = "";
                if (!string.IsNullOrWhiteSpace(_currentCashier.FirstName))
                    initials += _currentCashier.FirstName[0] + ".";
                if (!string.IsNullOrWhiteSpace(_currentCashier.Patronymic))
                    initials += _currentCashier.Patronymic[0] + ".";
                TbCashierName.Text = $"{_currentCashier.LastName} {initials}".Trim();
            }
            else
            {
                TbCashierName.Text = "Кассир";
            }
        }

        private void InitializeSearchField()
        {
            SearchClientForSubTextBox.Text = "+7";
            SearchClientForSubTextBox.CaretIndex = 2;
            SearchClientForSubTextBox.SelectionStart = 2;
        }

        private void LoadData()
        {
            LoadClients();
            LoadSubscriptions();
            LoadDiscounts();
            LoadPaymentMethods();
            LoadMedicalGroups();
        }

        private void LoadClients()
        {
            try
            {
                var clients = AppConnect.modelBD.Users
                    .Where(u => u.RoleId == 1)
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToList();
                DgClients.ItemsSource = clients;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSubscriptions()
        {
            try
            {
                var subscriptions = AppConnect.modelBD.Subscriptions
                    .OrderBy(s => s.NameSubscriptionId)
                    .ToList();
                CbSubscription.ItemsSource = subscriptions;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки абонементов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDiscounts()
        {
            try
            {
                var discounts = AppConnect.modelBD.Discounts
                    .Where(d => d.DiscountName.Contains("Студенческая") ||
                                d.DiscountName.Contains("Пенсионная") ||
                                d.DiscountName.Contains("Семейная"))
                    .OrderBy(d => d.DiscountPercent)
                    .ToList();

                discounts.Insert(0, new Discounts
                {
                    DiscountId = -1,
                    DiscountName = "Без скидки",
                    DiscountPercent = 0.00m
                });

                CbDiscount.ItemsSource = discounts;
                CbDiscount.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки скидок: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPaymentMethods()
        {
            try
            {
                var paymentMethods = AppConnect.modelBD.PaymentMethods
                    .OrderBy(m => m.MethodName)
                    .ToList();
                CbPaymentMethod.ItemsSource = paymentMethods;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки способов оплаты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMedicalGroups()
        {
            try
            {
                var medicalGroups = AppConnect.modelBD.MedicalGroups
                    .OrderBy(g => g.GroupName)
                    .ToList();
                CbMedicalGroup.ItemsSource = medicalGroups;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки медицинских групп: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ ЗАГРУЗКА АБОНЕМЕНТОВ КЛИЕНТА
        private void LoadClientSubscriptions(Users client)
        {
            _clientSubscriptions.Clear();
            _selectedClientSubscription = null;

            if (client == null)
                return;

            try
            {
                var activations = AppConnect.modelBD.ActivatedSubscriptions
                    .Where(a => a.UserId == client.UserId && a.StatusId == 1)
                    .ToList();

                foreach (var activation in activations)
                {
                    var subscription = AppConnect.modelBD.Subscriptions
                        .FirstOrDefault(s => s.SubscriptionId == activation.SubscriptionId);

                    if (subscription == null)
                        continue;

                    if (activation.ExpiryDate < DateTime.Today)
                        continue;

                    // ✅ ИСПРАВЛЕНО: убираем проверку VisitsRemaining <= 0
                    // Теперь показываем все активные абонементы
                    // if (activation.VisitsRemaining <= 0 && subscription.MaxVisits < 999)
                    //     continue;

                    _clientSubscriptions.Add(new ActivatedSubscriptionInfo
                    {
                        Activation = activation,
                        SubscriptionName = subscription.NameSubscriptionId,
                        VisitsRemaining = activation.VisitsRemaining,
                        MaxVisits = subscription.MaxVisits,
                        ExpiryDate = activation.ExpiryDate,
                        ActivationDate = activation.ActivationDate
                    });
                }

                if (_clientSubscriptions.Any())
                {
                    CbClientSubscriptions.ItemsSource = null;
                    CbClientSubscriptions.ItemsSource = _clientSubscriptions;
                    CbClientSubscriptions.SelectedIndex = 0;
                }
                else
                {
                    CbClientSubscriptions.ItemsSource = null;
                    TbNoSubscriptions.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки абонементов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSubscriptionInfo(ActivatedSubscriptionInfo info)
        {
            if (info == null)
                return;

            _selectedClientSubscription = info.Activation;

            if (info.MaxVisits >= 999)
            {
                TbRemainingVisits.Text = "∞ Безлимит";
                TbRemainingVisits.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
            }
            else
            {
                TbRemainingVisits.Text = $"{info.VisitsRemaining} из {info.MaxVisits}";

                if (info.VisitsRemaining <= 3)
                    TbRemainingVisits.Foreground = new SolidColorBrush(Colors.Red);
                else if (info.VisitsRemaining <= 10)
                    TbRemainingVisits.Foreground = new SolidColorBrush(Colors.DarkOrange);
                else
                    TbRemainingVisits.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
            }

            TbExpiryDate.Text = info.ExpiryDate.ToString("dd.MM.yyyy");

            int daysLeft = (info.ExpiryDate - DateTime.Today).Days;
            if (daysLeft <= 3)
                TbSubscriptionStatus.Text = $"⚠️ До окончания осталось {daysLeft} дн. — рекомендуется продлить";
            else
                TbSubscriptionStatus.Text = $"✅ Действует ещё {daysLeft} дн.";

            SubscriptionInfoBorder.Visibility = Visibility.Visible;
            TbNoSubscriptions.Visibility = Visibility.Collapsed;

            var subscription = AppConnect.modelBD.Subscriptions
                .FirstOrDefault(s => s.SubscriptionId == info.Activation.SubscriptionId);

            if (subscription != null && subscription.MaxVisits < 999 && info.VisitsRemaining > 0)
            {
                BtnDeductVisit.IsEnabled = true;
            }
            else
            {
                BtnDeductVisit.IsEnabled = false;
            }
        }

        private void BtnDeductVisit_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClientSubscription == null || _selectedClient == null)
            {
                MessageBox.Show("❌ Выберите абонемент для списания!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var subscription = AppConnect.modelBD.Subscriptions
                .FirstOrDefault(s => s.SubscriptionId == _selectedClientSubscription.SubscriptionId);

            if (subscription == null)
                return;

            if (subscription.MaxVisits >= 999)
            {
                MessageBox.Show("ℹ️ Это безлимитный абонемент. Посещения не списываются.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_selectedClientSubscription.VisitsRemaining <= 0)
            {
                MessageBox.Show("❌ В абонемементе закончились посещения!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(
                $"Списать 1 посещение для клиента {_selectedClient.LastName} {_selectedClient.FirstName}?\n\n" +
                $"Абонемент: {subscription.NameSubscriptionId}\n" +
                $"Осталось посещений: {_selectedClientSubscription.VisitsRemaining} → {_selectedClientSubscription.VisitsRemaining - 1}\n" +
                $"Действителен до: {_selectedClientSubscription.ExpiryDate:dd.MM.yyyy}",
                "Подтверждение списания",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _selectedClientSubscription.VisitsRemaining -= 1;

                if (_selectedClientSubscription.VisitsRemaining <= 0)
                {
                    var completedStatus = AppConnect.modelBD.SubscriptionStatuses
                        .FirstOrDefault(s => s.StatusName == "Завершен" || s.StatusId == 2);

                    if (completedStatus != null)
                    {
                        _selectedClientSubscription.StatusId = completedStatus.StatusId;
                    }
                }

                AppConnect.modelBD.SaveChanges();

                MessageBox.Show(
                    $"✅ Посещение успешно списано!\n\n" +
                    $"Осталось посещений: {_selectedClientSubscription.VisitsRemaining}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LoadClientSubscriptions(_selectedClient);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка при списании:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CbClientSubscriptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbClientSubscriptions.SelectedItem is ActivatedSubscriptionInfo selectedInfo)
            {
                UpdateSubscriptionInfo(selectedInfo);
            }
        }

        private void SearchClientForSubTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string phone = SearchClientForSubTextBox.Text?.Trim();

                if (string.IsNullOrEmpty(phone) || phone.Length < 12)
                {
                    ClearSelectedClient();
                    return;
                }

                var client = AppConnect.modelBD.Users
                    .FirstOrDefault(u => u.RoleId == 1 && u.Phone == phone);

                if (client != null)
                {
                    _selectedClient = client;
                    ShowSelectedClient(client);
                    ClientNotFoundHint.Visibility = Visibility.Collapsed;
                    LoadClientSubscriptions(client);
                }
                else
                {
                    ClearSelectedClient();
                    ClientNotFoundHint.Text = "❌ Клиент с номером " + phone + " не найден в базе данных!";
                    ClientNotFoundHint.Foreground = new SolidColorBrush(Colors.Red);
                    ClientNotFoundHint.Visibility = Visibility.Visible;

                    var result = MessageBox.Show(
                        $"Клиент с номером {phone} не найден в базе данных.\n\n" +
                        "Хотите перейти на вкладку «КЛИЕНТЫ» для регистрации нового клиента?",
                        "Клиент не найден", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        MainTabControl.SelectedIndex = 0;
                        InitializeSearchField();
                    }
                }
                ValidateForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchClientForSubTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox.CaretIndex <= 2 && (e.Key == Key.Back || e.Key == Key.Delete))
                e.Handled = true;
            if (textBox.SelectionStart < 2 && textBox.SelectionLength > 0)
            {
                if (e.Key == Key.Back || e.Key == Key.Delete || (e.Key == Key.V && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
                    e.Handled = true;
            }
        }

        private void SearchClientForSubTextBox_DataObjectPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                string digits = new string(text.Where(char.IsDigit).ToArray());
                var textBox = sender as TextBox;
                string fullText = "+7" + digits;
                if (fullText.Length > 12) fullText = fullText.Substring(0, 12);
                textBox.Text = fullText;
                textBox.CaretIndex = fullText.Length;
                e.CancelCommand();
            }
        }

        private void SearchClientForSubTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (!Regex.IsMatch(e.Text, @"^[0-9]+$")) { e.Handled = true; return; }
            string fullText = textBox.Text.Insert(textBox.CaretIndex, e.Text);
            if (fullText.Length > 12) { e.Handled = true; return; }
            if (textBox.CaretIndex < 2) { e.Handled = true; }
        }

        private void ShowSelectedClient(Users client)
        {
            SelectedClientPanel.Visibility = Visibility.Visible;
            string fullName = $"{client.LastName} {client.FirstName}";
            if (!string.IsNullOrWhiteSpace(client.Patronymic)) fullName += $" {client.Patronymic}";
            SelectedClientInfo.Text = $"👤 {fullName}\n📞 {client.Phone}";
            SelectedClientMedical.Text = "✅ Клиент найден в базе данных";
        }

        private void ClearSelectedClient()
        {
            _selectedClient = null;
            _selectedClientSubscription = null;
            _clientSubscriptions.Clear();
            SelectedClientPanel.Visibility = Visibility.Collapsed;
            CbClientSubscriptions.Items.Clear();
            SubscriptionInfoBorder.Visibility = Visibility.Collapsed;
            TbNoSubscriptions.Visibility = Visibility.Collapsed;
            BtnDeductVisit.IsEnabled = false;
            ValidateForm();
        }

        private void CbSubscription_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbSubscription.SelectedItem is Subscriptions subscription)
            {
                _selectedSubscription = subscription;
                FillSubscriptionInfo(subscription);
                CalculatePrice();
            }
            else { ClearSubscriptionInfo(); }
            ValidateForm();
        }

        private void FillSubscriptionInfo(Subscriptions subscription)
        {
            var subscriptionType = AppConnect.modelBD.SubscriptionTypes.FirstOrDefault(t => t.TypeId == subscription.TypeId);
            TbSubscriptionType.Text = subscriptionType?.TypeName ?? "Не указан";
            TbMaxVisits.Text = subscription.MaxVisits >= 999 ? "Безлимит" : subscription.MaxVisits.ToString();
            TbDuration.Text = $"{subscription.DurationDays} дн.";
            TbSubscriptionComment.Text = subscription.Comment ?? "Не указан";
            _basePrice = subscription.Price;
            TbBasePrice.Text = $"{_basePrice:F2} ₽";

            _subscriptionDiscountPercent = 0;
            if (subscription.DiscountId.HasValue)
            {
                var disc = AppConnect.modelBD.Discounts
                    .FirstOrDefault(d => d.DiscountId == subscription.DiscountId.Value);
                if (disc != null && disc.DiscountPercent.HasValue)
                {
                    _subscriptionDiscountPercent = disc.DiscountPercent.Value;
                }
            }
            CalculatePrice();
        }

        private void ClearSubscriptionInfo()
        {
            _selectedSubscription = null;
            TbSubscriptionType.Text = "-";
            TbMaxVisits.Text = "-";
            TbDuration.Text = "-";
            TbSubscriptionComment.Text = "-";
            TbBasePrice.Text = "0 ₽";
            _basePrice = 0;
        }

        private void CbDiscount_SelectionChanged(object sender, SelectionChangedEventArgs e) => CalculatePrice();

        private void CalculatePrice()
        {
            if (_basePrice == 0)
            {
                TbPriceWithoutDiscount.Text = "0 ₽";
                TbDiscountAmount.Text = "0 ₽";
                TbTotalAmount.Text = "0 ₽";
                return;
            }

            decimal afterSubscriptionDiscount = _basePrice * (1 - _subscriptionDiscountPercent / 100m);

            _extraDiscountPercent = 0;
            if (CbDiscount.SelectedItem is Discounts extraDisc && extraDisc.DiscountPercent.HasValue)
            {
                _extraDiscountPercent = extraDisc.DiscountPercent.Value;
            }

            decimal totalPrice = afterSubscriptionDiscount * (1 - _extraDiscountPercent / 100m);

            TbPriceWithoutDiscount.Text = $"{_basePrice:F2} ₽";
            TbDiscountAmount.Text = $"-{(_basePrice - totalPrice):F2} ₽ " +
                                   $"({_subscriptionDiscountPercent + _extraDiscountPercent:F0}%)";
            TbTotalAmount.Text = $"{totalPrice:F2} ₽";
        }

        private void ValidateForm()
        {
            bool isValid = _selectedClient != null &&
                          _selectedSubscription != null &&
                          CbPaymentMethod.SelectedItem != null &&
                          CbMedicalGroup.SelectedItem != null;
            BtnPurchaseSubscription.IsEnabled = isValid;
        }

        private void CbPaymentMethod_SelectionChanged(object sender, SelectionChangedEventArgs e) => ValidateForm();
        private void CbMedicalGroup_SelectionChanged(object sender, SelectionChangedEventArgs e) => ValidateForm();

        private void BtnGoToClients_Click(object sender, RoutedEventArgs e) => MainTabControl.SelectedIndex = 0;

        private void BtnPurchaseSubscription_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedClient == null) { MessageBox.Show("❌ Выберите клиента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (_selectedSubscription == null) { MessageBox.Show("❌ Выберите абонемент!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (CbMedicalGroup.SelectedItem == null) { MessageBox.Show("❌ Выберите медицинскую группу!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (CbPaymentMethod.SelectedItem == null) { MessageBox.Show("❌ Выберите способ оплаты!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                var medicalGroup = CbMedicalGroup.SelectedItem as MedicalGroups;
                var paymentMethod = CbPaymentMethod.SelectedItem as PaymentMethods;
                var activeStatus = AppConnect.modelBD.SubscriptionStatuses.FirstOrDefault(s => s.StatusId == 1);
                if (activeStatus == null) { MessageBox.Show("❌ Ошибка: не найден статус 'Активен'!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); return; }

                decimal afterSubDisc = _basePrice * (1 - _subscriptionDiscountPercent / 100m);
                decimal finalAmount = afterSubDisc * (1 - _extraDiscountPercent / 100m);

                var activatedSubscription = new ActivatedSubscriptions
                {
                    UserId = _selectedClient.UserId,
                    SubscriptionId = _selectedSubscription.SubscriptionId,
                    MedicalGroupId = medicalGroup.MedicalGroupId,
                    ActivationDate = DateTime.Now.Date,
                    ExpiryDate = DateTime.Now.Date.AddDays(_selectedSubscription.DurationDays),
                    StatusId = activeStatus.StatusId,
                    PaymentMethodId = paymentMethod.MethodId,
                    PaidAmount = finalAmount,
                    VisitsRemaining = _selectedSubscription.MaxVisits  // ✅ ПРАВИЛЬНО!
                };

                AppConnect.modelBD.ActivatedSubscriptions.Add(activatedSubscription);
                AppConnect.modelBD.SaveChanges();

                ShowReceipt(activatedSubscription, _selectedClient, _selectedSubscription, medicalGroup, paymentMethod, finalAmount);

                MessageBox.Show($"✅ Абонемент успешно оформлен!\n\n" +
                    $"Номер активации: {activatedSubscription.ActivationId}\n" +
                    $"Клиент: {_selectedClient.LastName} {_selectedClient.FirstName}\n" +
                    $"Абонемент: {_selectedSubscription.NameSubscriptionId}\n" +
                    $"Сумма: {finalAmount:F2} ₽\n" +
                    $"Срок действия: до {activatedSubscription.ExpiryDate:dd.MM.yyyy}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка при оформлении абонемента:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowReceipt(ActivatedSubscriptions activation, Users client, Subscriptions subscription, MedicalGroups medicalGroup, PaymentMethods paymentMethod, decimal finalAmount)
        {
            string receiptText = $@"
═══════════════════════════════════════
        СПОРТИВНЫЙ КОМПЛЕКС
           ""АКВАМАРИН""
        ЧЕК ОБ ОПЛАТЕ
═══════════════════════════════════════

📋 ИНФОРМАЦИЯ ОБ ОПЕРАЦИИ
───────────────────────────────────────
№ операции:     {activation.ActivationId:D8}
Дата:           {activation.ActivationDate:dd.MM.yyyy}
Время:          {DateTime.Now:HH:mm:ss}

👤 КЛИЕНТ
───────────────────────────────────────
ФИО:            {client.LastName} {client.FirstName} {(string.IsNullOrWhiteSpace(client.Patronymic) ? "" : client.Patronymic)}
Телефон:        {client.Phone}
Мед. группа:    {medicalGroup.GroupName}

🏊 АБОНЕМЕНТ
───────────────────────────────────────
Название:       {subscription.NameSubscriptionId}
Тип:            {(AppConnect.modelBD.SubscriptionTypes.FirstOrDefault(t => t.TypeId == subscription.TypeId)?.TypeName ?? "Не указан")}
Количество:     {(subscription.MaxVisits >= 999 ? "Безлимит" : subscription.MaxVisits.ToString() + " занятий")}
Срок действия:  {subscription.DurationDays} дн.

💰 ОПЛАТА
───────────────────────────────────────
Базовая цена:   {_basePrice:F2} ₽
Скидка абонемента: {_subscriptionDiscountPercent:F0}%
Доп. скидка:    {_extraDiscountPercent:F0}%
───────────────────────────────────────
ИТОГО:          {finalAmount:F2} ₽

Способ оплаты:  {paymentMethod.MethodName}

───────────────────────────────────────
Активирован:    {activation.ActivationDate:dd.MM.yyyy}
Действителен до:{activation.ExpiryDate:dd.MM.yyyy}

═══════════════════════════════════════
      Спасибо за покупку!
═══════════════════════════════════════
";

            var receiptWindow = new Window
            {
                Title = "Чек об оплате - №" + activation.ActivationId.ToString("D8"),
                Width = 450,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Colors.White),
                Owner = this
            };

            var textBlock = new TextBlock { Text = receiptText, FontFamily = new FontFamily("Consolas"), FontSize = 12, Margin = new Thickness(20), TextWrapping = TextWrapping.Wrap };
            var scrollViewer = new ScrollViewer { Content = textBlock, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(10) };
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 10) };

            var printButton = new Button { Content = "🖨️ ПЕЧАТЬ", Width = 120, Height = 35, Margin = new Thickness(10), Background = new SolidColorBrush(Color.FromRgb(180, 213, 232)), Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 14 };
            printButton.Click += (s, args) => PrintReceipt(receiptText);
            var closeButton = new Button { Content = "ЗАКРЫТЬ", Width = 120, Height = 35, Margin = new Thickness(10), Background = Brushes.LightGray, FontWeight = FontWeights.Bold, FontSize = 14 };
            closeButton.Click += (s, args) => receiptWindow.Close();

            buttonPanel.Children.Add(printButton);
            buttonPanel.Children.Add(closeButton);
            var mainPanel = new StackPanel();
            mainPanel.Children.Add(scrollViewer);
            mainPanel.Children.Add(buttonPanel);
            receiptWindow.Content = mainPanel;
            receiptWindow.ShowDialog();
        }

        private void PrintReceipt(string receiptText)
        {
            MessageBox.Show("🖨️ ЧЕК ОТПРАВЛЕН НА ПЕЧАТЬ", "Печать чека", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearForm()
        {
            InitializeSearchField();
            ClearSelectedClient();
            ClientNotFoundHint.Text = "ℹ️ Клиент не найден? Перейдите на вкладку «КЛИЕНТЫ» для регистрации нового.";
            ClientNotFoundHint.Foreground = new SolidColorBrush(Color.FromRgb(180, 213, 232));
            CbSubscription.SelectedIndex = -1;
            ClearSubscriptionInfo();
            CbDiscount.SelectedIndex = 0;
            CalculatePrice();
            CbPaymentMethod.SelectedIndex = -1;
            CbMedicalGroup.SelectedIndex = -1;
            ChkPrintReceipt.IsChecked = false;
            ValidateForm();
        }

        private void SearchClientsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string filter = SearchClientsTextBox.Text?.Trim().ToLower();
                if (string.IsNullOrEmpty(filter)) { LoadClients(); return; }
                var filteredClients = AppConnect.modelBD.Users
                    .Where(u => u.RoleId == 1 &&
                               (u.LastName.ToLower().Contains(filter) ||
                                u.FirstName.ToLower().Contains(filter) ||
                                (u.Patronymic != null && u.Patronymic.ToLower().Contains(filter)) ||
                                u.Phone.Contains(filter)))
                    .OrderBy(u => u.LastName).ToList();
                DgClients.ItemsSource = filteredClients;
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка при поиске: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnAddClient_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddClientWindow();
            if (win.ShowDialog() == true) { LoadClients(); MessageBox.Show("Клиент успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information); }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Завершить сеанс и выйти из системы?",
                "Выход",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var authWindow = new MainWindow();
                authWindow.Show();
                this.Close();
            }
        }
    }
}