using System.Security.Cryptography.X509Certificates;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using HeartDiseaseChecker.Services;
using HeartDiseaseChecker.Models;

using Microsoft.ML;
using System;
using System.Linq;

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

        if (AgeInput.Value == null)
        {
            TextResult.Text = "Lütfen geçerli bir yaş giriniz.";
            TextResult.Foreground = Brushes.OrangeRed;
            return;
        }

        if (!float.TryParse(BloodPressureInput.Text, out float bpValue) ||
           !float.TryParse(CholesterolInput.Text, out float cholValue))
        {
            TextResult.Text = "Lütfen Kan Basıncı ve Kolesterol için geçerli sayısal değerler giriniz.";
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

        if (genderVal == -1 || fbsVal == -1 || cpVal == -1 || exangVal == -1)
        {
            TextResult.Text = "Lütfen tüm seçenekleri belirlediğinizden emin olun.";
            TextResult.Foreground = Brushes.OrangeRed;
            return;
        }
        if (ModelService.TrainedModels.Count == 0 && string.IsNullOrEmpty("heartmodel.zip")) // Simplified check
        {
            // If no models trained in session, fallback to loading file if exists, or warn.
            // For Phase 5, we prefer session models.
            if (ModelService.TrainedModels.Count == 0)
            {
                // Try loading default just for legacy support or warn
                if (!System.IO.File.Exists("heartmodel.zip"))
                {
                    TextResult.Text = "Lütfen önce 'Veriseti Hesaplamaları' menüsünden modelleri sçip 'Modelleri Eğit' butonuna basınız.";
                    TextResult.Foreground = Brushes.OrangeRed;
                    return;
                }
            }
        }

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

        // Multi-model prediction
        var results = ModelService.Predict(input);

        // If we have multi-model results (session trained)
        if (results.Count > 0)
        {
            ConsensusPanel.IsVisible = true;
            ModelResultsList.ItemsSource = null; // Reset

            int riskVotes = results.Count(x => x.Prediction);
            int totalVotes = results.Count;
            double confidence = (double)riskVotes / totalVotes * 100;

            var items = new System.Collections.Generic.List<string>();
            foreach (var r in results)
            {
                string status = r.Prediction ? "RİSKLİ" : "SAĞLIKLI";
                items.Add($"{r.ModelName}: {status} (%{r.Probability * 100:F1})");
            }
            ModelResultsList.ItemsSource = items;

            if (riskVotes > totalVotes / 2)
            {
                TextResult.Text = $"YÜKSEK RİSK! ({totalVotes} modelden {riskVotes}'ü onayladı)";
                TextResult.Foreground = Brushes.Red;
                ConsensusText.Text = $"Güven Skoru: %{confidence:F1}";
                RiskBar.Value = confidence;
                _tempPrediction = new HeartPrediction { Prediction = true, Probability = (float)confidence / 100f };
            }
            else
            {
                TextResult.Text = $"Düşük Risk. ({totalVotes} modelden {totalVotes - riskVotes}'si temiz dedi)";
                TextResult.Foreground = Brushes.Green;
                ConsensusText.Text = $"Güven Skoru: %{100 - confidence:F1}";
                RiskBar.Value = confidence;
                _tempPrediction = new HeartPrediction { Prediction = false, Probability = (float)confidence / 100f };
            }
        }
        else // Legacy single model fallback
        {
            ConsensusPanel.IsVisible = false;
            MLContext mLContext = new MLContext();
            DataViewSchema modelSchema;
            ITransformer trainedModel = mLContext.Model.Load("heartmodel.zip", out modelSchema);
            var predEngine = mLContext.Model.CreatePredictionEngine<HeartData, HeartPrediction>(trainedModel);
            var result = predEngine.Predict(input);

            // ... (keep legacy display logic or refactor? Let's keep it simple for now as reliable fallback)
            _tempData = input;
            _tempPrediction = result;

            if (result.Prediction)
            {
                TextResult.Text = $"Kalp hastalığı riski YÜKSEK! (Olasılık: %{result.Probability * 100:F1})";
                TextResult.Foreground = Brushes.Red;
            }
            else
            {
                TextResult.Text = $"Kalp hastalığı riski Düşük! (Olasılık: %{result.Probability * 100:F1})";
                TextResult.Foreground = Brushes.Green;
            }
            RiskBar.Value = result.Probability * 100;
        }

        _tempData = input;

        AdvicePanel.Children.Clear();
        if (bpValue >= 180)
        {
            AddText("Hipertansif Krizdesiniz! Acilen yardım alın!", Brushes.DarkRed, AdvicePanel);
        }
        else if (bpValue >= 140)
        {
            AddText("2. Evre Yüksek Tansiyon! Derhal bir doktora danışın.", Brushes.Red, AdvicePanel);
        }
        else if (bpValue >= 130)
        {
            AddText("1. Evre Yüksek Tansiyon!", Brushes.OrangeRed, AdvicePanel);
        }
        else if (bpValue >= 120)
        {
            AddText("Tansiyonunuz yüksek.", Brushes.Yellow, AdvicePanel);
        }

        if (cholValue >= 240)
        {
            AddText("Kolesterol seviyeniz yüksek! Derhal bir doktora danışın.", Brushes.Red, AdvicePanel);
        }
        else if (cholValue >= 200)
        {
            AddText("Kolesterol seviyeniz sınırda yüksek! Bir doktora danışın.", Brushes.OrangeRed, AdvicePanel);
        }
    }
    private void AddText(string message, IBrush color, StackPanel panel)
    {
        var textBlock = new TextBlock
        {
            Text = message,
            Foreground = color,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };

        panel.Children.Add(textBlock);
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
        TextResult.Text = "Analizi görmek için tüm alanları doldurup \"Risk Analizi\"ne tıklayın";
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
        if (_tempData == null || _tempPrediction == null)
        {
            return;
        }
        DatabaseManager.InsertRecord(
            DateTime.Now,
            _tempData.Age,
            _tempData.Gender == 0 ? "Erkek" : "Kadın",
            _tempData.BloodPressure,
            _tempData.Cholesterol,
            _tempData.BloodSugar == 0 ? "Evet" : "Hayır",
            _tempData.ChestPainType switch
            {
                0 => "Tipik Anjina",
                1 => "Atipik Anjina",
                2 => "Anjinal Olmayan Ağrı",
                3 => "Asemptomatik",
                _ => "Bilinmiyor"
            },
            _tempData.ExerciseInducedAngina == 0 ? "Evet" : "Hayır",
            _tempPrediction!.Probability
        );

        SaveText.Children.Add(
            new TextBlock
            {
                Text = "Sonuç veritabanına kaydedildi.",
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

    private void BtnDataset_Click(object sender, RoutedEventArgs e)
    {
        var datasetWindow = new DatasetWindow();
        datasetWindow.ShowDialog(this);
    }

    private void BtnVisualize_Click(object sender, RoutedEventArgs e)
    {
        if (AgeInput.Value == null || MaxHRInput.Value == null)
        {
            TextResult.Text = "Riski görselleştirmek için lütfen Yaş ve Maksimum Kalp Atış Hızını girin.";
            TextResult.Foreground = Brushes.OrangeRed;
            return;
        }

        int genderVal = MaleInput.IsChecked == true ? 1 : FemaleInput.IsChecked == true ? 0 : -1;
        int cpVal = Cp0Input.IsChecked == true ? 0 :
                    Cp1Input.IsChecked == true ? 1 :
                    Cp2Input.IsChecked == true ? 2 :
                    Cp3Input.IsChecked == true ? 3 : -1;

        if (genderVal == -1 || cpVal == -1)
        {
            TextResult.Text = "Lütfen Cinsiyet ve Göğüs Ağrısı Tipini seçin.";
            TextResult.Foreground = Brushes.OrangeRed;
            return;
        }

        var metrics = new UserMetrics
        {
            Age = (double)AgeInput.Value,
            MaxHR = (double)MaxHRInput.Value,
            Sex = genderVal,
            CP = cpVal
        };

        var datasetWindow = new DatasetWindow(metrics);
        datasetWindow.ShowDialog(this);
    }
}
