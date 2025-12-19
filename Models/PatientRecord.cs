using System;

namespace HeartDiseaseChecker.Models
{
    public class PatientRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public float Age { get; set; }
        public string? Gender { get; set; }
        public float BloodPressure { get; set; }
        public float Cholesterol { get; set; }
        public string? BloodSugar { get; set; }
        public string? ChestPainType { get; set; }
        public string? ExerciseInducedAngina { get; set; }
        public float Probability { get; set; }
    }
}
