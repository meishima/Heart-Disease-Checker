using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

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

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = $"{record.Date} | Risk: %{record.Probability * 100:F1} | {record.Gender} | Age: {record.Age} | BP: {record.BloodPressure} | Chol: {record.Cholesterol} | Fbs: {record.BloodSugar} | CP: {record.ChestPainType} | Exang: {record.ExerciseInducedAngina}",
                FontSize = 14,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            });

            Grid.SetColumn(stack, 0);
            grid.Children.Add(stack);

            var deleteBtn = new Button
            {
                Content = "Delete",
                Background = Brushes.Red,
                Foreground = Brushes.White,
                Margin = new Thickness(10, 0, 0, 0),
            };

            deleteBtn.Click += (s, e) =>
            {
                DatabaseManager.DeleteRecord(record.Id);
                LoadData();
            };

            Grid.SetColumn(deleteBtn, 1);
            grid.Children.Add(deleteBtn);

            border.Child = grid;

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
}