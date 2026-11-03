using System;
using System.Collections.Generic;
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

    [DataContract]
    public class Question
    {
        [DataMember] public int Id, QuizId, Correct, Marks;   // Correct = index of the right option (0-3)
        [DataMember] public string Text;
        [DataMember] public List<string> Options = new List<string>();
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
