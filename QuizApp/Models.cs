using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace QuizApp
{
    [DataContract]
    public class User
    {
        [DataMember] public string Name, Username, Email, Password, Role;
        [DataMember] public List<string> Subjects = new List<string>();
    }

    [DataContract]
    public class Quiz
    {
        [DataMember] public int Id, Minutes;
        [DataMember] public string Title, Description, Subject;
        [DataMember] public bool Available;
    }

    [DataContract]
    public class Question
    {
        public static readonly string[] Types = { "Multiple choice", "True / False", "Short answer", "Multiple answers" };

        [DataMember] public int Id, QuizId, Correct, Marks;
        [DataMember] public string Text, Type, Answer;
        [DataMember] public List<string> Options = new List<string>();

        public string QuestionType { get { return string.IsNullOrEmpty(Type) ? Types[0] : Type; } }

        public bool IsCorrect(string given)
        {
            if (string.IsNullOrWhiteSpace(given)) return false;
            if (QuestionType == "Short answer")
                return (Answer ?? "").Split('|').Any(a => a.Trim().Equals(given.Trim(), StringComparison.OrdinalIgnoreCase));
            if (QuestionType == "Multiple answers")
                return Letters(given) == Letters(Answer);
            return given == Correct.ToString();
        }

        public string CorrectText()
        {
            if (QuestionType == "Short answer") return (Answer ?? "").Replace("|", " or ");
            if (QuestionType == "Multiple answers") return string.Join(", ", Letters(Answer).Select(c => c.ToString()));
            if (Options == null || Options.Count == 0) return "Not specified";
            int correct = Math.Max(0, Math.Min(Correct, Options.Count - 1));
            return QuestionType == "True / False" ? Options[correct] : (char)('A' + correct) + ". " + Options[correct];
        }

        static string Letters(string text)
        {
            return new string((text ?? "").ToUpper().Where(char.IsLetter).OrderBy(c => c).ToArray());
        }
    }

    [DataContract]
    public class Result
    {
        [DataMember] public string Username, Student, QuizTitle;
        [DataMember] public int QuizId, Score, Total, CorrectCount, WrongCount;
        [DataMember] public DateTime Date;

        public int Percent { get { return Total == 0 ? 0 : Score * 100 / Total; } }
        public bool Passed { get { return Percent >= 50; } }
    }

    [DataContract]
    public class Data
    {
        [DataMember] public List<User> Users = new List<User>();
        [DataMember] public List<Quiz> Quizzes = new List<Quiz>();
        [DataMember] public List<Question> Questions = new List<Question>();
        [DataMember] public List<Result> Results = new List<Result>();
        [DataMember] public int NextId = 1;
    }
}
