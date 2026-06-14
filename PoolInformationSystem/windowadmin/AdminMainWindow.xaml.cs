using PoolInformationSystem.ApplicationData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PoolInformation.WindowAdmin
{
    /// <summary>
    /// Логика взаимодействия для AdminMainWindow.xaml
    /// </summary>
    public partial class AdminMainWindow : Window
    {
        public AdminMainWindow()
        {
            InitializeComponent();
            DgClients.ItemsSource = SwimSubscriptionsDBEntities.GetContext().Users.ToList();
            DgSubscriptions.ItemsSource = SwimSubscriptionsDBEntities.GetContext().Subscriptions.ToList();
            DgUsers.ItemsSource = SwimSubscriptionsDBEntities.GetContext().Users.ToList();
            DgMedicalGroups.ItemsSource = SwimSubscriptionsDBEntities.GetContext().MedicalGroups.ToList();
            DgDiscounts.ItemsSource = SwimSubscriptionsDBEntities.GetContext().Discounts.ToList();
            DgTypes.ItemsSource = SwimSubscriptionsDBEntities.GetContext().SubscriptionTypes.ToList();
            LoadClients();
            LoadSubscriptions();
            LoadUsers();
            LoadMedicalGroups();
            LoadDiscounts();
            LoadSubscriptionTypes();
        }

        // КЛИЕНТЫ 
        private void LoadClients()
        {
            try
            {
                var clients = AppConnect.modelBD.Users
                    .Where(u => u.RoleId == 1) // Только клиенты
                    .OrderBy(u => u.LastName)  // Сортировка по Фамилии
                    .ThenBy(u => u.FirstName)
                    .ToList();
                DgClients.ItemsSource = clients;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SearchClientsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string filter = SearchClientsTextBox.Text?.Trim();

                if (string.IsNullOrEmpty(filter))
                {
                    // Если поле пустое — загружаем всех клиентов
                    LoadClients();
                    return;
                }

                // Фильтрация по ФИО и телефону, только для клиентов (RoleId == 1)
                var filteredClients = AppConnect.modelBD.Users
                    .Where(u => u.RoleId == 1 &&
                               (u.LastName.Contains(filter) ||
                                u.FirstName.Contains(filter) ||
                                (u.Patronymic != null && u.Patronymic.Contains(filter)) ||
                                u.Phone.Contains(filter)))
                    .ToList();

                DgClients.ItemsSource = filteredClients;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteClient_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = DgClients.SelectedItems.Cast<Users>().ToList();
            if (!selectedItems.Any())
            {
                MessageBox.Show("Выберите клиентов для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Вы действительно хотите удалить {selectedItems.Count} клиентов?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var client in selectedItems)
                    {
                        // Удаляем связанные активации (если есть)
                        var activations = AppConnect.modelBD.ActivatedSubscriptions
                            .Where(a => a.UserId == client.UserId)
                            .ToList();
                        AppConnect.modelBD.ActivatedSubscriptions.RemoveRange(activations);

                        // Удаляем самого клиента
                        AppConnect.modelBD.Users.Remove(client);
                    }

                    AppConnect.modelBD.SaveChanges();
                    LoadClients();
                    MessageBox.Show("Клиенты успешно удалены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        //АБОНЕМЕНТЫ

        private void LoadSubscriptions()
        {
            try
            {
                var subs = AppConnect.modelBD.Subscriptions.ToList();
                DgSubscriptions.ItemsSource = subs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки абонементов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddSubscription_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddEditSubscriptionWindow();
            if (win.ShowDialog() == true)
            {
                LoadSubscriptions(); // Обновить список
            }
        }

        private void BtnEditSubscription_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgSubscriptions.SelectedItem as Subscriptions;
            if (selected == null)
            {
                MessageBox.Show("Выберите абонемент для редактирования.", "Информация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var win = new AddEditSubscriptionWindow(selected);
            if (win.ShowDialog() == true)
            {
                LoadSubscriptions();
            }
        }

        private void BtnDeleteSubscription_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = DgSubscriptions.SelectedItems.Cast<Subscriptions>().ToList();
            if (!selectedItems.Any())
            {
                MessageBox.Show("Выберите абонементы для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Вы действительно хотите удалить {selectedItems.Count} абонемент(ов)?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // Проверка: используется ли абонемент в активных продажах?
                    foreach (var sub in selectedItems)
                    {
                        bool hasActive = AppConnect.modelBD.ActivatedSubscriptions
                            .Any(a => a.SubscriptionId == sub.SubscriptionId && a.StatusId == 1); // Активен

                        if (hasActive)
                        {
                            MessageBox.Show($"Нельзя удалить абонемент \"{sub.NameSubscriptionId}\", так как он используется в активных продажах.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    AppConnect.modelBD.Subscriptions.RemoveRange(selectedItems);
                    AppConnect.modelBD.SaveChanges();
                    LoadSubscriptions();
                    MessageBox.Show("Абонементы успешно удалены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        //СПРАВОЧНИКИКИ

        // Мед группа
        private void LoadMedicalGroups()
        {
            try
            {
                var medicalGroups = AppConnect.modelBD.MedicalGroups
                    .OrderBy(g => g.GroupName)
                    .ToList();
                DgMedicalGroups.ItemsSource = medicalGroups;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки медицинских групп: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddMedicalGroup_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddEditMedicalGroupWindow();
            if (win.ShowDialog() == true)
            {
                LoadMedicalGroups(); // Обновляем список после добавления
            }
        }

        private void BtnEditMedicalGroup_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgMedicalGroups.SelectedItem as MedicalGroups;
            if (selected == null)
            {
                MessageBox.Show("Выберите медицинскую группу для редактирования.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new AddEditMedicalGroupWindow(selected);
            if (win.ShowDialog() == true)
            {
                LoadMedicalGroups(); // Обновляем список после редактирования
            }
        }

        private void BtnDeleteMedicalGroup_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgMedicalGroups.SelectedItem as MedicalGroups;
            if (selected == null)
            {
                MessageBox.Show("Выберите медицинскую группу для удаления.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Проверка: есть ли зависимости (активированные абонементы с этой группой)
            var hasDependencies = AppConnect.modelBD.ActivatedSubscriptions
                .Any(a => a.MedicalGroupId == selected.MedicalGroupId);

            if (hasDependencies)
            {
                MessageBox.Show("Невозможно удалить: медицинская группа используется в абонементах.",
                    "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Удалить медицинскую группу \"{selected.GroupName}\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    AppConnect.modelBD.MedicalGroups.Remove(selected);
                    AppConnect.modelBD.SaveChanges();
                    LoadMedicalGroups();
                    MessageBox.Show("Медицинская группа успешно удалена.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        //Скидки 

        private void LoadDiscounts()
        {
            try
            {
                var discounts = AppConnect.modelBD.Discounts
                    .OrderBy(d => d.DiscountName)
                    .ToList();
                DgDiscounts.ItemsSource = discounts;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки скидок: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddDiscount_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddEditDiscountWindow();
            if (win.ShowDialog() == true)
            {
                LoadDiscounts(); // Обновляем список после добавления
            }
        }

        private void BtnEditDiscount_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgDiscounts.SelectedItem as Discounts;
            if (selected == null)
            {
                MessageBox.Show("Выберите скидку для редактирования.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new AddEditDiscountWindow(selected);
            if (win.ShowDialog() == true)
            {
                LoadDiscounts(); // Обновляем список после редактирования
            }
        }

        private void BtnDeleteDiscount_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgDiscounts.SelectedItem as Discounts;
            if (selected == null)
            {
                MessageBox.Show("Выберите скидку для удаления.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Проверка: используется ли скидка в абонементах
            var isUsed = AppConnect.modelBD.Subscriptions
                .Any(s => s.DiscountId == selected.DiscountId);

            if (isUsed)
            {
                MessageBox.Show("Невозможно удалить: скидка используется в абонементах.",
                    "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Удалить скидку \"{selected.DiscountName}\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    AppConnect.modelBD.Discounts.Remove(selected);
                    AppConnect.modelBD.SaveChanges();
                    LoadDiscounts();
                    MessageBox.Show("Скидка успешно удалена.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        //Тип абонементов

        private void LoadSubscriptionTypes()
        {
            try
            {
                var types = AppConnect.modelBD.SubscriptionTypes
                    .OrderBy(t => t.TypeName)
                    .ToList();
                DgTypes.ItemsSource = types;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов абонементов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddType_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddEditSubscriptionTypeWindow();
            if (win.ShowDialog() == true)
            {
                LoadSubscriptionTypes(); // Обновляем список после добавления
            }
        }

        private void BtnEditType_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgTypes.SelectedItem as SubscriptionTypes;
            if (selected == null)
            {
                MessageBox.Show("Выберите тип абонемента для редактирования.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new AddEditSubscriptionTypeWindow(selected);
            if (win.ShowDialog() == true)
            {
                LoadSubscriptionTypes(); // Обновляем список после редактирования
            }
        }

        private void BtnDeleteType_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgTypes.SelectedItem as SubscriptionTypes;
            if (selected == null)
            {
                MessageBox.Show("Выберите тип абонемента для удаления.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Проверка: используется ли тип в абонементах
            var isUsed = AppConnect.modelBD.Subscriptions
                .Any(s => s.TypeId == selected.TypeId);

            if (isUsed)
            {
                MessageBox.Show("Невозможно удалить: тип абонемента используется в активных предложениях.",
                    "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Удалить тип абонемента \"{selected.TypeName}\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    AppConnect.modelBD.SubscriptionTypes.Remove(selected);
                    AppConnect.modelBD.SaveChanges();
                    LoadSubscriptionTypes();
                    MessageBox.Show("Тип абонемента успешно удалён.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        //ПОЛЬЗОВАТЕЛИ

        private void LoadUsers()
        {
            try
            {
                var users = AppConnect.modelBD.Users
                    .Where(u => u.RoleId == 2 || u.RoleId == 3) // Кассиры и администраторы
                    .OrderBy(u => u.LastName)  // Сортировка по Фамилии
                    .ThenBy(u => u.FirstName)
                    .ToList();
                DgUsers.ItemsSource = users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddEditUserWindow();
            if (win.ShowDialog() == true)
            {
                LoadUsers();
            }
        }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgUsers.SelectedItem as Users;
            if (selected == null)
            {
                MessageBox.Show("Выберите пользователя для редактирования.");
                return;
            }

            var win = new AddEditUserWindow(selected);
            if (win.ShowDialog() == true)
            {
                LoadUsers();
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = DgUsers.SelectedItems.Cast<Users>().ToList();
            if (!selectedItems.Any())
            {
                MessageBox.Show("Выберите пользователей для удаления.");
                return;
            }

            if (MessageBox.Show(
                $"Удалить {selectedItems.Count} пользователей?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    AppConnect.modelBD.Users.RemoveRange(selectedItems);
                    AppConnect.modelBD.SaveChanges();
                    LoadUsers();
                    MessageBox.Show("Пользователи удалены.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления:\n{ex.Message}");
                }
            }
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
                // Закрываем все окна, кроме главного
                foreach (Window window in Application.Current.Windows)
                {
                    if (window != this && window is AdminMainWindow)
                    {
                        window.Close();
                    }
                }

                // Возвращаемся к окну авторизации
                var authWindow = new MainWindow();
                authWindow.Show();
                this.Close();
            }
        }
    }
}