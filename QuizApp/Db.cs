using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace QuizApp
{
    // Where the data lives. Local = a file on this PC (works).
    // Online = a server (NOT built yet, only the place where it would go).
    public interface IStorage
    {
        Data Load();
        void Save(Data data);
    }

    public class LocalStorage : IStorage
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "quizdata.json");

        public Data Load()
        {
            if (!File.Exists(path)) return null;
            try
            {
                using (var f = File.OpenRead(path))
                    return (Data)new DataContractJsonSerializer(typeof(Data)).ReadObject(f);
            }
            catch { return null; }   // broken file -> start again with sample data
        }

        public void Save(Data data)
        {
            using (var f = File.Create(path))
                new DataContractJsonSerializer(typeof(Data)).WriteObject(f, data);
        }
    }

    public class OnlineStorage : IStorage
    {
        public Data Load() { throw new NotImplementedException("Online mode is not built yet."); }
        public void Save(Data data) { throw new NotImplementedException("Online mode is not built yet."); }
    }

    // All the shared data and the simple logic (login, register, marking...)
    public static class Db
    {
        public static IStorage Storage = new LocalStorage();
        public static Data All;
        public static User Me;

        public static void Start()
        {
            All = Storage.Load();
            if (All == null)
            {
                All = new Data();
                AddSampleData();
                Save();
            }
        }

        public static void Save() { Storage.Save(All); }
        public static int NewId() { return All.NextId++; }

        // ----- passwords -----
        public static string Hash(string password)
        {
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes("quiz-app:" + password)));
        }

        // ----- checks (return null if OK, otherwise the error text) -----
        public static string CheckUser(string name, string username, string email, string password, bool passwordRequired, User ignore)
        {
            if (name.Trim() == "") return "Please enter the full name.";
            if (!Regex.IsMatch(username, @"^[A-Za-z0-9_]{3,20}$")) return "Username must be 3-20 letters, numbers or _";
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) return "Please enter a valid email.";
            if ((passwordRequired || password != "") && password.Length < 6) return "Password must be at least 6 characters.";
            foreach (User u in All.Users)
            {
                if (u == ignore) continue;
                if (u.Username.ToLower() == username.ToLower()) return "This username is already taken.";
                if (u.Email.ToLower() == email.ToLower()) return "This email is already used.";
            }
            return null;
        }

        public static User Login(string nameOrEmail, string password)
        {
            string key = nameOrEmail.Trim().ToLower();
            return All.Users.FirstOrDefault(u => (u.Username.ToLower() == key || u.Email.ToLower() == key)
                                                 && u.Password == Hash(password));
        }

        // ----- quiz helpers -----
        public static List<Question> QuestionsOf(int quizId) { return All.Questions.Where(q => q.QuizId == quizId).ToList(); }
        public static int MaxScore(int quizId) { return QuestionsOf(quizId).Sum(q => q.Marks); }

        public static void DeleteQuiz(Quiz quiz)
        {
            All.Questions.RemoveAll(q => q.QuizId == quiz.Id);
            All.Quizzes.Remove(quiz);
            Save();
        }

        public static List<Result> MyResults()
        {
            return All.Results.Where(r => Me.Role == "Admin" || r.Username == Me.Username)
                              .OrderByDescending(r => r.Date).ToList();
        }

        // ----- sample data for the first run -----
        static void AddSampleData()
        {
            AddUser("Administrator", "admin", "admin@quiz.com", "Admin@123", "Admin");
            AddUser("Test Student", "student", "student@quiz.com", "Student@123", "Student");

            Quiz csharp = AddQuiz("C# Basics", "Variables, loops and classes", 10, true);
            AddQuestion(csharp, "Which keyword makes a constant in C#?", 0, 2, "const", "static", "final", "fixed");
            AddQuestion(csharp, "Which loop runs at least once?", 2, 2, "for", "while", "do-while", "foreach");
            AddQuestion(csharp, "What is 7 / 2 for two ints?", 1, 1, "3.5", "3", "4", "2");

            Quiz oop = AddQuiz("OOP Concepts", "Encapsulation, inheritance, polymorphism", 8, true);
            AddQuestion(oop, "Hiding data inside a class is called?", 0, 2, "Encapsulation", "Inheritance", "Polymorphism", "Overloading");
            AddQuestion(oop, "A class that gets members from another class uses?", 1, 2, "Interfaces only", "Inheritance", "Static", "Delegates");

            AddQuiz("Databases (draft)", "Not ready yet - hidden from students", 12, false);
        }

        static void AddUser(string name, string username, string email, string password, string role)
        {
            All.Users.Add(new User { Name = name, Username = username, Email = email, Password = Hash(password), Role = role });
        }

        static Quiz AddQuiz(string title, string description, int minutes, bool available)
        {
            var q = new Quiz { Id = NewId(), Title = title, Description = description, Minutes = minutes, Available = available };
            All.Quizzes.Add(q);
            return q;
        }

        static void AddQuestion(Quiz quiz, string text, int correct, int marks, params string[] options)
        {
            All.Questions.Add(new Question { Id = NewId(), QuizId = quiz.Id, Text = text, Correct = correct, Marks = marks, Options = new List<string>(options) });
        }
    }
}
