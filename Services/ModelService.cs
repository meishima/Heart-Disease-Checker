using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using HeartDiseaseChecker.Models;

namespace HeartDiseaseChecker.Services
{
    public enum PreprocessingType
    {
        None,
        MinMax,
        ZScore
    }

    [Flags]
    public enum AlgorithmType
    {
        LogisticRegression = 1,
        RandomForest = 2,
        XGBoost = 4,
        SVM = 8,
        NeuralNetwork = 16,
        All = LogisticRegression | RandomForest | XGBoost | SVM | NeuralNetwork
    }

    public static class ModelService
    {
        // Cache to store trained models
        public static Dictionary<string, ITransformer> TrainedModels { get; private set; } = new Dictionary<string, ITransformer>();

        public static List<ModelResult> RunAllModels(string dataPath, PreprocessingType preprocessing = PreprocessingType.None, AlgorithmType selection = AlgorithmType.All)
        {
            TrainedModels.Clear(); // Reset cache
            var results = new List<ModelResult>();
            var mlContext = new MLContext(seed: 0);

            if (!File.Exists(dataPath)) return results;

            var dataView = mlContext.Data.LoadFromTextFile<HeartData>(dataPath, hasHeader: true, separatorChar: ',');

            var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            var trainSet = split.TrainSet;
            var testSet = split.TestSet;

            IEstimator<ITransformer> pipeline = mlContext.Transforms.Concatenate("Features", "Age", "Gender", "ChestPainType", "BloodPressure", "Cholesterol", "BloodSugar", "ExerciseInducedAngina");

            // Append normalization based on selection
            switch (preprocessing)
            {
                case PreprocessingType.MinMax:
                    pipeline = pipeline.Append(mlContext.Transforms.NormalizeMinMax("Features"));
                    break;
                case PreprocessingType.ZScore:
                    pipeline = pipeline.Append(mlContext.Transforms.NormalizeMeanVariance("Features"));
                    break;
            }

            var trainers = new Dictionary<string, IEstimator<ITransformer>>();

            if (selection.HasFlag(AlgorithmType.LogisticRegression))
            {
                trainers.Add("Logistic Regression",
                    mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));
            }

            if (selection.HasFlag(AlgorithmType.RandomForest))
            {
                var forestInfo = mlContext.BinaryClassification.Trainers.FastForest(labelColumnName: "Label", featureColumnName: "Features");
                trainers.Add("Random Forest",
                    forestInfo.Append(mlContext.BinaryClassification.Calibrators.Platt(labelColumnName: "Label")));
            }

            if (selection.HasFlag(AlgorithmType.XGBoost))
            {
                var treeInfo = mlContext.BinaryClassification.Trainers.FastTree(labelColumnName: "Label", featureColumnName: "Features");
                trainers.Add("XGBoost (FastTree)",
                    treeInfo.Append(mlContext.BinaryClassification.Calibrators.Platt(labelColumnName: "Label")));
            }

            if (selection.HasFlag(AlgorithmType.SVM))
            {
                var svmInfo = mlContext.BinaryClassification.Trainers.LinearSvm(labelColumnName: "Label", featureColumnName: "Features");
                trainers.Add("SVM (Linear)",
                    svmInfo.Append(mlContext.BinaryClassification.Calibrators.Platt(labelColumnName: "Label")));
            }

            if (selection.HasFlag(AlgorithmType.NeuralNetwork))
            {
                var perceptronInfo = mlContext.BinaryClassification.Trainers.AveragedPerceptron(labelColumnName: "Label", featureColumnName: "Features");
                trainers.Add("Artificial Neural Network",
                    perceptronInfo.Append(mlContext.BinaryClassification.Calibrators.Platt(labelColumnName: "Label")));
            }

            foreach (var t in trainers)
            {
                try
                {
                    var fullPipeline = pipeline.Append(t.Value);
                    var model = fullPipeline.Fit(trainSet);

                    // Cache the successful model
                    TrainedModels[t.Key] = model;

                    var transformedTest = model.Transform(testSet);
                    var metrics = mlContext.BinaryClassification.Evaluate(transformedTest, labelColumnName: "Label");

                    results.Add(new ModelResult
                    {
                        Name = t.Key,
                        Accuracy = $"%{metrics.Accuracy * 100:F2}",
                        Precision = metrics.PositivePrecision,
                        Recall = metrics.PositiveRecall,
                        F1Score = metrics.F1Score,
                        AUC = metrics.AreaUnderRocCurve,
                        ConfusionMatrix = new double[][]
                        {
                            new double[] { metrics.ConfusionMatrix.Counts[0][0], metrics.ConfusionMatrix.Counts[0][1] },
                            new double[] { metrics.ConfusionMatrix.Counts[1][0], metrics.ConfusionMatrix.Counts[1][1] }
                        },
                        RocFpr = CalculateROC(mlContext, transformedTest).FPR,
                        RocTpr = CalculateROC(mlContext, transformedTest).TPR,
                        FeatureImportance = CalculateWeights(model)
                    });
                }
                catch (Exception ex) { results.Add(new ModelResult { Name = t.Key, Accuracy = $"Error {ex.Message}" }); }
            }
            return results.OrderByDescending(x =>
            {
                if (double.TryParse(x.Accuracy.TrimStart('%'), out double val)) return val;
                return -1.0; // Errors go to bottom
            }).ToList();
        }

