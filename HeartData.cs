using Microsoft.ML.Data;

public class HeartData
{
    [LoadColumn(0)] public float Yas { get; set; }
    [LoadColumn(1)] public float Cinsiyet { get; set; }
    [LoadColumn(3)] public float Tansiyon { get; set;}
    [LoadColumn(4)] public float Kolestrol { get; set; }
    [LoadColumn(13)] public bool Label { get; set; }
}

public class HeartPrediction
{
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }
    public float Probability { get; set; }
}