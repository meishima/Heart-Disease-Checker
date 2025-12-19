using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Layout;
using Avalonia.Media;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using HeartDiseaseChecker.Models;
using HeartDiseaseChecker.Services;

namespace HeartDiseaseChecker;

public class UserMetrics
{
    public double? Age { get; set; }
    public double? MaxHR { get; set; }
    public int? Sex { get; set; }
    public int? CP { get; set; }
}

public partial class DatasetWindow : Window
{
    private UserMetrics? _userMetrics;

    public DatasetWindow()
    {
        InitializeComponent();
        // Load data structure but don't train any models yet (faster startup)
        LoadDataAndCharts(selection: (AlgorithmType)0);
        DrawDashboard();
    }

    public DatasetWindow(UserMetrics metrics) : this()
    {
        _userMetrics = metrics;
        DrawDashboard(); // Redraw with metrics
    }

    private async void LoadDataAndCharts(PreprocessingType preprocessing = PreprocessingType.None, AlgorithmType selection = AlgorithmType.All)
    {
        if (ModelGrid == null) return;

        ModelGrid.ItemsSource = new List<ModelResult> { new ModelResult { Name = "Eğitiliyor.. Lütfen Bekleyin.", Accuracy = "..." } };

        var results = await Task.Run(() =>
        {
            if (!File.Exists("heart.csv")) return new List<ModelResult>();
            return ModelService.RunAllModels("heart.csv", preprocessing, selection);
        });

        ModelGrid.ItemsSource = results;
        if (results.Any())
        {
            var bestModel = results.First();
            DrawConfusionMatrix(ConfusionMatrixChart, bestModel.ConfusionMatrix, bestModel.Name);
            DrawROC(ROCChart, results);
        }
        else
        {
            // Clear charts if no results
            ConfusionMatrixChart.Plot.Clear(); ConfusionMatrixChart.Refresh();
            ROCChart.Plot.Clear(); ROCChart.Refresh();
        }
    }

    private AlgorithmType GetSelectedAlgorithms()
    {
        AlgorithmType selection = 0;
        if (chkLR?.IsChecked == true) selection |= AlgorithmType.LogisticRegression;
        if (chkRF?.IsChecked == true) selection |= AlgorithmType.RandomForest;
        if (chkXGB?.IsChecked == true) selection |= AlgorithmType.XGBoost;
        if (chkSVM?.IsChecked == true) selection |= AlgorithmType.SVM;
        if (chkNN?.IsChecked == true) selection |= AlgorithmType.NeuralNetwork;
        return selection;
    }

    private void TrainButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var algs = GetSelectedAlgorithms();
        if (algs == 0)
        {
            // Show message maybe? For now just return.
            return;
        }

