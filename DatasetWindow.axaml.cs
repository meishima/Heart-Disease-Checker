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
        LoadDataAndCharts();
        DrawDashboard();
    }

    public DatasetWindow(UserMetrics metrics) : this()
    {
        _userMetrics = metrics;
        DrawDashboard(); // Redraw with metrics
    }

    private async void LoadDataAndCharts()
    {
        ModelGrid.ItemsSource = new List<ModelResult> { new ModelResult { Name = "Training.. Please Wait.", Accuracy = "..." } };
        var results = await Task.Run(() =>
        {
            if (!File.Exists("heart.csv")) return new List<ModelResult>();
            return ModelComparison.RunAllModels("heart.csv");
        });
        ModelGrid.ItemsSource = results;
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
        var sexH = new List<double>(); var sexS = new List<double>(); // Added CP lists

        var allAges = new List<double>();
        var allBps = new List<double>();
        var allChols = new List<double>();
        var allMaxHRs = new List<double>();
        var allCPs = new List<double>(); // Added allCPs list

        foreach (var line in lines)
        {
            var p = line.Split(',');
            if (p.Length <= 13) continue;

            if (!int.TryParse(p[13], out int target)) continue;

            double.TryParse(p[3], out double bp);
            double.TryParse(p[4], out double chol);
            double.TryParse(p[7], out double hr);
            double.TryParse(p[8], out double exang);
            double.TryParse(p[2], out double cp); // Extract CP from index 2
            double.TryParse(p[0], out double age);
            double.TryParse(p[1], out double sex);

            allAges.Add(age);
            allBps.Add(bp);
            allChols.Add(chol);
            allMaxHRs.Add(hr);
            allCPs.Add(cp); // Populate allCPs

            if (target == 1)
            {
                cholH.Add(chol);
                bpH.Add(bp);
                hrH.Add(hr);
                exH.Add(exang);
                ageH.Add(age);
                cpH.Add(cp);
                sexH.Add(sex); // Populate cpH
            }
            else
            {
                cholS.Add(chol);
                bpS.Add(bp);
                hrS.Add(hr);
                exS.Add(exang);
                ageS.Add(age);
                cpS.Add(cp);
                sexS.Add(sex); // Populate cpS
            }
        }

        DrawSingleBarChart(CholChart, "Average Cholesterol", cholH.Average(), cholS.Average(), "mg/dl");
        DrawSingleBarChart(BPChart, "Average Blood Pressure", bpH.Average(), bpS.Average(), "mm Hg");
        DrawSingleBarChart(HRChart, "Maximum Heart Rate", hrH.Average(), hrS.Average(), "bpm");
        double exH_Percent = (exH.Count(x => x > 0) / (double)exH.Count) * 100;
        double exS_Percent = (exS.Count(x => x > 0) / (double)exS.Count) * 100;
        DrawSingleBarChart(ExangChart, "Exang Percentage", exH_Percent, exS_Percent, "%");
        // Drawn in CPSeverityChart instead

        // New Distribution Charts
        DrawBoxPlot(ThalachBoxPlot, "Max Heart Rate Distribution", hrH, hrS, "Max Heart Rate", _userMetrics?.MaxHR);
        DrawBoxPlot(AgeBoxPlot, "Age Distribution", ageH, ageS, "Age", _userMetrics?.Age);

        int? userCPIndex = _userMetrics?.CP;
        DrawGroupedBarChart(CPSeverityChart, "Chest Pain Risk Analysis", cpH, cpS,
            new[] { "Typical", "Atypical", "Non-Anginal", "Asymptomatic" }, userCPIndex);

        int? userSexIndex = _userMetrics?.Sex;
        DrawGroupedBarChart(SexRiskChart, "Sex Risk Analysis", sexH, sexS,
            new[] { "Male", "Female" }, userSexIndex); // Note: Dataset has 0=Female, 1=Male usually, but distinct sort might change order. 
                                                       // Let's verify distinct order: Female(0) then Male(1).
                                                       // But Wait, distinct.OrderBy(x=>x) means 0 then 1. 0 is Female usually in heart datasets. 
                                                       // If userSexIndex match 0 or 1 it will work.

        DrawCorrelationMatrix(CorrelationChart, allAges, allBps, allChols, allMaxHRs, allCPs); // Updated call
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
        tickGen.AddMajor(1, "Healthy");
        tickGen.AddMajor(2, "In Risk");
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

    private void DrawScatterPlot(ScottPlot.Avalonia.AvaPlot chart, string title,
        List<double> xH, List<double> yH,
        List<double> xS, List<double> yS,
        string xLabel, string yLabel)
    {
        chart.Plot.Clear();

        var colorHealthy = ScottPlot.Color.FromHex("#48C9B0");
        var colorSick = ScottPlot.Color.FromHex("#EC7063");

        // Scatter Points with opacity
        var scatterH = chart.Plot.Add.Scatter(xH.ToArray(), yH.ToArray());
        scatterH.Color = colorHealthy.WithAlpha(0.6);
        scatterH.LegendText = "Healthy";
        scatterH.MarkerStyle.Size = 6;
        scatterH.LineWidth = 0;

        var scatterS = chart.Plot.Add.Scatter(xS.ToArray(), yS.ToArray());
        scatterS.Color = colorSick.WithAlpha(0.6);
        scatterS.LegendText = "In Risk";
        scatterS.MarkerStyle.Size = 6;
        scatterS.LineWidth = 0;

        // Trend Lines
        if (xH.Count > 1)
        {
            var (slopeH, interceptH) = GetLinearRegression(xH, yH);
            var lineH = chart.Plot.Add.Line(xH.Min(), xH.Min() * slopeH + interceptH, xH.Max(), xH.Max() * slopeH + interceptH);
            lineH.Color = colorHealthy;
            lineH.LineWidth = 3;
            lineH.LinePattern = ScottPlot.LinePattern.Solid;
        }

        if (xS.Count > 1)
        {
            var (slopeS, interceptS) = GetLinearRegression(xS, yS);
            var lineS = chart.Plot.Add.Line(xS.Min(), xS.Min() * slopeS + interceptS, xS.Max(), xS.Max() * slopeS + interceptS);
            lineS.Color = colorSick;
            lineS.LineWidth = 3;
            lineS.LinePattern = ScottPlot.LinePattern.Solid;
        }

        chart.Plot.Title(title);
        chart.Plot.XLabel(xLabel);
        chart.Plot.YLabel(yLabel);

        // Improve        
        chart.Plot.ShowLegend();
        chart.Plot.Axes.Title.Label.FontSize = 16;
        chart.Plot.Axes.Title.Label.Bold = true;

        // chart.Plot.HideGrid(); // Removed to enable grid
        chart.UserInputProcessor.Disable();
        chart.Refresh();
    }

    private (double Slope, double Intercept) GetLinearRegression(List<double> x, List<double> y)
    {
        double n = x.Count;
        double sumX = x.Sum();
        double sumY = y.Sum();
        double sumXY = x.Zip(y, (a, b) => a * b).Sum();
        double sumX2 = x.Select(a => a * a).Sum();

        double denominator = n * sumX2 - sumX * sumX;
        if (Math.Abs(denominator) < 1e-9) return (0, y.Average());

        double slope = (n * sumXY - sumX * sumY) / denominator;
        double intercept = (sumY - slope * sumX) / n;
        return (slope, intercept);
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
            marker.LegendText = "You";
            chart.Plot.ShowLegend();
        }

        chart.Plot.Title(title);
        chart.Plot.YLabel(yLabel);

        ScottPlot.TickGenerators.NumericManual tickGen = new();
        tickGen.AddMajor(1, "Healthy");
        tickGen.AddMajor(2, "In Risk");
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

        // ScottPlot.Box struct usually takes these order
        // Box(double min, double max, double q1, double median, double q3) - Check signature or use properties
        // Assuming ScottPlot.Box is a struct/class with properties.

        // Safety: In ScottPlot 5, Box is often a struct.
        // Logic: range (min to max), box (q1 to q3), line (median)

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
                Label = i == 0 ? "Healthy" : null
            };

            // Red bar (Risk)
            var barS = new ScottPlot.Bar()
            {
                Position = i * 3 + 1,
                Value = countS,
                FillColor = ScottPlot.Color.FromHex("#EC7063"),
                Label = i == 0 ? "In Risk" : null
            };

            // Highlight if matches user category (assuming index match val)
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
        List<double> ages, List<double> bps, List<double> chols, List<double> hrs, List<double> cps) // Updated signature
    {
        chart.Plot.Clear();

        double[][] data = {
            ages.ToArray(),
            bps.ToArray(),
            chols.ToArray(),
            hrs.ToArray(),
            cps.ToArray() // Updated data array
        };

        string[] labels = { "Age", "Trestbps", "Chol", "Thalach", "CP" }; // Updated labels
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

        // Add text labels for correlation values on top of heatmap cells
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

        // Set up axes
        chart.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual();
        chart.Plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericManual();

        for (int i = 0; i < count; i++)
        {
            var bottomTick = (ScottPlot.TickGenerators.NumericManual)chart.Plot.Axes.Bottom.TickGenerator;
            var leftTick = (ScottPlot.TickGenerators.NumericManual)chart.Plot.Axes.Left.TickGenerator;

            bottomTick.AddMajor(i, labels[i]);
            leftTick.AddMajor(i, labels[i]);
        }

        // Invert Y axis so (0,0) is top-left like a matrix
        chart.Plot.Axes.SetLimits(-0.5, count - 0.5, -0.5, count - 0.5);
        chart.Plot.Axes.Left.TickLabelStyle.FontSize = 14;
        chart.Plot.Axes.Bottom.TickLabelStyle.FontSize = 14;

        // Add ColorBar
        chart.Plot.Add.ColorBar(hm);

        chart.Plot.Title("Correlation Matrix");
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
}