using Microsoft.Data.Sqlite;
using System.IO;
using System;

namespace HeartDiseaseChecker
{
    public class DatabaseManager
    {
        private const string DbName = "HeartDiseaseChecker.db";

        public static void InitializeDatabase()
        {
            if (!File.Exists(DbName))
            {
                using (var connection = new SqliteConnection($"Data Source={DbName}"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText =
                    @"
                    CREATE TABLE IF NOT EXISTS PatientRecords (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Date TEXT NOT NULL,
                        Age REAL NOT NULL,
                        Gender TEXT NOT NULL,
                        BloodPressure REAL NOT NULL,
                        Cholesterol REAL NOT NULL,
                        BloodSugar TEXT NOT NULL,
                        ChestPainType TEXT NOT NULL,
                        ExerciseInducedAngina TEXT NOT NULL,
                        Probability REAL NOT NULL
                    );
                    ";
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void InsertRecord(DateTime date, float age, string gender, float bloodPressure, float cholesterol,
                                        string bloodSugar, string chestPainType,
                                        string exerciseInducedAngina, float probability)
        {
            using (var connection = new SqliteConnection($"Data Source={DbName}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                INSERT INTO PatientRecords (Date, Age, Gender, BloodPressure, Cholesterol, BloodSugar, ChestPainType, ExerciseInducedAngina, Probability)
                VALUES ($date, $age, $gender, $bloodPressure, $cholesterol, $bloodSugar, $chestPainType, $exerciseInducedAngina, $probability);
                ";
                command.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("$age", age);
                command.Parameters.AddWithValue("$gender", gender);
                command.Parameters.AddWithValue("$bloodPressure", bloodPressure);
                command.Parameters.AddWithValue("$cholesterol", cholesterol);
                command.Parameters.AddWithValue("$bloodSugar", bloodSugar);
                command.Parameters.AddWithValue("$chestPainType", chestPainType);
                command.Parameters.AddWithValue("$exerciseInducedAngina", exerciseInducedAngina);
                command.Parameters.AddWithValue("$probability", probability);
                command.ExecuteNonQuery();
            }
        }
    }
}