using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace QuizApp
{
    // Role is just a text: "Admin" (also acts as teacher) or "Student"
    [DataContract]
    public class User
    {
        [DataMember] public string Name, Username, Email, Password, Role;
    }

    [DataContract]
    public class Quiz
    {
        [DataMember] public int Id, Minutes;
        [DataMember] public string Title, Description;
        [DataMember] public bool Available;
    }

    // A question can be one of 4 types. How the right answer is stored depends on the type:
    //   Multiple choice   -> Correct = index of the right option (0-3)
    //   True / False      -> Correct = 0 (True) or 1 (False)
    //   Short answer      -> Answer  = accepted answers separated by |   (example: new|New)
    //   Multiple answers  -> Answer  = letters of all right options      (example: ACD)
    [DataContract]
    public class Question
    {
        public static readonly string[] Types = { "Multiple choice", "True / False", "Short answer", "Multiple answers" };

        [DataMember] public int Id, QuizId, Correct, Marks;
        [DataMember] public string Text, Type, Answer;
        [DataMember] public List<string> Options = new List<string>();

        // old data files have no Type, so treat them as multiple choice
        public string QuestionType { get { return string.IsNullOrEmpty(Type) ? Types[0] : Type; } }

        // "given" is what the student answered: option index ("2"), letters ("AC") or text
        public bool IsCorrect(string given)
        {
            if (string.IsNullOrWhiteSpace(given)) return false;
            if (QuestionType == "Short answer")
                return Answer.Split('|').Any(a => a.Trim().ToLower() == given.Trim().ToLower());
            if (QuestionType == "Multiple answers")
                return Letters(given) == Letters(Answer);
            return given == Correct.ToString();
        }

        // text shown as "the correct answer" in lists
        public string CorrectText()
        {
            if (QuestionType == "Short answer") return Answer.Replace("|", " or ");
            if (QuestionType == "Multiple answers") return string.Join(", ", Letters(Answer).Select(c => c.ToString()));
            return QuestionType == "True / False" ? Options[Correct] : (char)('A' + Correct) + ". " + Options[Correct];
        }

        static string Letters(string text)
        {
            return new string(text.ToUpper().Where(char.IsLetter).OrderBy(c => c).ToArray());
        }
    }

    [DataContract]
    public class Result
    {
        [DataMember] public string Username, Student, QuizTitle;
        [DataMember] public int QuizId, Score, Total, CorrectCount, WrongCount;
        [DataMember] public DateTime Date;

        public int Percent { get { return Total == 0 ? 0 : Score * 100 / Total; } }
        public bool Passed { get { return Percent >= 50; } }   // pass mark is 50%
    }

    // Everything the app saves
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
