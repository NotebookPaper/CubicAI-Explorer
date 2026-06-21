using System.Globalization;
using System.Windows;

namespace CubicAIExplorer.Views;

public partial class RothIraCalculatorDialog : Window
{
    public RothIraCalculatorDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => Recalculate();
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded) return;
        Recalculate();
    }

    private void Recalculate()
    {
        int childAge = (int)AgeSlider.Value;
        double monthlyDeposit = MonthlyDepositSlider.Value;
        double annualRate = ReturnRateSlider.Value / 100.0;

        int yearsUntilRetirement = Math.Max(65 - childAge, 0);
        int totalMonths = yearsUntilRetirement * 12;
        double monthlyRate = annualRate / 12.0;

        double futureValue = 0;
        if (monthlyRate > 0 && totalMonths > 0)
        {
            futureValue = monthlyDeposit * ((Math.Pow(1 + monthlyRate, totalMonths) - 1) / monthlyRate);
        }
        else if (totalMonths > 0)
        {
            futureValue = monthlyDeposit * totalMonths;
        }

        double totalContributed = monthlyDeposit * totalMonths;
        double interestEarned = futureValue - totalContributed;

        AgeDisplay.Text = childAge.ToString();
        DepositDisplay.Text = monthlyDeposit.ToString("N0");
        ReturnRateDisplay.Text = ReturnRateSlider.Value.ToString("0.#");
        YearsDisplay.Text = yearsUntilRetirement.ToString();
        TotalContributedDisplay.Text = totalContributed.ToString("C0", CultureInfo.GetCultureInfo("en-US"));
        InterestEarnedDisplay.Text = interestEarned.ToString("C0", CultureInfo.GetCultureInfo("en-US"));
        FutureValueDisplay.Text = futureValue.ToString("C0", CultureInfo.GetCultureInfo("en-US"));
    }
}
