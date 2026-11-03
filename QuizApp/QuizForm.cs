using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace QuizApp
{
    // The window where the student answers a quiz
    public class QuizForm : Form
    {
        Quiz quiz;
        List<Question> questions;
        int[] answers;            // chosen option for each question, -1 = not answered
        int current = 0;
        int secondsLeft;
        bool finished = false;
        bool loading = false;

        Label numberLabel, textLabel, timeLabel;
        RadioButton[] options = new RadioButton[4];
        Button prevButton, nextButton;
        Timer timer = new Timer { Interval = 1000 };

        public QuizForm(Quiz quiz)
        {
            this.quiz = quiz;
            questions = Db.QuestionsOf(quiz.Id);
            answers = new int[questions.Count];
            for (int i = 0; i < answers.Length; i++) answers[i] = -1;
            secondsLeft = quiz.Minutes * 60;

            Text = quiz.Title;
            ClientSize = new Size(640, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 10);

            numberLabel = UI.MakeLabel("", 25, 20);
            timeLabel = UI.MakeLabel("", 500, 20);
            timeLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            timeLabel.ForeColor = Color.Firebrick;
            textLabel = new Label { Left = 25, Top = 60, Width = 590, Height = 60, Font = new Font("Segoe UI", 13, FontStyle.Bold) };
            Controls.AddRange(new Control[] { numberLabel, timeLabel, textLabel });

            for (int i = 0; i < 4; i++)
            {
                options[i] = new RadioButton { Left = 40, Top = 135 + i * 45, Width = 560, Height = 35, Tag = i };
                options[i].CheckedChanged += OptionChanged;
                Controls.Add(options[i]);
            }

            prevButton = UI.MakeButton("Previous", 25, 350, 100, (s, e) => ShowQuestion(current - 1));
            nextButton = UI.MakeButton("Next", 135, 350, 100, (s, e) => ShowQuestion(current + 1));
            Controls.AddRange(new Control[] { prevButton, nextButton, UI.MakeButton("Submit quiz", 485, 350, 130, (s, e) => Submit(false)) });

            timer.Tick += (s, e) =>
            {
                secondsLeft--;
                ShowTime();
                if (secondsLeft <= 0) Submit(true);
            };
            FormClosing += (s, e) =>
            {
                if (!finished && !UI.Ask("Leave without submitting? Your answers will be lost."))
                    e.Cancel = true;
                else timer.Stop();
            };

            ShowQuestion(0);
            ShowTime();
            timer.Start();
        }

        void ShowTime()
        {
            timeLabel.Text = (secondsLeft / 60).ToString("00") + ":" + (secondsLeft % 60).ToString("00");
        }

        void ShowQuestion(int index)
        {
            current = index;
            loading = true;                       // so changing the radio buttons here is not saved as an answer
            Question q = questions[index];
            numberLabel.Text = "Question " + (index + 1) + " of " + questions.Count + "   (" + q.Marks + " marks)";
            textLabel.Text = q.Text;
            for (int i = 0; i < 4; i++)
            {
                options[i].Text = (char)('A' + i) + ".  " + q.Options[i];
                options[i].Checked = (answers[index] == i);
            }
            prevButton.Enabled = index > 0;
            nextButton.Enabled = index < questions.Count - 1;
            loading = false;
        }

        void OptionChanged(object sender, EventArgs e)
        {
            var radio = (RadioButton)sender;
            if (!loading && radio.Checked) answers[current] = (int)radio.Tag;
        }

        void Submit(bool timeIsUp)
        {
            if (!timeIsUp)
            {
                int left = 0;
                foreach (int a in answers) if (a == -1) left++;
                string text = left > 0 ? "You did not answer " + left + " question(s). Submit anyway?" : "Submit your answers?";
                if (!UI.Ask(text)) return;
            }
            timer.Stop();

            var result = new Result { Username = Db.Me.Username, Student = Db.Me.Name, QuizId = quiz.Id, QuizTitle = quiz.Title, Date = DateTime.Now };
            for (int i = 0; i < questions.Count; i++)
            {
                result.Total += questions[i].Marks;
                if (answers[i] == questions[i].Correct) { result.Score += questions[i].Marks; result.CorrectCount++; }
                else result.WrongCount++;
            }
            Db.All.Results.Add(result);
            Db.Save();

            finished = true;
            UI.Info((timeIsUp ? "Time is up!\n\n" : "") + "Your score: " + result.Score + " / " + result.Total + " (" + result.Percent + "%)\n"
                    + (result.Passed ? "You passed." : "You did not pass.") + "\nCorrect: " + result.CorrectCount + "   Wrong: " + result.WrongCount);
            Close();
        }
    }
}
