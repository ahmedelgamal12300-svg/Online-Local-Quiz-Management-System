using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuizApp
{
    public static class UI
    {
        public static readonly Color Blue = Color.FromArgb(37, 99, 160);
        public static readonly Color Navy = Color.FromArgb(24, 42, 68);
        public static readonly Color Page = Color.FromArgb(245, 247, 251);
        public static readonly Color Muted = Color.FromArgb(102, 115, 133);

        public static Label MakeLabel(string text, int x, int y)
        {
            return new Label { Text = text, Left = x, Top = y, AutoSize = true, ForeColor = Navy };
        }

        public static TextBox MakeText(int x, int y, int width)
        {
            return new TextBox { Left = x, Top = y, Width = width, Height = 30, BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White, ForeColor = Navy, Font = new Font("Segoe UI", 10) };
        }

        public static Button MakeButton(string text, int x, int y, int width, EventHandler onClick)
        {
            var b = new Button { Text = text, Left = x, Top = y, Width = width, Height = 34,
                FlatStyle = FlatStyle.Flat, BackColor = Blue, ForeColor = Color.White, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.Click += onClick;
            return b;
        }

        public static ListView MakeList(params string[] columns)
        {
            var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = false,
                HideSelection = false, MultiSelect = false, BorderStyle = BorderStyle.None, BackColor = Color.White, ForeColor = Navy,
                Font = new Font("Segoe UI", 10) };
            foreach (string c in columns) list.Columns.Add(c, 130);
            return list;
        }

        public static T Selected<T>(ListView list) where T : class
        {
            return list.SelectedItems.Count == 0 ? null : list.SelectedItems[0].Tag as T;
        }

        public static void AddRow(ListView list, object tag, params string[] cells)
        {
            list.Items.Add(new ListViewItem(cells) { Tag = tag });
        }

        public static void Info(string text) { MessageBox.Show(text, "Quiz App", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        public static void Warn(string text) { MessageBox.Show(text, "Quiz App", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        public static bool Ask(string text) { return MessageBox.Show(text, "Quiz App", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes; }
    }
}
