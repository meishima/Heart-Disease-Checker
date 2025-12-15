using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using Microsoft.ML;

namespace HeartDiseaseChecker 
{
    public class ModelResult
    {
        public string Name {get; set;} = "";
        public string Accuracy {get; set;} = "";
    }

    public static class ModelComparison
    {
        public static List<ModelResult> RunAllModels(string dataPath)
        {
            var results = new List<ModelResult>();
            var mlContext = new MLContext(seed: 0);

            if(!File.Exists(dataPath)) return results;

            var dataView = mlContext.Data.LoadFromTextFile<HeartData>(path: dataPath, hasHeader: true, separatorChar: ',');
            var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            var trainSet = split.TrainSet;
            var testSet = split.TestSet;

            var pipeline = mlContext.Transforms.Concatenate("Features", "Age", "Gender", "ChestPainType", "BloodPressure", "Cholesterol", "BloodSugar", "ExerciseInducedAngina");

            var trainers = new Dictionary<string, IEstimator<ITransformer>>();

            trainers.Add("Logistic Regression", 
                mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));

            var forestInfo = mlContext.BinaryClassification.Trainers.FastForest(labelColumnName: "Label", featureColumnName: "Features");
            trainers.Add("Random Forest", 
                forestInfo.Append(mlContext.BinaryClassification.Calibrators.Platt(labelColumnName: "Label")));

            var treeInfo = mlContext.BinaryClassification.Trainers.FastTree(labelColumnName: "Label", featureColumnName: "Features");
            trainers.Add("XGBoost (FastTree)", 
                treeInfo.Append(mlContext.BinaryClassification.Calibrators.Platt(labelColumnName: "Label")));

            var svmInfo = mlContext.BinaryClassification.Trainers.LinearSvm(labelColumnName: "Label", featureColumnName: "Features");
            trainers.Add("SVM (Linear)", 
                svmInfo.Append(mlContext.BinaryClassification.Calibrators.Platt(labelColumnName: "Label")));

            var perceptronInfo = mlContext.BinaryClassification.Trainers.AveragedPerceptron(labelColumnName: "Label", featureColumnName: "Features");
            trainers.Add("Artificial Neural Network", 
                perceptronInfo.Append(mlContext.BinaryClassification.Calibrators.Platt(labelColumnName: "Label")));

            foreach (var t in trainers)
            {
                try
                {
                    var model = pipeline.Append(t.Value).Fit(trainSet);
                    var metrics = mlContext.BinaryClassification.Evaluate(model.Transform(testSet), labelColumnName: "Label");

                    results.Add(new ModelResult
                    {
                        Name = t.Key,
                        Accuracy = $"%{metrics.Accuracy * 100:F2}",
                    });
                }
                catch (Exception ex) { results.Add(new ModelResult { Name = t.Key, Accuracy = $"Error {ex.Message}"}); }
            }
            return results.OrderByDescending(x => x.Accuracy).ToList();
        }
    }
}