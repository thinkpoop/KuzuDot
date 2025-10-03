using System;
using System.Text;
using System.Windows.Forms;

namespace KuzuDot.Demo
{
    public class MainForm : Form
    {
        private readonly TextBox _queryInput;
        private readonly Button _executeButton;
        private readonly TextBox _resultsBox;
        private readonly KuzuDot.Database _database;
        private readonly KuzuDot.Connection _connection;

        public MainForm()
        {
            Text = "KuzuDot Demo";
            Width = 1000;
            Height = 700;

            _queryInput = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Top,
                Height = 150,
                Font = new System.Drawing.Font("Consolas", 10)
            };

            _executeButton = new Button
            {
                Text = "Execute (Ctrl+Enter)",
                Dock = DockStyle.Top,
                Height = 35
            };
            _executeButton.Click += (_, _) => ExecuteQuery();

            _resultsBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 10),
                WordWrap = false
            };

            Controls.Add(_resultsBox);
            Controls.Add(_executeButton);
            Controls.Add(_queryInput);

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.Enter)
                {
                    ExecuteQuery();
                    e.SuppressKeyPress = true;
                }
            };

            // Initialize in-memory database & sample schema/data
            _database = new KuzuDot.Database(":memory:");
            _connection = _database.Connect();
            try
            {
                _connection.Query("CREATE NODE TABLE Person(id STRING, name STRING, age INT64, PRIMARY KEY(id))").Dispose();
                _connection.Query("CREATE (:Person {id:'1', name:'Alice', age:30})").Dispose();
                _connection.Query("CREATE (:Person {id:'2', name:'Bob', age:36})").Dispose();
                _queryInput.Text = "MATCH (p:Person) RETURN p.name, p.age ORDER BY p.age";
            }
            catch (KuzuDot.KuzuException ex)
            {
                _resultsBox.Text = "Initialization error: " + ex.Message;
            }
        }

        private void ExecuteQuery()
        {
            string query = _queryInput.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                _resultsBox.Text = "Enter a query.";
                return;
            }
            try
            {
                using var result = _connection.Query(query);
                var sb = new StringBuilder();
                ulong cols = result.NumColumns;
                for (ulong c = 0; c < cols; c++)
                {
                    if (c > 0) sb.Append(" | ");
                    sb.Append(result.GetColumnName(c));
                }
                sb.AppendLine();
                sb.AppendLine(new string('-', Math.Max(20, (int)cols * 12)));
                while (result.HasNext())
                {
                    using var row = result.GetNext();
                    for (ulong c = 0; c < cols; c++)
                    {
                        if (c > 0) sb.Append(" | ");
                        using var val = row.GetValue(c);
                        sb.Append(val.ToString());
                    }
                    sb.AppendLine();
                }
                _resultsBox.Text = sb.ToString();
            }
            catch (KuzuDot.KuzuException ex)
            {
                _resultsBox.Text = "Error: " + ex.Message;
            }
            catch (Exception ex)
            {
                _resultsBox.Text = "Unexpected error: " + ex.Message;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _connection.Dispose();
            _database.Dispose();
        }
    }
}
