using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuizApp
{
    // Small shortcuts so the forms do not repeat the same lines again and again
    public static class UI
    {
        public static readonly Color Blue = Color.FromArgb(44, 82, 130);

        public static Label MakeLabel(string text, int x, int y)
        {
            return new Label { Text = text, Left = x, Top = y, AutoSize = true };
        }

        public static TextBox MakeText(int x, int y, int width)
        {
            return new TextBox { Left = x, Top = y, Width = width };
        }

        public static Button MakeButton(string text, int x, int y, int width, EventHandler onClick)
        {
            var b = new Button { Text = text, Left = x, Top = y, Width = width, Height = 30 };
            b.Click += onClick;
            return b;
        }

        public static ListView MakeList(params string[] columns)
        {
            var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true, HideSelection = false, MultiSelect = false };
            foreach (string c in columns) list.Columns.Add(c, 130);
            return list;
        }

        // The object stored in the selected row (or null)
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
