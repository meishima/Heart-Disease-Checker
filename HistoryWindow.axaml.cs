using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Metadata;

namespace HeartDiseaseChecker;

public partial class HistoryWindow : Window
{
    public HistoryWindow()
    {
        InitializeComponent();
        LoadData();
    }

    private void LoadData()
    {
        HistoryPanel.Children.Clear();

        var records = DatabaseManager.GetRecords();
        foreach (var record in records)
        {
            var border = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                Background = record.Probability > 0.5 ? Brushes.DarkRed : Brushes.DarkGreen
            };

            var textBlock = new TextBlock
            {
                Text = $"{record.Date} | Risk: %{record.Probability * 100:F1} | {record.Gender} | Age: {record.Age} | BP: {record.BloodPressure} | Chol: {record.Cholesterol} | Fbs: {record.BloodSugar} | CP: {record.ChestPainType} | Exang: {record.ExerciseInducedAngina}",
                FontSize = 14,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            };

            border.Child = textBlock;

            HistoryPanel.Children.Add(border);
        }

        if(records.Count == 0)
        {
            HistoryPanel.Children.Add(new TextBlock
            {
                Text = "No records found.",
            });
        }
    }

    private void BtnDeleteAll_Click(object sender, RoutedEventArgs e)
    {
        DatabaseManager.DeleteAllRecords();
        LoadData();
    }
}