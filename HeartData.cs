using Microsoft.ML.Data;

public class HeartData
{
    [LoadColumn(0)] public float Age { get; set; }
    [LoadColumn(1)] public float Gender { get; set; }
    [LoadColumn(2)] public float ChestPainType { get; set; }
    [LoadColumn(3)] public float BloodPressure { get; set;}
    [LoadColumn(4)] public float Cholesterol { get; set; }
    [LoadColumn(5)] public float BloodSugar { get; set; }
    [LoadColumn(8)] public float ExerciseInducedAngina { get; set; }
    [LoadColumn(13)] public bool Label { get; set; }
}

public class HeartPrediction
{
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }
    public float Probability { get; set; }
}