        private static (double[] FPR, double[] TPR) CalculateROC(MLContext mlContext, IDataView data)
        {
            var predictions = mlContext.Data.CreateEnumerable<Prediction>(data, reuseRowObject: false).ToList();
            var distinctScores = predictions.Select(p => p.Probability).Distinct().OrderByDescending(s => s).ToList();

            if (distinctScores.Count > 100)
            {
                var step = distinctScores.Count / 100;
                distinctScores = distinctScores.Where((x, i) => i % step == 0).ToList();
            }

            var fprs = new List<double>();
            var tprs = new List<double>();

            foreach (var threshold in distinctScores)
            {
                int tp = 0; int fp = 0; int tn = 0; int fn = 0;
                foreach (var p in predictions)
                {
                    bool actual = p.Label;
                    bool predicted = p.Probability >= threshold;

                    if (actual && predicted) tp++;
                    else if (!actual && predicted) fp++;
                    else if (actual && !predicted) fn++;
                    else tn++;
                }

                double tpr = (tp + fn) > 0 ? (double)tp / (tp + fn) : 0;
                double fpr = (fp + tn) > 0 ? (double)fp / (fp + tn) : 0;

                tprs.Add(tpr);
                fprs.Add(fpr);
            }

            tprs.Add(0); fprs.Add(0);
            tprs.Insert(0, 1); fprs.Insert(0, 1);

            return (fprs.ToArray(), tprs.ToArray());
        }

        private static Dictionary<string, double> CalculateWeights(ITransformer model)
        {
            var importance = new Dictionary<string, double>();
            var names = new[] { "Age", "Gender", "ChestPainType", "BloodPressure", "Cholesterol", "BloodSugar", "ExerciseInducedAngina" };

            if (model is TransformerChain<ITransformer> chain)
            {
                foreach (var transformer in chain)
                {
                    // 1. Get the "Model" from the transformer (e.g. BinaryPredictionTransformer)
                    var type = transformer.GetType();
                    var modelProp = type.GetProperty("Model");

                    if (modelProp != null)
                    {
                        var modelParams = modelProp.GetValue(transformer);
                        if (modelParams == null) continue;

                        // Function to check a potential object for weights
                        Dictionary<string, double>? TryGetWeights(object obj)
                        {
                            if (obj is LinearBinaryModelParameters linear)
                            {
                                var weights = linear.Weights;
                                if (weights.Count == names.Length)
                                {
                                    var imp = new Dictionary<string, double>();
                                    for (int i = 0; i < names.Length; i++) imp[names[i]] = weights[i];
                                    return imp;
                                }
                            }
                            return null;
                        }

                        // Check the modelParams directly
                        var directResult = TryGetWeights(modelParams);
                        if (directResult != null) return directResult;

                        // Check properties of modelParams (e.g. SubModel)
                        foreach (var prop in modelParams.GetType().GetProperties())
                        {
                            if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                            {
                                try
                                {
                                    var val = prop.GetValue(modelParams);
                                    if (val != null)
                                    {
                                        var subResult = TryGetWeights(val);
                                        if (subResult != null) return subResult;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            return importance;
        }
        public static List<(string ModelName, bool Prediction, float Probability)> Predict(HeartData data)
        {
            var results = new List<(string, bool, float)>();
            var mlContext = new MLContext(seed: 0);

            foreach (var kvp in TrainedModels)
            {
                try
                {
                    var engine = mlContext.Model.CreatePredictionEngine<HeartData, Prediction>(kvp.Value);
                    var prediction = engine.Predict(data);
                    results.Add((kvp.Key, prediction.Label, prediction.Probability));
                }
                catch (Exception)
                {
                    // Ignore prediction errors for individual models
                }
            }
            return results;
        }
    }
}