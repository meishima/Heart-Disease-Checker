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

public partial class DatasetWindow : Window
{
    public DatasetWindow()
    {
        InitializeComponent();
        LoadDataAndCharts();

        DrawDashboard();
    }

    private async void LoadDataAndCharts()
    {
        ModelGrid.ItemsSource = new List<ModelResult> { new ModelResult { Name = "Training.. Please Wait.", Accuracy = "..."} };
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
        var bpH = new List<double>();   var bpS = new List<double>();
        var hrH = new List<double>();   var hrS = new List<double>();
        var exH = new List<double>();   var exS = new List<double>();

        foreach (var line in lines)
        {
            var p = line.Split(',');
            if (p.Length <= 13) continue;
            
            if (!int.TryParse(p[13], out int target)) continue;

            double.TryParse(p[3], out double bp);
            double.TryParse(p[4], out double chol);
            double.TryParse(p[7], out double hr);
            double.TryParse(p[8], out double exang);

            if (target == 1) 
            {
                cholH.Add(chol); 
                bpH.Add(bp); 
                hrH.Add(hr); 
                exH.Add(exang);
            }
            else 
            {
                cholS.Add(chol); 
                bpS.Add(bp); 
                hrS.Add(hr); 
                exS.Add(exang);
            }
        }

        DrawSingleBarChart(CholChart, "Average Cholesterol", cholH.Average(), cholS.Average(), "mg/dl");
        DrawSingleBarChart(BPChart, "Average Blood Pressure", bpH.Average(), bpS.Average(), "mm Hg");
        DrawSingleBarChart(HRChart, "Maximum Heart Rate", hrH.Average(), hrS.Average(), "bpm");
        double exH_Percent = (exH.Count(x => x > 0) / (double)exH.Count) * 100;
        double exS_Percent = (exS.Count(x => x > 0) / (double)exS.Count) * 100;
        DrawSingleBarChart(ExangChart, "Exang Percentage", exH_Percent, exS_Percent, "%");
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
}