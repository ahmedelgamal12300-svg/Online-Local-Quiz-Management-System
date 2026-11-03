using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace QuizApp
{
    public class LoginForm : Form
    {
        TextBox userBox, passBox;
        CheckBox rememberBox;
        Label errorLabel;
        string rememberFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "remember.txt");

        public LoginForm()
        {
            Text = "Quiz Management System - Login";
            ClientSize = new Size(380, 380);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 10);

            var title = UI.MakeLabel("Quiz Management System", 40, 25);
            title.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            title.ForeColor = UI.Blue;

            userBox = UI.MakeText(40, 95, 300);
            passBox = UI.MakeText(40, 150, 300);
            passBox.UseSystemPasswordChar = true;
            rememberBox = new CheckBox { Text = "Remember me", Left = 40, Top = 185, AutoSize = true };
            errorLabel = UI.MakeLabel("", 40, 215);
            errorLabel.ForeColor = Color.Firebrick;

            var loginButton = UI.MakeButton("Login", 40, 250, 300, (s, e) => DoLogin());
            var registerButton = UI.MakeButton("Create new account", 40, 290, 300, (s, e) => new RegisterForm().ShowDialog());
            AcceptButton = loginButton;   // Enter key = login

            Controls.AddRange(new Control[] { title, UI.MakeLabel("Username or email", 40, 70), userBox,
                UI.MakeLabel("Password", 40, 125), passBox, rememberBox, errorLabel, loginButton, registerButton,
                UI.MakeLabel("Demo: admin / Admin@123    student / Student@123", 40, 335) });

            if (File.Exists(rememberFile)) { userBox.Text = File.ReadAllText(rememberFile); rememberBox.Checked = true; }
        }

        void DoLogin()
        {
            if (userBox.Text.Trim() == "" || passBox.Text == "")
            {
                errorLabel.Text = "Please enter your username and password.";
                return;
            }
            User user = Db.Login(userBox.Text, passBox.Text);
            if (user == null)
            {
                errorLabel.Text = "Wrong username or password.";
                return;
            }

            if (rememberBox.Checked) File.WriteAllText(rememberFile, user.Username);
            else if (File.Exists(rememberFile)) File.Delete(rememberFile);

            Db.Me = user;
            DialogResult = DialogResult.OK;
        }
    }

    public class RegisterForm : Form
    {
        TextBox nameBox, userBox, emailBox, passBox, confirmBox;

        public RegisterForm()
        {
            Text = "Create account";
            ClientSize = new Size(360, 470);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 10);

            nameBox = UI.MakeText(30, 35, 300);
            userBox = UI.MakeText(30, 90, 300);
            emailBox = UI.MakeText(30, 145, 300);
            passBox = UI.MakeText(30, 200, 300);
            confirmBox = UI.MakeText(30, 255, 300);
            passBox.UseSystemPasswordChar = true;
            confirmBox.UseSystemPasswordChar = true;

            Controls.AddRange(new Control[] {
                UI.MakeLabel("Full name", 30, 10), nameBox, UI.MakeLabel("Username", 30, 65), userBox,
                UI.MakeLabel("Email", 30, 120), emailBox, UI.MakeLabel("Password (6+ characters)", 30, 175), passBox,
                UI.MakeLabel("Confirm password", 30, 230), confirmBox,
                UI.MakeLabel("New accounts are Student accounts.", 30, 295),
                UI.MakeButton("Register", 30, 335, 300, (s, e) => DoRegister()),
                UI.MakeButton("Cancel", 30, 375, 300, (s, e) => Close()) });
        }

        void DoRegister()
        {
            string error = Db.CheckUser(nameBox.Text, userBox.Text.Trim(), emailBox.Text.Trim(), passBox.Text, true, null);
            if (error == null && passBox.Text != confirmBox.Text) error = "The two passwords are not the same.";
            if (error != null) { UI.Warn(error); return; }

            Db.All.Users.Add(new User { Name = nameBox.Text.Trim(), Username = userBox.Text.Trim(), Email = emailBox.Text.Trim(),
                                        Password = Db.Hash(passBox.Text), Role = "Student" });
            Db.Save();
            UI.Info("Account created. You can login now.");
            Close();
        }
    }
}
