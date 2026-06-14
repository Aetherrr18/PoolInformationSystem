using System.Windows;

namespace PoolInformation.WindowAuthorization  // ← Должно точно совпадать!
{
    public partial class PersonalDataConsentWindow : Window
    {
        public PersonalDataConsentWindow()
        {
            InitializeComponent();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}