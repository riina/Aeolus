using Aeolus.ModelProxies;

namespace Aeolus;

public partial class RemediationEntry : ContentView
{
    public RemediationEntry()
    {
        InitializeComponent();
    }

    private async void FixBtn_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is Remediation remediation)
            await App.Me!.RunRemediationAsync(remediation);
    }
}
