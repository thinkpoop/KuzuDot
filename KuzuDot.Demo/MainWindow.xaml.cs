using System;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace KuzuDot.Demo
{
    public partial class MainWindow : Window
    {
        private Database? _database;
        private Connection? _connection;
        private bool _initialized;

        public MainWindow()
        {
            InitializeComponent();
            // Defer potentially failing work until window is created so startup exceptions do not prevent the window from showing.
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (_initialized) return;
            try
            {
                _database = new Database(":memory:");
                _connection = _database.Connect();
                Seed();
                QueryInput.Text = "MATCH (p:Person) RETURN p.name, p.age ORDER BY p.age";
                QueryInput.Focus();
                _initialized = true;
            }
            catch (KuzuException ex)
            {
                ResultsBox.Text = "Initialization error: " + ex.Message;
            }
            catch (Exception ex)
            {
                // Catch any unexpected exception so WPF doesn't wrap it and abort window creation.
                ResultsBox.Text = "Unexpected initialization error: " + ex.Message;
            }
        }

        private void Seed()
        {
            if (_connection == null) return;
            _connection.Query("CREATE NODE TABLE Person(id STRING, name STRING, age INT64, PRIMARY KEY(id))").Dispose();
            _connection.Query("CREATE (:Person {id:'1', name:'Alice', age:30})").Dispose();
            _connection.Query("CREATE (:Person {id:'2', name:'Bob', age:36})").Dispose();
        }

        private void Execute()
        {
            if (_connection == null) { ResultsBox.Text = "Not initialized."; return; }
            var query = QueryInput.Text.Trim();
            if (string.IsNullOrEmpty(query)) { ResultsBox.Text = "Enter a query."; return; }
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
                ResultsBox.Text = sb.ToString();
            }
            catch (KuzuException ex)
            {
                ResultsBox.Text = "Error: " + ex.Message;
            }
            catch (Exception ex)
            {
                ResultsBox.Text = "Unexpected error: " + ex.Message;
            }
        }

        private void ExecuteButton_OnClick(object sender, RoutedEventArgs e) => Execute();

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Execute();
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _connection?.Dispose();
            _database?.Dispose();
        }
    }
}
