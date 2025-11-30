using System.Security.Cryptography.X509Certificates;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Microsoft.ML;
using System;

namespace HeartDiseaseChecker;

public partial class MainWindow : Window
{
    // Temporary variables for "SaveBtn_Click" function
    private HeartData? _tempData;
    private HeartPrediction? _tempPrediction;
    public MainWindow()
    {
        InitializeComponent();
        SaveBtn.IsEnabled = false;
    }
    public void BtnCalculate_Click(object sender, RoutedEventArgs e)
    {
        SaveText.Children.Clear();

        if(AgeInput.Value == null)
        {
            TextResult.Text = "Please enter a valid age.";
            TextResult.Foreground = Brushes.OrangeRed;
            return;
        }

        if(!float.TryParse(BloodPressureInput.Text, out float bpValue) ||
           !float.TryParse(CholesterolInput.Text, out float cholValue))
        {
            TextResult.Text = "Please enter valid numeric values for Blood Pressure and Cholesterol.";
            TextResult.Foreground = Brushes.OrangeRed;
            return;
        }

        int genderVal = MaleInput.IsChecked == true ? 0 : FemaleInput.IsChecked == true ? 1 : -1;
        int fbsVal = FbsYesInput.IsChecked == true ? 0 : FbsNoInput.IsChecked == true ? 1 : -1;
        int cpVal = Cp0Input.IsChecked == true ? 0 : 
                    Cp1Input.IsChecked == true ? 1 :
                    Cp2Input.IsChecked == true ? 2 : 
                    Cp3Input.IsChecked == true ? 3 : -1;
        int exangVal = ExangYesInput.IsChecked == true ? 0 : ExangNoInput.IsChecked == true ? 1 : -1;

        if(genderVal == -1 || fbsVal == -1 || cpVal == -1 || exangVal == -1)
        {
            TextResult.Text = "Please make sure to select all options.";
            TextResult.Foreground = Brushes.OrangeRed;
            return;
        }
        SaveBtn.IsEnabled = true;

        MLContext mLContext = new MLContext();
        DataViewSchema modelSchema;
        ITransformer trainedModel = mLContext.Model.Load("heartmodel.zip", out modelSchema);

        var predEngine = mLContext.Model.CreatePredictionEngine<HeartData, HeartPrediction>(trainedModel);

        var input = new HeartData()
        {
            Age = (float)AgeInput.Value,
            Gender = genderVal,
            BloodPressure = bpValue,
            Cholesterol = cholValue,
            BloodSugar = fbsVal,
            ChestPainType = cpVal,
            ExerciseInducedAngina = exangVal,
        };

        var result = predEngine.Predict(input);

        _tempData = input;
        _tempPrediction = result;

        if(result.Prediction)
        {
            TextResult.Text = $"High risk of heart disease with a probability of {result.Probability * 100:F1}";
            TextResult.Foreground = Brushes.Red;
        }
        else
        {
            TextResult.Text = $"Low risk of heart disease! (Probability: {result.Probability * 100:F1}%)";
            TextResult.Foreground = Brushes.Green;
        }

        AdvicePanel.Children.Clear();
        if(bpValue >= 180)
        {
            AddAdvice("You are in Hypertensive Crisis! Seek emergency help immediately!", Brushes.DarkRed);
        } 
        else if(bpValue >= 140)
        {
            AddAdvice("You are in High Blood Pressure Stage 2! Consult a doctor immediately.", Brushes.Red);
        } 
        else if(bpValue >= 130)
        {
            AddAdvice("You are in High Blood Pressure Stage 1!", Brushes.OrangeRed);
        } 
        else if(bpValue >= 120)
        {
            AddAdvice("Your blood pressure is elevated.", Brushes.Yellow);
        }   

        if(cholValue >= 240)
        {
            AddAdvice("Your cholesterol level is high! Consult a doctor immediately.", Brushes.Red);
        }
        else if(cholValue >= 200)
        {
            AddAdvice("Your cholesterol level is borderline-high! Consult a doctor.", Brushes.OrangeRed);
        }
        RiskBar.Value = result.Probability * 100;
    }
    private void AddAdvice(string message, IBrush color)
    {
        var textBlock = new TextBlock
        {
            Text = message,
            Foreground = color,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };

        AdvicePanel.Children.Add(textBlock);    
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        AgeInput.Value = null;
        MaleInput.IsChecked = false;
        FemaleInput.IsChecked = false;
        BloodPressureInput.Text = "";
        CholesterolInput.Text = "";
        FbsYesInput.IsChecked = false;
        FbsNoInput.IsChecked = false;
        Cp0Input.IsChecked = false;
        Cp1Input.IsChecked = false;
        Cp2Input.IsChecked = false;
        Cp3Input.IsChecked = false;
        ExangYesInput.IsChecked = false;
        ExangNoInput.IsChecked = false;
        TextResult.Text = "Click \"Analyze Risk\" after filling all fields to see the analysis";
        TextResult.Foreground = Brushes.White;
        RiskBar.Value = 0;
        AdvicePanel.Children.Clear();
        SaveBtn.IsEnabled = false;
        _tempData = null;
        _tempPrediction = null;
        SaveText.Children.Clear();
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if(_tempData == null || _tempPrediction == null)
        {
            return;
        }
        DatabaseManager.InsertRecord(
            DateTime.Now,
            _tempData.Age,
            _tempData.Gender == 0 ? "Male" : "Female",
            _tempData.BloodPressure,
            _tempData.Cholesterol,
            _tempData.BloodSugar == 0 ? "Yes" : "No",
            _tempData.ChestPainType switch
            {
                0 => "Typical Angina",
                1 => "Atypical Angina",
                2 => "Non-Anginal Pain",
                3 => "Asymptomatic",
                _ => "Unknown"
            },
            _tempData.ExerciseInducedAngina == 0 ? "Yes" : "No",
            _tempPrediction!.Probability
        );

        SaveText.Children.Add(
            new TextBlock
            {
                Text = "Result has saved to the database.",
                Foreground = Brushes.LightGreen,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            }
        );
        SaveBtn.IsEnabled = false;
    }

    private void BtnHistory_Click(object sender, RoutedEventArgs e)
    {
        var historyWindow = new HistoryWindow();
        historyWindow.ShowDialog(this);
    }
}
