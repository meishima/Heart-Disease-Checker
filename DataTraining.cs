using Microsoft.ML;

public class DataTraining
{
    private static string dataPath = "heart.csv";
    private static string modelPath = "heartmodel.zip";
    private static MLContext mlContext = new MLContext();

    public static void Train()
    {
        IDataView traningData = mlContext.Data.LoadFromTextFile<HeartData>(path: dataPath, hasHeader:true, separatorChar:',');
        var pipeline = mlContext.Transforms.Concatenate("Features", "Age", "Gender", "ChestPainType", "BloodPressure", "Cholesterol", "BloodSugar", "ExerciseInducedAngina")
            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));

        var model = pipeline.Fit(traningData);
        mlContext.Model.Save(model, traningData.Schema, modelPath);
    }
}