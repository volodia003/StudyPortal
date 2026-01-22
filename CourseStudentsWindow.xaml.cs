using System;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Data.SQLite;

namespace StudyPortal
{
    /// <summary>
    /// Логика окна студентов курса.
    /// </summary>
    public partial class CourseStudentsWindow : Window
    {
        private readonly string _connectionString;

        public CourseStudentsWindow(string connectionString)
        {
            InitializeComponent();
            _connectionString = string.IsNullOrWhiteSpace(connectionString)
                ? ResolveConnectionString()
                : connectionString;
        }

        private static string ResolveConnectionString()
        {
            var settings = ConfigurationManager.ConnectionStrings["EduPortal"];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                return "Data Source=EduPortal.db;Version=3;Foreign Keys=True;";
            }

            return settings.ConnectionString;
        }

        private void CourseStudentsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadCourses();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить курсы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCourses()
        {
            var table = GetDataTable("SELECT course_id, course_name FROM Courses ORDER BY course_name");
            CourseFilterComboBox.ItemsSource = table.DefaultView;

            if (CourseFilterComboBox.Items.Count > 0)
            {
                CourseFilterComboBox.SelectedIndex = 0;
            }
            else
            {
                CourseStudentsDataGrid.ItemsSource = null;
            }
        }

        private void CourseFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(CourseFilterComboBox.SelectedValue is int courseId))
            {
                return;
            }

            LoadStudentsForCourse(courseId);
        }

        private void LoadStudentsForCourse(int courseId)
        {
            var table = GetDataTable(
                @"SELECT s.student_id,
                         s.first_name,
                         s.last_name,
                         s.email,
                         c.course_name
                  FROM Students s
                  INNER JOIN Courses c ON s.course_id = c.course_id
                  WHERE c.course_id = @courseId
                  ORDER BY s.last_name, s.first_name",
                new SQLiteParameter("courseId", courseId));
            CourseStudentsDataGrid.ItemsSource = table.DefaultView;
        }

        private DataTable GetDataTable(string sql, params SQLiteParameter[] parameters)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            using (var command = new SQLiteCommand(sql, connection))
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                using (var adapter = new SQLiteDataAdapter(command))
                {
                    var table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
        }
    }
}
