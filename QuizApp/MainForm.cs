using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuizApp
{
    // The main window: a menu on the left (tabs) and one page for each screen
    public class MainForm : Form
    {
        public bool LoggedOut = false;
        bool isAdmin = Db.Me.Role == "Admin";
        TabControl tabs = new TabControl();

        // dashboard
        Label welcomeLabel, statsLabel;
        ListBox recentBox;
        // quizzes
        ListView quizList;
        TextBox quizSearch, titleBox, descBox, minutesBox;
        CheckBox availableBox;
        // questions
        ComboBox quizPicker, typeBox, correctBox;
        ListView questionList;
        TextBox questionBox, shortAnswerBox;
        TextBox[] optionBoxes = new TextBox[4];
        CheckBox[] rightChecks = new CheckBox[4];
        Label[] optionLabels = new Label[4];
        Label correctLabel, shortLabel;
        NumericUpDown marksBox;
        // results, users, reports
        ListView resultList, userList, reportList, studentList;
        TextBox userSearch, nameBox, usernameBox, emailBox, passwordBox;
        ComboBox roleBox;
        // settings
        TextBox oldPass, newPass, confirmPass;
        ComboBox modeBox;

        public MainForm()
        {
            Text = "Quiz Management System";
            ClientSize = new Size(1100, 680);
            MinimumSize = new Size(950, 600);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);

            // blue bar at the top with the user name
            var header = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = UI.Blue };
            var appName = UI.MakeLabel("Quiz Management System", 15, 14);
            appName.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            appName.ForeColor = Color.White;
            var userName = UI.MakeLabel(Db.Me.Name + "  (" + Db.Me.Role + ")", 0, 18);
            userName.ForeColor = Color.White;
            userName.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            userName.Left = 1100 - 250;
            header.Controls.Add(appName);
            header.Controls.Add(userName);

            // tabs on the left side
            tabs.Dock = DockStyle.Fill;
            tabs.Alignment = TabAlignment.Left;
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.ItemSize = new Size(42, 170);       // for left tabs the width and height are swapped
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.DrawItem += DrawTab;

            Controls.Add(tabs);
            Controls.Add(header);

            BuildDashboard();
            BuildQuizzes();
            if (isAdmin) BuildQuestions();
            BuildResults();
            if (isAdmin) { BuildUsers(); BuildReports(); }
            BuildSettings();

            tabs.SelectedIndexChanged += (s, e) => RefreshAll();
            RefreshAll();
        }

        void DrawTab(object sender, DrawItemEventArgs e)
        {
            bool selected = (e.Index == tabs.SelectedIndex);
            Rectangle r = tabs.GetTabRect(e.Index);
            e.Graphics.FillRectangle(new SolidBrush(selected ? UI.Blue : Color.FromArgb(235, 238, 242)), r);
            TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, Font, new Rectangle(r.X + 15, r.Y, r.Width - 15, r.Height),
                selected ? Color.White : Color.Black, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        TabPage AddTab(string title)
        {
            var page = new TabPage(title) { BackColor = Color.White };
            tabs.TabPages.Add(page);
            return page;
        }

        void RefreshAll()
        {
            FillDashboard();
            FillQuizzes();
            if (isAdmin) { FillQuizPicker(); FillQuestions(); FillUsers(); FillReports(); }
            FillResults();
        }

        // ================= DASHBOARD =================
        void BuildDashboard()
        {
            var page = AddTab("Dashboard");
            welcomeLabel = UI.MakeLabel("", 25, 20);
            welcomeLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            statsLabel = UI.MakeLabel("", 25, 70);
            statsLabel.Font = new Font("Segoe UI", 12);
            recentBox = new ListBox { Left = 25, Top = 190, Width = 600, Height = 250 };
            page.Controls.AddRange(new Control[] { welcomeLabel, statsLabel, UI.MakeLabel("Recent results:", 25, 160), recentBox });
        }

        void FillDashboard()
        {
            welcomeLabel.Text = "Welcome, " + Db.Me.Name;
            int available = Db.All.Quizzes.Count(q => q.Available);
            var mine = Db.MyResults();
            statsLabel.Text = "Total quizzes: " + Db.All.Quizzes.Count + "\n"
                            + "Available quizzes: " + available + "\n"
                            + "Completed quizzes: " + mine.Count + "\n"
                            + "Total questions: " + Db.All.Questions.Count;
            recentBox.Items.Clear();
            foreach (Result r in mine.Take(8))
                recentBox.Items.Add(r.Date.ToString("dd/MM HH:mm") + "   " + r.Student + " - " + r.QuizTitle + " - " + r.Percent + "%");
        }

        // ================= QUIZZES =================
        void BuildQuizzes()
        {
            var page = AddTab("Quizzes");
            quizList = UI.MakeList("Title", "Description", "Duration", "Questions", "Max score", "Available");
            quizList.Columns[1].Width = 250;

            var top = new Panel { Dock = DockStyle.Top, Height = 45 };
            quizSearch = UI.MakeText(75, 8, 200);
            quizSearch.TextChanged += (s, e) => FillQuizzes();
            top.Controls.AddRange(new Control[] { UI.MakeLabel("Search:", 10, 11), quizSearch,
                UI.MakeButton("Take selected quiz", 300, 6, 160, (s, e) => TakeQuiz()) });

            page.Controls.Add(quizList);   // the list fills what is left
            page.Controls.Add(top);

            if (!isAdmin) return;
            var edit = new Panel { Dock = DockStyle.Bottom, Height = 120 };
            titleBox = UI.MakeText(80, 10, 200);
            descBox = UI.MakeText(80, 45, 330);
            minutesBox = UI.MakeText(500, 10, 60);
            availableBox = new CheckBox { Text = "Available to students", Left = 500, Top = 45, AutoSize = true, Checked = true };
            edit.Controls.AddRange(new Control[] { UI.MakeLabel("Title", 10, 13), titleBox, UI.MakeLabel("Description", 10, 48), descBox,
                UI.MakeLabel("Minutes", 430, 13), minutesBox, availableBox,
                UI.MakeButton("Add", 10, 80, 90, (s, e) => SaveQuiz(true)), UI.MakeButton("Update", 110, 80, 90, (s, e) => SaveQuiz(false)),
                UI.MakeButton("Delete", 210, 80, 90, (s, e) => DeleteQuiz()) });
            page.Controls.Add(edit);

            quizList.SelectedIndexChanged += (s, e) =>
            {
                Quiz q = UI.Selected<Quiz>(quizList);
                if (q == null) return;
                titleBox.Text = q.Title; descBox.Text = q.Description;
                minutesBox.Text = q.Minutes.ToString(); availableBox.Checked = q.Available;
            };
        }

        void FillQuizzes()
        {
            quizList.Items.Clear();
            foreach (Quiz q in Db.All.Quizzes)
            {
                if (!isAdmin && !q.Available) continue;                        // students only see available quizzes
                if (q.Title.ToLower().IndexOf(quizSearch.Text.ToLower()) < 0) continue;
                UI.AddRow(quizList, q, q.Title, q.Description, q.Minutes + " min", Db.QuestionsOf(q.Id).Count.ToString(),
                          Db.MaxScore(q.Id).ToString(), q.Available ? "Yes" : "No");
            }
        }

        void SaveQuiz(bool isNew)
        {
            Quiz q = isNew ? new Quiz() : UI.Selected<Quiz>(quizList);
            if (q == null) { UI.Warn("Select a quiz first."); return; }
            int minutes;
            if (titleBox.Text.Trim() == "") { UI.Warn("Please enter a title."); return; }
            if (!int.TryParse(minutesBox.Text, out minutes) || minutes < 1) { UI.Warn("Minutes must be a number like 10."); return; }

            q.Title = titleBox.Text.Trim(); q.Description = descBox.Text.Trim();
            q.Minutes = minutes; q.Available = availableBox.Checked;
            if (isNew) { q.Id = Db.NewId(); Db.All.Quizzes.Add(q); }
            Db.Save();
            FillQuizzes();
        }

        void DeleteQuiz()
        {
            Quiz q = UI.Selected<Quiz>(quizList);
            if (q == null) { UI.Warn("Select a quiz first."); return; }
            if (!UI.Ask("Delete this quiz and all its questions?")) return;
            Db.DeleteQuiz(q);
            RefreshAll();
        }

        void TakeQuiz()
        {
            Quiz q = UI.Selected<Quiz>(quizList);
            if (q == null) { UI.Warn("Select a quiz first."); return; }
            if (!q.Available) { UI.Warn("This quiz is not available."); return; }
            if (Db.QuestionsOf(q.Id).Count == 0) { UI.Warn("This quiz has no questions yet."); return; }
            new QuizForm(q).ShowDialog(this);
            RefreshAll();
        }

        // ================= QUESTIONS (admin) =================
        void BuildQuestions()
        {
            var page = AddTab("Questions");
            questionList = UI.MakeList("Question", "Type", "Correct answer", "Marks");
            questionList.Columns[0].Width = 380;
            questionList.Columns[2].Width = 200;

            var top = new Panel { Dock = DockStyle.Top, Height = 45 };
            quizPicker = new ComboBox { Left = 75, Top = 8, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
            quizPicker.SelectedIndexChanged += (s, e) => FillQuestions();
            top.Controls.AddRange(new Control[] { UI.MakeLabel("Quiz:", 10, 11), quizPicker });

            var edit = new Panel { Dock = DockStyle.Bottom, Height = 235 };
            questionBox = UI.MakeText(80, 10, 600);
            typeBox = new ComboBox { Left = 80, Top = 45, Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
            typeBox.Items.AddRange(Question.Types);
            typeBox.SelectedIndex = 0;
            typeBox.SelectedIndexChanged += (s, e) => UpdateEditor();
            marksBox = new NumericUpDown { Left = 330, Top = 45, Width = 60, Minimum = 1, Maximum = 50, Value = 1 };
            edit.Controls.AddRange(new Control[] { UI.MakeLabel("Question", 10, 13), questionBox, UI.MakeLabel("Type", 10, 48), typeBox,
                UI.MakeLabel("Marks", 270, 48), marksBox });

            // the four answers (used by "Multiple choice" and "Multiple answers")
            for (int i = 0; i < 4; i++)
            {
                int x = 80 + (i % 2) * 360, y = 85 + (i / 2) * 35;
                optionLabels[i] = UI.MakeLabel("" + (char)('A' + i), x - 20, y + 3);
                optionBoxes[i] = UI.MakeText(x, y, 240);
                rightChecks[i] = new CheckBox { Text = "right", Left = x + 250, Top = y + 2, AutoSize = true };
                edit.Controls.AddRange(new Control[] { optionLabels[i], optionBoxes[i], rightChecks[i] });
            }
            correctLabel = UI.MakeLabel("Correct answer", 10, 160);
            correctBox = new ComboBox { Left = 120, Top = 157, Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
            shortLabel = UI.MakeLabel("Accepted answers (separate with | )", 10, 88);
            shortAnswerBox = UI.MakeText(250, 85, 300);
            edit.Controls.AddRange(new Control[] { correctLabel, correctBox, shortLabel, shortAnswerBox,
                UI.MakeButton("Add", 10, 195, 90, (s, e) => SaveQuestion(true)), UI.MakeButton("Update", 110, 195, 90, (s, e) => SaveQuestion(false)),
                UI.MakeButton("Delete", 210, 195, 90, (s, e) => DeleteQuestion()) });

            page.Controls.Add(questionList);
            page.Controls.Add(top);
            page.Controls.Add(edit);

            questionList.SelectedIndexChanged += (s, e) =>
            {
                Question q = UI.Selected<Question>(questionList);
                if (q == null) return;
                typeBox.SelectedItem = q.QuestionType;          // this also changes which controls are visible
                questionBox.Text = q.Text;
                marksBox.Value = q.Marks;
                for (int i = 0; i < 4; i++)
                {
                    optionBoxes[i].Text = i < q.Options.Count ? q.Options[i] : "";
                    rightChecks[i].Checked = q.QuestionType == "Multiple answers" && q.Answer.ToUpper().Contains((char)('A' + i));
                }
                shortAnswerBox.Text = q.QuestionType == "Short answer" ? q.Answer : "";
                if (q.QuestionType == "Multiple choice" || q.QuestionType == "True / False") correctBox.SelectedIndex = q.Correct;
            };
            UpdateEditor();
        }

        // show only the controls that this type of question needs
        void UpdateEditor()
        {
            string type = typeBox.SelectedItem.ToString();
            bool choices = (type == "Multiple choice" || type == "Multiple answers");
            for (int i = 0; i < 4; i++)
            {
                optionLabels[i].Visible = optionBoxes[i].Visible = choices;
                rightChecks[i].Visible = (type == "Multiple answers");
            }
            shortLabel.Visible = shortAnswerBox.Visible = (type == "Short answer");
            correctLabel.Visible = correctBox.Visible = (type == "Multiple choice" || type == "True / False");

            correctBox.Items.Clear();
            if (type == "True / False") correctBox.Items.AddRange(new object[] { "True", "False" });
            else correctBox.Items.AddRange(new object[] { "A", "B", "C", "D" });
            correctBox.SelectedIndex = 0;
        }

        void FillQuizPicker()
        {
            int index = quizPicker.SelectedIndex;          // remember which quiz was picked
            quizPicker.Items.Clear();
            foreach (Quiz q in Db.All.Quizzes) quizPicker.Items.Add(q.Title);
            if (quizPicker.Items.Count > 0) quizPicker.SelectedIndex = Math.Max(0, Math.Min(index, quizPicker.Items.Count - 1));
        }

        Quiz PickedQuiz()
        {
            return quizPicker.SelectedIndex < 0 ? null : Db.All.Quizzes[quizPicker.SelectedIndex];
        }

        void FillQuestions()
        {
            questionList.Items.Clear();
            Quiz quiz = PickedQuiz();
            if (quiz == null) return;
            foreach (Question q in Db.QuestionsOf(quiz.Id))
                UI.AddRow(questionList, q, q.Text, q.QuestionType, q.CorrectText(), q.Marks.ToString());
        }

        void SaveQuestion(bool isNew)
        {
            Quiz quiz = PickedQuiz();
            Question q = isNew ? new Question() : UI.Selected<Question>(questionList);
            if (quiz == null || q == null) { UI.Warn(isNew ? "Create a quiz first." : "Select a question first."); return; }
            if (questionBox.Text.Trim() == "") { UI.Warn("Please write the question."); return; }

            string type = typeBox.SelectedItem.ToString();
            var options = new System.Collections.Generic.List<string>();
            int correct = 0;
            string answer = null;

            if (type == "True / False")
            {
                options.AddRange(new[] { "True", "False" });
                correct = correctBox.SelectedIndex;
            }
            else if (type == "Short answer")
            {
                answer = shortAnswerBox.Text.Trim();
                if (answer == "") { UI.Warn("Please write the accepted answer."); return; }
            }
            else
            {
                if (optionBoxes.Any(b => b.Text.Trim() == "")) { UI.Warn("Please fill all 4 answers."); return; }
                options = optionBoxes.Select(b => b.Text.Trim()).ToList();
                if (type == "Multiple choice") correct = correctBox.SelectedIndex;
                else
                {
                    for (int i = 0; i < 4; i++) if (rightChecks[i].Checked) answer += (char)('A' + i);
                    if (answer == null) { UI.Warn("Tick at least one right answer."); return; }
                }
            }

            q.QuizId = quiz.Id; q.Text = questionBox.Text.Trim(); q.Type = type;
            q.Options = options; q.Correct = correct; q.Answer = answer; q.Marks = (int)marksBox.Value;
            if (isNew) { q.Id = Db.NewId(); Db.All.Questions.Add(q); }
            Db.Save();
            FillQuestions();
            FillQuizzes();
        }

        void DeleteQuestion()
        {
            Question q = UI.Selected<Question>(questionList);
            if (q == null) { UI.Warn("Select a question first."); return; }
            if (!UI.Ask("Delete this question?")) return;
            Db.All.Questions.Remove(q);
            Db.Save();
            FillQuestions();
        }

        // ================= RESULTS =================
        void BuildResults()
        {
            var page = AddTab("Results");
            resultList = UI.MakeList("Student", "Quiz", "Score", "Percentage", "Status", "Correct", "Wrong", "Date");
            page.Controls.Add(resultList);
        }

        void FillResults()
        {
            resultList.Items.Clear();
            foreach (Result r in Db.MyResults())
                UI.AddRow(resultList, r, r.Student, r.QuizTitle, r.Score + " / " + r.Total, r.Percent + "%", r.Passed ? "Pass" : "Fail",
                          r.CorrectCount.ToString(), r.WrongCount.ToString(), r.Date.ToString("dd/MM/yyyy HH:mm"));
        }

        // ================= USERS (admin) =================
        void BuildUsers()
        {
            var page = AddTab("Users");
            userList = UI.MakeList("Name", "Username", "Email", "Role");
            userList.Columns[2].Width = 220;

            var top = new Panel { Dock = DockStyle.Top, Height = 45 };
            userSearch = UI.MakeText(75, 8, 250);
            userSearch.TextChanged += (s, e) => FillUsers();
            top.Controls.AddRange(new Control[] { UI.MakeLabel("Search:", 10, 11), userSearch });

            var edit = new Panel { Dock = DockStyle.Bottom, Height = 130 };
            nameBox = UI.MakeText(90, 10, 200); usernameBox = UI.MakeText(90, 45, 200);
            emailBox = UI.MakeText(420, 10, 220); passwordBox = UI.MakeText(420, 45, 220);
            roleBox = new ComboBox { Left = 740, Top = 10, Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            roleBox.Items.AddRange(new object[] { "Student", "Admin" });
            roleBox.SelectedIndex = 0;
            edit.Controls.AddRange(new Control[] { UI.MakeLabel("Name", 10, 13), nameBox, UI.MakeLabel("Username", 10, 48), usernameBox,
                UI.MakeLabel("Email", 330, 13), emailBox, UI.MakeLabel("Password", 330, 48), passwordBox, UI.MakeLabel("Role", 690, 13), roleBox,
                UI.MakeLabel("(leave the password empty to keep the old one when updating)", 90, 80),
                UI.MakeButton("Add", 10, 95, 90, (s, e) => SaveUser(true)), UI.MakeButton("Update", 110, 95, 90, (s, e) => SaveUser(false)),
                UI.MakeButton("Delete", 210, 95, 90, (s, e) => DeleteUser()) });

            page.Controls.Add(userList);
            page.Controls.Add(top);
            page.Controls.Add(edit);

            userList.SelectedIndexChanged += (s, e) =>
            {
                User u = UI.Selected<User>(userList);
                if (u == null) return;
                nameBox.Text = u.Name; usernameBox.Text = u.Username; emailBox.Text = u.Email;
                passwordBox.Text = ""; roleBox.SelectedItem = u.Role;
            };
        }

        void FillUsers()
        {
            userList.Items.Clear();
            foreach (User u in Db.All.Users)
                if ((u.Name + u.Username + u.Email).ToLower().Contains(userSearch.Text.ToLower()))
                    UI.AddRow(userList, u, u.Name, u.Username, u.Email, u.Role);
        }

        void SaveUser(bool isNew)
        {
            User u = isNew ? new User() : UI.Selected<User>(userList);
            if (u == null) { UI.Warn("Select a user first."); return; }
            string error = Db.CheckUser(nameBox.Text, usernameBox.Text.Trim(), emailBox.Text.Trim(), passwordBox.Text, isNew, isNew ? null : u);
            if (error != null) { UI.Warn(error); return; }

            u.Name = nameBox.Text.Trim(); u.Username = usernameBox.Text.Trim(); u.Email = emailBox.Text.Trim();
            u.Role = roleBox.SelectedItem.ToString();
            if (passwordBox.Text != "") u.Password = Db.Hash(passwordBox.Text);
            if (isNew) Db.All.Users.Add(u);
            Db.Save();
            FillUsers();
        }

        void DeleteUser()
        {
            User u = UI.Selected<User>(userList);
            if (u == null) { UI.Warn("Select a user first."); return; }
            if (u == Db.Me) { UI.Warn("You cannot delete the account you are using."); return; }
            if (!UI.Ask("Delete " + u.Name + "?")) return;
            Db.All.Users.Remove(u);
            Db.Save();
            FillUsers();
        }

        // ================= REPORTS (admin) =================
        void BuildReports()
        {
            var page = AddTab("Reports");
            reportList = UI.MakeList("Quiz", "Attempts", "Average %", "Pass rate %", "Highest %");
            studentList = UI.MakeList("Student", "Quizzes done", "Average %");
            reportList.Dock = DockStyle.Top; reportList.Height = 200;

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 45 };
            bottom.Controls.Add(UI.MakeButton("Export results to CSV", 5, 8, 200, (s, e) => ExportCsv()));

            page.Controls.Add(studentList);   // fills the middle
            page.Controls.Add(reportList);
            page.Controls.Add(bottom);
        }

        void FillReports()
        {
            reportList.Items.Clear();
            foreach (Quiz q in Db.All.Quizzes)
            {
                var rs = Db.All.Results.Where(r => r.QuizId == q.Id).ToList();
                if (rs.Count == 0) UI.AddRow(reportList, q, q.Title, "0", "-", "-", "-");
                else UI.AddRow(reportList, q, q.Title, rs.Count.ToString(), Math.Round(rs.Average(r => r.Percent), 1).ToString(),
                               (rs.Count(r => r.Passed) * 100 / rs.Count).ToString(), rs.Max(r => r.Percent).ToString());
            }
            studentList.Items.Clear();
            foreach (var g in Db.All.Results.GroupBy(r => r.Username))
                UI.AddRow(studentList, null, g.First().Student, g.Count().ToString(), Math.Round(g.Average(r => r.Percent), 1).ToString());
        }

        void ExportCsv()
        {
            var dialog = new SaveFileDialog { Filter = "CSV file|*.csv", FileName = "results.csv" };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            var text = new StringBuilder("Student,Quiz,Score,Total,Percent,Status,Date\r\n");
            foreach (Result r in Db.All.Results)
                text.AppendLine("\"" + r.Student + "\",\"" + r.QuizTitle + "\"," + r.Score + "," + r.Total + "," + r.Percent + "," + (r.Passed ? "Pass" : "Fail") + "," + r.Date.ToString("yyyy-MM-dd HH:mm"));
            File.WriteAllText(dialog.FileName, text.ToString(), Encoding.UTF8);
            UI.Info("Saved: " + dialog.FileName);
        }

        // ================= SETTINGS =================
        void BuildSettings()
        {
            var page = AddTab("Settings");
            oldPass = UI.MakeText(180, 150, 220); newPass = UI.MakeText(180, 185, 220); confirmPass = UI.MakeText(180, 220, 220);
            oldPass.UseSystemPasswordChar = newPass.UseSystemPasswordChar = confirmPass.UseSystemPasswordChar = true;
            modeBox = new ComboBox { Left = 180, Top = 345, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            modeBox.Items.AddRange(new object[] { "Local (this computer)", "Online (planned)" });
            modeBox.SelectedIndex = 0;
            modeBox.SelectionChangeCommitted += (s, e) =>
            {
                if (modeBox.SelectedIndex == 1) UI.Info("Online mode is not built yet (no server). The app keeps using local data.");
                modeBox.SelectedIndex = 0;
            };

            var profile = UI.MakeLabel("Name: " + Db.Me.Name + "\nUsername: " + Db.Me.Username + "\nEmail: " + Db.Me.Email + "\nRole: " + Db.Me.Role, 25, 20);
            profile.Font = new Font("Segoe UI", 11);
            page.Controls.AddRange(new Control[] { profile, UI.MakeLabel("Change password", 25, 115), UI.MakeLabel("Old password", 25, 153), oldPass,
                UI.MakeLabel("New password", 25, 188), newPass, UI.MakeLabel("Confirm", 25, 223), confirmPass,
                UI.MakeButton("Change password", 180, 258, 220, (s, e) => ChangePassword()),
                UI.MakeLabel("Application", 25, 310), UI.MakeLabel("Data mode", 25, 348), modeBox,
                UI.MakeButton("Logout", 25, 420, 120, (s, e) => Logout()) });
        }

        void ChangePassword()
        {
            if (Db.Hash(oldPass.Text) != Db.Me.Password) { UI.Warn("The old password is wrong."); return; }
            if (newPass.Text.Length < 6) { UI.Warn("The new password must be at least 6 characters."); return; }
            if (newPass.Text != confirmPass.Text) { UI.Warn("The new passwords are not the same."); return; }
            Db.Me.Password = Db.Hash(newPass.Text);
            Db.Save();
            oldPass.Clear(); newPass.Clear(); confirmPass.Clear();
            UI.Info("Password changed.");
        }

        void Logout()
        {
            if (!UI.Ask("Do you want to logout?")) return;
            LoggedOut = true;
            Close();
        }
    }
}
