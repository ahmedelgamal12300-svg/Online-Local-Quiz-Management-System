using System;
using System.Windows.Forms;

namespace QuizApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Db.Start();

            // login -> main window -> (logout) -> login again
            while (true)
            {
                var login = new LoginForm();
                if (login.ShowDialog() != DialogResult.OK) break;

                var main = new MainForm();
                Application.Run(main);
                if (!main.LoggedOut) break;
            }
        }
    }
}
