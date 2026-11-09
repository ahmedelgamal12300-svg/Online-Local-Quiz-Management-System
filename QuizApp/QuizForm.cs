using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace QuizApp
{
    // The window where the student answers a quiz.
    // Every answer is kept as text: "2" (option index), "AC" (letters) or what the student typed.
    public class QuizForm : Form
    {
        Quiz quiz;
        List<Question> questions;
        string[] answers;
        int current = 0;
        int secondsLeft;
        bool finished = false;
        bool loading = false;

        Label numberLabel, textLabel, timeLabel, hintLabel;
        RadioButton[] radios = new RadioButton[4];
        CheckBox[] checks = new CheckBox[4];
        TextBox answerBox;
        Button prevButton, nextButton;
        Timer timer = new Timer { Interval = 1000 };

        public QuizForm(Quiz quiz)
        {
            this.quiz = quiz;
            questions = Db.QuestionsOf(quiz.Id);
            answers = new string[questions.Count];
            secondsLeft = quiz.Minutes * 60;

            Text = quiz.Title;
            ClientSize = new Size(640, 430);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 10);

            numberLabel = UI.MakeLabel("", 25, 20);
            timeLabel = UI.MakeLabel("", 500, 20);
            timeLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            timeLabel.ForeColor = Color.Firebrick;
            textLabel = new Label { Left = 25, Top = 55, Width = 590, Height = 60, Font = new Font("Segoe UI", 13, FontStyle.Bold) };
            hintLabel = UI.MakeLabel("", 25, 120);
            hintLabel.ForeColor = Color.Gray;
            Controls.AddRange(new Control[] { numberLabel, timeLabel, textLabel, hintLabel });

            for (int i = 0; i < 4; i++)
            {
                radios[i] = new RadioButton { Left = 40, Top = 145 + i * 42, Width = 560, Height = 32, Tag = i };
                radios[i].CheckedChanged += OptionChanged;
                checks[i] = new CheckBox { Left = 40, Top = 145 + i * 42, Width = 560, Height = 32 };
                checks[i].CheckedChanged += OptionChanged;
                Controls.Add(radios[i]);
                Controls.Add(checks[i]);
            }
            answerBox = new TextBox { Left = 40, Top = 150, Width = 520, Font = new Font("Segoe UI", 12) };
            answerBox.TextChanged += OptionChanged;
            Controls.Add(answerBox);

            prevButton = UI.MakeButton("Previous", 25, 380, 100, (s, e) => ShowQuestion(current - 1));
            nextButton = UI.MakeButton("Next", 135, 380, 100, (s, e) => ShowQuestion(current + 1));
            Controls.AddRange(new Control[] { prevButton, nextButton, UI.MakeButton("Submit quiz", 485, 380, 130, (s, e) => Submit(false)) });

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
            loading = true;                       // so changing the controls here is not saved as an answer
            Question q = questions[index];
            string type = q.QuestionType;
            string saved = answers[index] ?? "";

            numberLabel.Text = "Question " + (index + 1) + " of " + questions.Count + "   (" + q.Marks + " marks)";
            textLabel.Text = q.Text;
            hintLabel.Text = type == "Multiple answers" ? "Choose all the correct answers." :
                             type == "Short answer" ? "Type your answer." : "";

            for (int i = 0; i < 4; i++)
            {
                bool used = i < q.Options.Count;
                string label = (type == "True / False" ? "" : (char)('A' + i) + ".  ") + (used ? q.Options[i] : "");
                radios[i].Visible = used && (type == "Multiple choice" || type == "True / False");
                radios[i].Text = label;
                radios[i].Checked = (saved == i.ToString());
                checks[i].Visible = used && type == "Multiple answers";
                checks[i].Text = label;
                checks[i].Checked = saved.Contains(((char)('A' + i)).ToString());
            }
            answerBox.Visible = (type == "Short answer");
            answerBox.Text = type == "Short answer" ? saved : "";

            prevButton.Enabled = index > 0;
            nextButton.Enabled = index < questions.Count - 1;
            loading = false;
        }

        // called when the student picks, ticks or types something
        void OptionChanged(object sender, EventArgs e)
        {
            if (loading) return;
            string type = questions[current].QuestionType;
            if (type == "Short answer") answers[current] = answerBox.Text;
            else if (type == "Multiple answers")
            {
                string letters = "";
                for (int i = 0; i < 4; i++) if (checks[i].Checked) letters += (char)('A' + i);
                answers[current] = letters;
            }
            else
            {
                var radio = sender as RadioButton;
                if (radio != null && radio.Checked) answers[current] = radio.Tag.ToString();
            }
        }

        void Submit(bool timeIsUp)
        {
            if (!timeIsUp)
            {
                int left = 0;
                foreach (string a in answers) if (string.IsNullOrWhiteSpace(a)) left++;
                string text = left > 0 ? "You did not answer " + left + " question(s). Submit anyway?" : "Submit your answers?";
                if (!UI.Ask(text)) return;
            }
            timer.Stop();

            var result = new Result { Username = Db.Me.Username, Student = Db.Me.Name, QuizId = quiz.Id, QuizTitle = quiz.Title, Date = DateTime.Now };
            for (int i = 0; i < questions.Count; i++)
            {
                result.Total += questions[i].Marks;
                if (questions[i].IsCorrect(answers[i])) { result.Score += questions[i].Marks; result.CorrectCount++; }
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
