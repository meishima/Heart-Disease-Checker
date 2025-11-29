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
        MLContext mLContext = new MLContext();
        DataViewSchema modelSchema;
        ITransformer trainedModel = mLContext.Model.Load("heartmodel.zip", out modelSchema);

        var predEngine = mLContext.Model.CreatePredictionEngine<HeartData, HeartPrediction>(trainedModel);

        float genderValue = 0;

        string genderInputText = GenderInput.Text?.ToLower().Trim() ?? "";

        if(genderInputText == "male" || genderInputText == "m" || genderInputText == "erkek" || genderInputText == "1")
        {
            genderValue = 1;
        }
        else
        {
            genderValue = 0;
        }

        var input = new HeartData()
        {
            Yas = (float)(AgeInput.Value ?? 0),
            Cinsiyet = genderValue,
            Tansiyon = float.Parse(BloodPressureInput.Text ?? "0"),
            Kolestrol = float.Parse(CholesterolInput.Text ?? "0")
        };

        var result = predEngine.Predict(input);

        if(result.Prediction)
        {
            TextResult.Text = $"High risk of heart disease with a probability of {result.Probability * 100:F1}";
            TextResult.Foreground = Brushes.Red;
        }
        else
        {
            TextResult.Text = "Low risk of heart disease!";
            TextResult.Foreground = Brushes.Green;
        }
    }
}