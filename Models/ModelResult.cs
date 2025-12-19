using System.Collections.Generic;

namespace HeartDiseaseChecker.Models
{
    public class ModelResult
    {
        public string Name {get; set;} = "";
        public string Accuracy {get; set;} = "";
        public double Precision {get; set;}
        public double Recall {get; set;}
        public double F1Score {get; set;}
        public double AUC {get; set;}
        public double[][] ConfusionMatrix {get; set;} = new double[0][];
        public double[] RocFpr { get; set; } = new double[0];
        public double[] RocTpr { get; set; } = new double[0];
        public Dictionary<string, double> FeatureImportance { get; set; } = new Dictionary<string, double>();
    }
}
