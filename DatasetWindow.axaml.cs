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
}