        if (ScalingCombo.SelectedIndex >= 0)
        {
            var type = (PreprocessingType)ScalingCombo.SelectedIndex;
            LoadDataAndCharts(type, algs);
        }
    }

    private void ScalingCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Avoid running before UI is fully initialized
        if (ModelGrid == null) return;

        if (sender is ComboBox combo && combo.SelectedIndex >= 0)
        {
            var type = (PreprocessingType)combo.SelectedIndex;
            // Use currently selected algorithms
            var algs = GetSelectedAlgorithms();
            if (algs != 0) LoadDataAndCharts(type, algs);
        }
    }

    private void DrawDashboard()
    {
        if (!File.Exists("heart.csv")) return;
        var lines = File.ReadAllLines("heart.csv").Skip(1).ToList();

        var cholH = new List<double>(); var cholS = new List<double>();
        var bpH = new List<double>(); var bpS = new List<double>();
        var hrH = new List<double>(); var hrS = new List<double>();
        var exH = new List<double>(); var exS = new List<double>();
        var ageH = new List<double>(); var ageS = new List<double>();
        var cpH = new List<double>(); var cpS = new List<double>();
        var sexH = new List<double>(); var sexS = new List<double>();

        var allAges = new List<double>();
        var allBps = new List<double>();
        var allChols = new List<double>();
        var allMaxHRs = new List<double>();
        var allCPs = new List<double>();

        foreach (var line in lines)
        {
            var p = line.Split(',');
            if (p.Length <= 13) continue;

            if (!int.TryParse(p[13], out int target)) continue;

            double.TryParse(p[3], out double bp);
            double.TryParse(p[4], out double chol);
            double.TryParse(p[7], out double hr);
            double.TryParse(p[8], out double exang);
            double.TryParse(p[2], out double cp);
            double.TryParse(p[0], out double age);
            double.TryParse(p[1], out double sex);

            allAges.Add(age);
            allBps.Add(bp);
            allChols.Add(chol);
            allMaxHRs.Add(hr);
            allCPs.Add(cp);

            if (target == 1)
            {
                cholH.Add(chol);
                bpH.Add(bp);
                hrH.Add(hr);
                exH.Add(exang);
                ageH.Add(age);
                cpH.Add(cp);
                sexH.Add(sex);
            }
            else
            {
                cholS.Add(chol);
                bpS.Add(bp);
                hrS.Add(hr);
                exS.Add(exang);
                ageS.Add(age);
                cpS.Add(cp);
                sexS.Add(sex);
            }
        }

        DrawSingleBarChart(CholChart, "Ortalama Kolesterol", cholH.Average(), cholS.Average(), "mg/dl");
        DrawSingleBarChart(BPChart, "Ortalama Kan Basıncı", bpH.Average(), bpS.Average(), "mm Hg");
        DrawSingleBarChart(HRChart, "Maksimum Kalp Atış Hızı", hrH.Average(), hrS.Average(), "bpm");
        double exH_Percent = (exH.Count(x => x > 0) / (double)exH.Count) * 100;
        double exS_Percent = (exS.Count(x => x > 0) / (double)exS.Count) * 100;
        DrawSingleBarChart(ExangChart, "Egzersiz Anjinası Yüzdesi", exH_Percent, exS_Percent, "%");

        DrawBoxPlot(ThalachBoxPlot, "Maksimum Kalp Atış Hızı Dağılımı", hrH, hrS, "Maksimum Kalp Atış Hızı", _userMetrics?.MaxHR);
        DrawBoxPlot(AgeBoxPlot, "Yaş Dağılımı", ageH, ageS, "Yaş", _userMetrics?.Age);

        int? userCPIndex = _userMetrics?.CP;
        DrawGroupedBarChart(CPSeverityChart, "Göğüs Ağrısı Risk Analizi", cpH, cpS,
            new[] { "Tipik", "Atipik", "Anjinal Olmayan", "Asemptomatik" }, userCPIndex);

        int? userSexIndex = _userMetrics?.Sex;
        DrawGroupedBarChart(SexRiskChart, "Cinsiyet Risk Analizi", sexH, sexS,
            new[] { "Erkek", "Kadın" }, userSexIndex);

        DrawCorrelationMatrix(CorrelationChart, allAges, allBps, allChols, allMaxHRs, allCPs);
    }

    private void DrawSingleBarChart(ScottPlot.Avalonia.AvaPlot chart, string title, double valHealthy, double valSick, string yLabel)
    {
        chart.Plot.Clear();

        var colorHealthy = ScottPlot.Color.FromHex("#48C9B0");
        var colorSick = ScottPlot.Color.FromHex("#EC7063");

        var bar1 = chart.Plot.Add.Bar(position: 1, value: valHealthy);
        bar1.Color = colorHealthy;

        var bar2 = chart.Plot.Add.Bar(position: 2, value: valSick);
        bar2.Color = colorSick;

        chart.Plot.Axes.SetLimitsY(0, Math.Max(valHealthy, valSick) * 1.2);

        var txt1 = chart.Plot.Add.Text(valHealthy.ToString("F1"), 1, valHealthy);
        txt1.LabelAlignment = ScottPlot.Alignment.LowerCenter;
        txt1.LabelFontColor = ScottPlot.Colors.Black;
        txt1.LabelFontSize = 14;
        txt1.LabelBold = true;

        var txt2 = chart.Plot.Add.Text(valSick.ToString("F1"), 2, valSick);
        txt2.LabelAlignment = ScottPlot.Alignment.LowerCenter;
        txt2.LabelFontColor = ScottPlot.Colors.Black;
        txt2.LabelFontSize = 14;
        txt2.LabelBold = true;

        chart.Plot.Title(title);
        chart.Plot.Axes.Title.Label.FontSize = 16;
        chart.Plot.Axes.Title.Label.Bold = true;

        chart.Plot.YLabel(yLabel);

        ScottPlot.TickGenerators.NumericManual tickGen = new();
        tickGen.AddMajor(1, "Sağlıklı");
        tickGen.AddMajor(2, "Riskli");
        chart.Plot.Axes.Bottom.TickGenerator = tickGen;
        chart.Plot.Axes.Bottom.TickLabelStyle.FontSize = 12;
        chart.Plot.Axes.Bottom.TickLabelStyle.Bold = true;

        chart.Plot.HideGrid();
        chart.Plot.Axes.Left.FrameLineStyle.Width = 0;
        chart.Plot.Axes.Right.FrameLineStyle.Width = 0;
        chart.Plot.Axes.Top.FrameLineStyle.Width = 0;

        chart.Plot.Axes.Bottom.FrameLineStyle.Color = ScottPlot.Colors.Gray;

        chart.UserInputProcessor.Disable();
        chart.Refresh();
    }

    private void DrawBoxPlot(ScottPlot.Avalonia.AvaPlot chart, string title,
        List<double> healthy, List<double> risk, string yLabel, double? userValue = null)
    {
        chart.Plot.Clear();

        var boxH = GetBoxStats(healthy);
        boxH.Position = 1;
        var bp1 = chart.Plot.Add.Box(boxH);
        bp1.FillColor = ScottPlot.Color.FromHex("#48C9B0"); // Green

        var boxS = GetBoxStats(risk);
        boxS.Position = 2;
        var bp2 = chart.Plot.Add.Box(boxS);
        bp2.FillColor = ScottPlot.Color.FromHex("#EC7063"); // Red

        if (userValue.HasValue)
        {
            var line = chart.Plot.Add.HorizontalLine(userValue.Value);
            line.Color = ScottPlot.Colors.Gold;
            line.LineWidth = 3;
            line.LinePattern = ScottPlot.LinePattern.Dashed;

            var marker = chart.Plot.Add.Marker(1.5, userValue.Value);
            marker.Color = ScottPlot.Colors.Gold;
            marker.Size = 15;
            marker.Shape = ScottPlot.MarkerShape.FilledDiamond;
            marker.LegendText = "Siz";
            chart.Plot.ShowLegend();
        }

        chart.Plot.Title(title);
        chart.Plot.YLabel(yLabel);

        ScottPlot.TickGenerators.NumericManual tickGen = new();
        tickGen.AddMajor(1, "Sağlıklı");
        tickGen.AddMajor(2, "Riskli");
        chart.Plot.Axes.Bottom.TickGenerator = tickGen;

        chart.UserInputProcessor.Disable();
        chart.Refresh();
    }

    private ScottPlot.Box GetBoxStats(List<double> values)
    {
        if (values.Count == 0) return new ScottPlot.Box();

        values.Sort();
        double min = values.First();
        double max = values.Last();
        double median = values[values.Count / 2];
        double q1 = values[values.Count / 4];
        double q3 = values[values.Count * 3 / 4];

        return new ScottPlot.Box
        {
            WhiskerMin = min,
            WhiskerMax = max,
            BoxMin = q1,
            BoxMax = q3,
            BoxMiddle = median,
        };
    }

    private void DrawGroupedBarChart(ScottPlot.Avalonia.AvaPlot chart, string title,
        List<double> valH, List<double> valS, string[] labels, int? userCategory = null)
    {
        chart.Plot.Clear();

        var distinct = valH.Concat(valS).Distinct().OrderBy(x => x).ToList();

        List<ScottPlot.Bar> bars = new();

        for (int i = 0; i < distinct.Count; i++)
        {
            double val = distinct[i];
            double countH = valH.Count(v => v == val);
            double countS = valS.Count(v => v == val);

            // Green bar (Healthy)
            var barH = new ScottPlot.Bar()
            {
                Position = i * 3,
                Value = countH,
                FillColor = ScottPlot.Color.FromHex("#48C9B0"),
                Label = i == 0 ? "Sağlıklı" : null
            };

            // Red bar (Risk)
            var barS = new ScottPlot.Bar()
            {
                Position = i * 3 + 1,
                Value = countS,
                FillColor = ScottPlot.Color.FromHex("#EC7063"),
                Label = i == 0 ? "Riskli" : null
            };

            if (userCategory.HasValue && (int)val == userCategory.Value)
            {
                barH.LineWidth = 3;
                barH.LineColor = ScottPlot.Colors.Gold;
                barS.LineWidth = 3;
                barS.LineColor = ScottPlot.Colors.Gold;
            }

            bars.Add(barH);
            bars.Add(barS);
        }

        chart.Plot.Add.Bars(bars);

        ScottPlot.TickGenerators.NumericManual tickGen = new();
        for (int i = 0; i < distinct.Count && i < labels.Length; i++)
        {
            tickGen.AddMajor(i * 3 + 0.5, labels[i]);
        }
        chart.Plot.Axes.Bottom.TickGenerator = tickGen;

        chart.Plot.ShowLegend();
        chart.Plot.Title(title);
        chart.Plot.YLabel("Count");

        chart.UserInputProcessor.Disable();
        chart.Refresh();
    }

    private void DrawCorrelationMatrix(ScottPlot.Avalonia.AvaPlot chart,
        List<double> ages, List<double> bps, List<double> chols, List<double> hrs, List<double> cps)
    {
        chart.Plot.Clear();

        double[][] data = {
            ages.ToArray(),
            bps.ToArray(),
            chols.ToArray(),
            hrs.ToArray(),
            cps.ToArray()
        };

        string[] labels = { "Yaş", "Tansiyon", "Kol", "Nabız", "GA" };
        int count = labels.Length;
        double[,] matrix = new double[count, count];

        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                matrix[i, j] = CalculatePearsonCorrelation(data[i], data[j]);
            }
        }

        var hm = chart.Plot.Add.Heatmap(matrix);

        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                var txt = chart.Plot.Add.Text(matrix[i, j].ToString("F2"), j, i);
                txt.LabelFontColor = Math.Abs(matrix[i, j]) > 0.5 ? ScottPlot.Colors.White : ScottPlot.Colors.Black;
                txt.LabelFontSize = 14;
                txt.LabelBold = true;
                txt.LabelAlignment = ScottPlot.Alignment.MiddleCenter;
            }
        }

        chart.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual();
        chart.Plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericManual();

        for (int i = 0; i < count; i++)
        {
            var bottomTick = (ScottPlot.TickGenerators.NumericManual)chart.Plot.Axes.Bottom.TickGenerator;
            var leftTick = (ScottPlot.TickGenerators.NumericManual)chart.Plot.Axes.Left.TickGenerator;

            bottomTick.AddMajor(i, labels[i]);
            leftTick.AddMajor(i, labels[i]);
        }

        chart.Plot.Axes.SetLimits(-0.5, count - 0.5, -0.5, count - 0.5);
        chart.Plot.Axes.Left.TickLabelStyle.FontSize = 14;
        chart.Plot.Axes.Bottom.TickLabelStyle.FontSize = 14;

        chart.Plot.Add.ColorBar(hm);

        chart.Plot.Title("Korelasyon Matrisi");
        chart.Plot.Axes.Title.Label.FontSize = 18;

        chart.UserInputProcessor.Disable();
        chart.Refresh();
    }

    private double CalculatePearsonCorrelation(double[] x, double[] y)
    {
        if (x.Length != y.Length) return 0;
        int n = x.Length;

        double sumX = x.Sum();
        double sumY = y.Sum();
        double sumXY = x.Zip(y, (a, b) => a * b).Sum();
        double sumX2 = x.Select(a => a * a).Sum();
        double sumY2 = y.Select(a => a * a).Sum();

        double numerator = n * sumXY - sumX * sumY;
        double denominator = Math.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));

        if (denominator == 0) return 0;
        return numerator / denominator;
    }

    private void DrawConfusionMatrix(ScottPlot.Avalonia.AvaPlot chart, double[][] matrixData, string modelName)
    {
        chart.Plot.Clear();

        double[,] matrix = new double[2, 2];

        // Safeguard against empty or invalid matrix (e.g. from error results)
        if (matrixData == null || matrixData.Length < 2 || matrixData[0].Length < 2)
        {
            chart.Plot.Clear();
            chart.Plot.Add.Text("Hata Matrisi\nBulunamadı", 0.5, 0.5);
            chart.Refresh();
            return;
        }

        // matrixData: Row=Actual, Col=Predicted
        // [0][0] = TN, [0][1] = FP
        // [1][0] = FN, [1][1] = TP

        double[,] visualMatrix = new double[2, 2];
        visualMatrix[0, 0] = matrixData[1][0]; // y=0 (Sick), x=0 (Healthy) -> FN
        visualMatrix[0, 1] = matrixData[1][1]; // y=0 (Sick), x=1 (Sick)    -> TP
        visualMatrix[1, 0] = matrixData[0][0]; // y=1 (Healthy), x=0 (Healthy) -> TN
        visualMatrix[1, 1] = matrixData[0][1]; // y=1 (Healthy), x=1 (Sick) -> FP

        var hm = chart.Plot.Add.Heatmap(visualMatrix);
        hm.Colormap = new ScottPlot.Colormaps.Blues();

        var t1 = chart.Plot.Add.Text(visualMatrix[0, 0].ToString(), 0, 0); t1.LabelAlignment = ScottPlot.Alignment.MiddleCenter; t1.LabelFontSize = 18; t1.LabelBold = true;
        var t2 = chart.Plot.Add.Text(visualMatrix[0, 1].ToString(), 1, 0); t2.LabelAlignment = ScottPlot.Alignment.MiddleCenter; t2.LabelFontSize = 18; t2.LabelBold = true;
        var t3 = chart.Plot.Add.Text(visualMatrix[1, 0].ToString(), 0, 1); t3.LabelAlignment = ScottPlot.Alignment.MiddleCenter; t3.LabelFontSize = 18; t3.LabelBold = true;
        var t4 = chart.Plot.Add.Text(visualMatrix[1, 1].ToString(), 1, 1); t4.LabelAlignment = ScottPlot.Alignment.MiddleCenter; t4.LabelFontSize = 18; t4.LabelBold = true;

        ScottPlot.TickGenerators.NumericManual tickGenX = new();
        tickGenX.AddMajor(0, "Tahmin: Sağlıklı");
        tickGenX.AddMajor(1, "Tahmin: Riskli");
        chart.Plot.Axes.Bottom.TickGenerator = tickGenX;

        ScottPlot.TickGenerators.NumericManual tickGenY = new();
        tickGenY.AddMajor(0, "Gerçek: Riskli");
        tickGenY.AddMajor(1, "Gerçek: Sağlıklı");
        chart.Plot.Axes.Left.TickGenerator = tickGenY;

        chart.Plot.Title($"Hata Matrisi ({modelName})");

        chart.Plot.Axes.Bottom.FrameLineStyle.Width = 0;
        chart.Plot.Axes.Left.FrameLineStyle.Width = 0;
        chart.Plot.Axes.Right.FrameLineStyle.Width = 0;
        chart.Plot.Axes.Top.FrameLineStyle.Width = 0;

        chart.Plot.Axes.SetLimits(-0.5, 1.5, -0.5, 1.5);

        chart.UserInputProcessor.Disable();
        chart.Refresh();
    }

    private void DrawROC(ScottPlot.Avalonia.AvaPlot chart, List<ModelResult> results)
    {
        chart.Plot.Clear();

        var diag = chart.Plot.Add.Line(0, 0, 1, 1);
        diag.LinePattern = ScottPlot.LinePattern.Dashed;
        diag.Color = ScottPlot.Colors.Gray;
        diag.LineWidth = 2;
        diag.LegendText = "Rastgele";

        var palette = new ScottPlot.Palettes.Category10();
        int i = 0;

        foreach (var model in results)
        {
            if (model.RocFpr.Length > 0)
            {
                var scatter = chart.Plot.Add.Scatter(model.RocFpr, model.RocTpr);
                scatter.LineWidth = 2;
                scatter.MarkerSize = 0; // Line only
                scatter.Color = palette.GetColor(i++);
                scatter.LegendText = $"{model.Name} (AUC: {model.AUC:F2})";
            }
        }

        chart.Plot.Title("ROC Eğrisi - Model Karşılaştırması");
        chart.Plot.XLabel("Yanlış Pozitif Oranı (FPR)");
        chart.Plot.YLabel("Doğru Pozitif Oranı (TPR)");

        chart.Plot.ShowLegend();
        chart.Plot.Axes.SetLimits(0, 1, 0, 1);

        chart.UserInputProcessor.Disable();
        chart.Refresh();
    }


}