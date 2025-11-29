using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Microsoft.ML;

namespace HeartDiseaseChecker;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void BtnCalculate_Click(object sender, RoutedEventArgs e)
    {
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

        int genderVal = MaleInput.IsChecked == true ? 1 : FemaleInput.IsChecked == true ? 0 : -1;
        int fbsVal = FbsYesInput.IsChecked == true ? 1 : FbsNoInput.IsChecked == true ? 0 : -1;
        int cpVal = Cp0Input.IsChecked == true ? 0 : 
                    Cp1Input.IsChecked == true ? 1 :
                    Cp2Input.IsChecked == true ? 2 : 
                    Cp3Input.IsChecked == true ? 3 : -1;
        int exangVal = ExangYesInput.IsChecked == true ? 1 : ExangNoInput.IsChecked == true ? 0 : -1;

        if(genderVal == -1 || fbsVal == -1 || cpVal == -1 || exangVal == -1)
        {
            TextResult.Text = "Please make sure to select all options.";
            TextResult.Foreground = Brushes.OrangeRed;
            return;
        }

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
    }
}