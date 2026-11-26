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
            catch { return null; }
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
            else
            {
                foreach (User user in All.Users)
                {
                    if (user.Role == "Admin") user.Role = "Teacher";
                    if (user.Username == "admin" && user.Email == "admin@quiz.com")
                    {
                        user.Name = "Teacher";
                        user.Username = "teacher";
                        user.Email = "teacher@quiz.com";
                        user.Password = Hash("Teacher@123");
                        user.Role = "Teacher";
                    }
                }
                EnsureExpandedSampleData();
            }

            foreach (User user in All.Users)
                if (user.Subjects == null) user.Subjects = new List<string>();
            foreach (Quiz quiz in All.Quizzes)
                if (string.IsNullOrWhiteSpace(quiz.Subject)) quiz.Subject = SubjectForTitle(quiz.Title);
            Save();
        }

        public static List<string> AvailableSubjects()
        {
            return All.Quizzes.Where(q => q.Available && !string.IsNullOrWhiteSpace(q.Subject))
                              .Select(q => q.Subject.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)
                              .OrderBy(subject => subject).ToList();
        }

        static string SubjectForTitle(string title)
        {
            if (title == "C# Basics" || title == "OOP Concepts") return "C# Programming";
            if (title == "Database Fundamentals") return "Databases";
            if (title == "Web Development") return "Web Development";
            if (title == "Programming Fundamentals") return "Programming Fundamentals";
            if (title == "Windows Forms Controls & Event Handling") return "Windows Forms";
            if (title == "GDI+ / Graphics / Visualization") return "Computer Graphics";
            return title;
        }

        public static void Save() { Storage.Save(All); }
        public static int NewId() { return All.NextId++; }

        public static string Hash(string password)
        {
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes("quiz-app:" + password)));
        }

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
            return All.Results.Where(r => Me.Role == "Teacher" || r.Username == Me.Username)
                              .OrderByDescending(r => r.Date).ToList();
        }

        static void AddSampleData()
        {
            AddUser("Teacher", "teacher", "teacher@quiz.com", "Teacher@123", "Teacher");
            AddUser("Test Student", "student", "student@quiz.com", "Student@123", "Student");

            AddExam("C# Basics", "Variables, loops and classes", 10,
                "Which keyword makes a constant in C#?", "const", "Which keyword creates a new object?", "new");
            AddExam("OOP Concepts", "Encapsulation, inheritance and polymorphism", 10,
                "Hiding data inside a class is called?", "Encapsulation", "What is the word for many forms in OOP?", "polymorphism");
            AddExam("Database Fundamentals", "Tables, keys and SQL queries", 10,
                "Which SQL command reads data from a table?", "SELECT", "Which database object stores rows?", "table");
            AddExam("Web Development", "HTML, CSS and HTTP basics", 10,
                "Which language gives a web page its structure?", "HTML", "Which protocol is used to transfer web pages?", "HTTP");
            AddExam("Programming Fundamentals", "Algorithms, variables and control flow", 10,
                "Which structure repeats a block of code?", "loop", "What does an algorithm describe?", "steps");
            AddWindowsFormsExam();
            AddGraphicsExam();
        }

        static void EnsureExpandedSampleData()
        {
            string[] titles = { "C# Basics", "OOP Concepts", "Database Fundamentals", "Web Development", "Programming Fundamentals" };
            string[] descriptions = { "Variables, loops and classes", "Encapsulation, inheritance and polymorphism", "Tables, keys and SQL queries", "HTML, CSS and HTTP basics", "Algorithms, variables and control flow" };
            string[] choiceQuestions = { "Which keyword makes a constant in C#?", "Hiding data inside a class is called?", "Which SQL command reads data from a table?", "Which language gives a web page its structure?", "Which structure repeats a block of code?" };
            string[] choiceAnswers = { "const", "Encapsulation", "SELECT", "HTML", "loop" };
            string[] shortQuestions = { "Which keyword creates a new object?", "What is the word for many forms in OOP?", "Which database object stores rows?", "Which protocol is used to transfer web pages?", "What does an algorithm describe?" };
            string[] shortAnswers = { "new", "polymorphism", "table", "HTTP", "steps" };

            for (int i = 0; i < titles.Length; i++)
            {
                Quiz quiz = All.Quizzes.FirstOrDefault(q => q.Title == titles[i]);
                if (quiz == null && titles[i] == "Database Fundamentals")
                {
                    quiz = All.Quizzes.FirstOrDefault(q => q.Title == "Databases (draft)");
                    if (quiz != null)
                    {
                        quiz.Title = titles[i];
                        quiz.Description = descriptions[i];
                    }
                }
                if (quiz == null)
                {
                    AddExam(titles[i], descriptions[i], 10, choiceQuestions[i], choiceAnswers[i], shortQuestions[i], shortAnswers[i]);
                    continue;
                }

                AddQuestionsUntilTen(quiz, choiceQuestions[i], choiceAnswers[i], shortQuestions[i], shortAnswers[i]);
                quiz.Available = true;
            }

            EnsureSpecializedExam("Windows Forms Controls & Event Handling", AddWindowsFormsExam);
            EnsureSpecializedExam("GDI+ / Graphics / Visualization", AddGraphicsExam);
        }

        static void EnsureSpecializedExam(string title, Action addExam)
        {
            Quiz quiz = All.Quizzes.FirstOrDefault(q => q.Title == title);
            if (quiz == null)
            {
                addExam();
                return;
            }

            quiz.Available = true;
        }

        static void AddQuestionsUntilTen(Quiz quiz, string choiceQuestion, string choiceAnswer,
                                         string shortQuestion, string shortAnswer)
        {
            int next = Db.QuestionsOf(quiz.Id).Count;
            while (next < 10)
            {
                switch (next % 4)
                {
                    case 0:
                        AddChoice(quiz, choiceQuestion, 0, 2, choiceAnswer, "class", "method", "namespace");
                        break;
                    case 1:
                        AddTrueFalse(quiz, "A program can use variables to store values.", true, 1);
                        break;
                    case 2:
                        AddShort(quiz, shortQuestion, shortAnswer, 2);
                        break;
                    default:
                        AddMulti(quiz, "Which are common programming concepts?", "ABD", 2, "Variables", "Loops", "Paint", "Functions");
                        break;
                }
                next++;
            }
        }

        static void AddExam(string title, string description, int minutes,
                            string choiceQuestion, string choiceAnswer,
                            string shortQuestion, string shortAnswer)
        {
            Quiz quiz = AddQuiz(title, description, minutes, true);
            AddChoice(quiz, choiceQuestion, 0, 2, choiceAnswer, "class", "method", "namespace");
            AddChoice(quiz, "Which option is commonly used to store a group of values?", 1, 2, "Boolean", "Array", "Operator", "Comment");
            AddChoice(quiz, "Which value represents a successful true condition?", 2, 1, "0", "null", "true", "empty");
            AddTrueFalse(quiz, "A program can use variables to store values.", true, 1);
            AddTrueFalse(quiz, "Every programming language uses exactly the same syntax.", false, 1);
            AddShort(quiz, shortQuestion, shortAnswer, 2);
            AddShort(quiz, "Type the common English word for a reusable block of instructions.", "function|method", 2);
            AddMulti(quiz, "Which of these are common programming concepts?", "ABD", 2, "Variables", "Loops", "Paint", "Functions");
            AddMulti(quiz, "Which items can help organize a software project?", "ABC", 2, "Classes", "Methods", "Tests", "Weather");
            AddMulti(quiz, "Which are valid benefits of testing software?", "ACD", 2, "Finding bugs", "Changing the monitor", "Improving confidence", "Preventing regressions");
        }

        static void AddWindowsFormsExam()
        {
            Quiz quiz = AddQuiz("Windows Forms Controls & Event Handling", "Controls, forms and event-driven programming", 10, true);
            AddChoice(quiz, "Which control is used to enter a single line of text?", 0, 2, "TextBox", "Label", "Panel", "Timer");
            AddChoice(quiz, "Which event occurs when a button is pressed?", 1, 2, "Load", "Click", "Paint", "Close");
            AddChoice(quiz, "Which control allows a user to select one item from a list?", 2, 1, "Label", "Panel", "ComboBox", "Timer");
            AddTrueFalse(quiz, "A Windows Form can contain controls.", true, 1);
            AddTrueFalse(quiz, "The Tick event is commonly raised by a Timer.", true, 1);
            AddShort(quiz, "Which event is commonly used when a form is first displayed?", "Load", 2);
            AddShort(quiz, "What is the method used to display a modal form?", "ShowDialog", 2);
            AddMulti(quiz, "Which are Windows Forms controls?", "ABC", 2, "Button", "TextBox", "CheckBox", "Database");
            AddMulti(quiz, "Which properties can control a control's position or size?", "ABD", 2, "Left", "Width", "Color", "Top");
            AddMulti(quiz, "Which are common event-handling practices?", "ACD", 2, "Subscribe to an event", "Ignore all events", "Validate input", "Handle exceptions");
        }

        static void AddGraphicsExam()
        {
            Quiz quiz = AddQuiz("GDI+ / Graphics / Visualization", "Drawing shapes, colors and graphics in .NET", 10, true);
            AddChoice(quiz, "Which class represents a drawing surface in GDI+?", 0, 2, "Graphics", "Bitmap", "Color", "Pen");
            AddChoice(quiz, "Which object is used to draw the outline of a shape?", 1, 2, "Brush", "Pen", "Font", "Point");
            AddChoice(quiz, "Which object fills a shape with color?", 2, 1, "Pen", "Point", "Brush", "Size");
            AddTrueFalse(quiz, "A Bitmap can store a raster image.", true, 1);
            AddTrueFalse(quiz, "The Paint event can be used to draw on a control.", true, 1);
            AddShort(quiz, "Which method draws a rectangle outline?", "DrawRectangle", 2);
            AddShort(quiz, "Which structure represents an x and y coordinate?", "Point", 2);
            AddMulti(quiz, "Which are commonly used in GDI+ drawing?", "ABC", 2, "Graphics", "Pen", "Brush", "Database");
            AddMulti(quiz, "Which can be used to represent a color?", "ABD", 2, "Color", "Brush", "Bitmap", "Timer");
            AddMulti(quiz, "Which are useful for visualization?", "ACD", 2, "Charts", "Passwords", "Shapes", "Images");
        }

        static void AddUser(string name, string username, string email, string password, string role)
        {
            All.Users.Add(new User { Name = name, Username = username, Email = email, Password = Hash(password), Role = role });
        }

        static Quiz AddQuiz(string title, string description, int minutes, bool available)
        {
            var q = new Quiz { Id = NewId(), Title = title, Description = description, Subject = SubjectForTitle(title), Available = available };
            All.Quizzes.Add(q);
            return q;
        }

        static void AddQuestion(Quiz quiz, string type, string text, int correct, string answer, int marks, params string[] options)
        {
            All.Questions.Add(new Question { Id = NewId(), QuizId = quiz.Id, Type = type, Text = text, Correct = correct,
                                             Answer = answer, Marks = marks, Options = new List<string>(options) });
        }

        static void AddChoice(Quiz q, string text, int correct, int marks, params string[] options)
        {
            AddQuestion(q, Question.Types[0], text, correct, null, marks, options);
        }

        static void AddTrueFalse(Quiz q, string text, bool isTrue, int marks)
        {
            AddQuestion(q, Question.Types[1], text, isTrue ? 0 : 1, null, marks, "True", "False");
        }

        static void AddShort(Quiz q, string text, string answer, int marks)
        {
            AddQuestion(q, Question.Types[2], text, 0, answer, marks);
        }

        static void AddMulti(Quiz q, string text, string letters, int marks, params string[] options)
        {
            AddQuestion(q, Question.Types[3], text, 0, letters, marks, options);
        }
    }
}
