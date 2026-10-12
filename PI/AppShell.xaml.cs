using PI.Views;

namespace PI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(CredentialDetailPage), typeof(CredentialDetailPage));
        }
    }
}
