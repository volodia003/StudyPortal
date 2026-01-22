using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Npgsql;

namespace StudyPortal
{
    /// <summary>
    /// Main window logic.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _connectionString;
        private int? _selectedCourseId;
        private int? _selectedStudentId;
        private int? _selectedTeacherId;
        private int? _selectedMaterialId;
        private string _selectedMaterialSourcePath;
        private string _selectedMaterialStoredPath;

        public MainWindow()
        {
            InitializeComponent();
            _connectionString = ResolveConnectionString();
        }

        private string ResolveConnectionString()
        {
            var settings = ConfigurationManager.ConnectionStrings["EduPortal"];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                return "Host=localhost;Port=5432;Database=EduPortal;Username=postgres;Password=your_password";
            }

            return settings.ConnectionString;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_connectionString.Contains("your_password"))
            {
                MessageBox.Show("Update App.config with a real PostgreSQL password before using the app.",
                    "Configuration", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            RefreshAllData();
        }

        private void RefreshAllData()
        {
            try
            {
                LoadTeachers();
                LoadCourses(null);
                LoadCourseLookup();
                LoadStudents();
                LoadMaterials();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTeachers()
        {
            var table = GetDataTable(
                "SELECT teacher_id, first_name, last_name, subject FROM Teachers ORDER BY last_name, first_name");
            TeachersDataGrid.ItemsSource = table.DefaultView;

            var lookup = GetDataTable(
                "SELECT teacher_id, first_name || ' ' || last_name AS teacher_name FROM Teachers ORDER BY last_name, first_name");
            CourseTeacherComboBox.ItemsSource = lookup.DefaultView;
        }

        private void LoadCourses(string searchTerm)
        {
            const string baseSql = @"SELECT c.course_id,
                                            c.course_name,
                                            c.duration,
                                            c.teacher_id,
                                            t.first_name || ' ' || t.last_name AS teacher_name
                                     FROM Courses c
                                     LEFT JOIN Teachers t ON c.teacher_id = t.teacher_id";

            string sql;
            NpgsqlParameter[] parameters = null;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql = baseSql + " WHERE c.course_name ILIKE @search ORDER BY c.course_name";
                parameters = new[] { new NpgsqlParameter("search", $"%{searchTerm.Trim()}%") };
            }
            else
            {
                sql = baseSql + " ORDER BY c.course_name";
            }

            var table = GetDataTable(sql, parameters);
            CoursesDataGrid.ItemsSource = table.DefaultView;
        }

        private void LoadCourseLookup()
        {
            var table = GetDataTable("SELECT course_id, course_name FROM Courses ORDER BY course_name");
            StudentCourseComboBox.ItemsSource = table.DefaultView;
            MaterialCourseComboBox.ItemsSource = table.Copy().DefaultView;
        }

        private void LoadStudents()
        {
            var table = GetDataTable(
                @"SELECT s.student_id,
                         s.first_name,
                         s.last_name,
                         s.email,
                         s.course_id,
                         c.course_name
                  FROM Students s
                  LEFT JOIN Courses c ON s.course_id = c.course_id
                  ORDER BY s.last_name, s.first_name");
            StudentsDataGrid.ItemsSource = table.DefaultView;
        }

        private void LoadMaterials()
        {
            var table = GetDataTable(
                @"SELECT m.material_id,
                         m.course_id,
                         c.course_name,
                         m.file_name,
                         m.file_path,
                         m.uploaded_at
                  FROM CourseMaterials m
                  LEFT JOIN Courses c ON m.course_id = c.course_id
                  ORDER BY m.uploaded_at DESC");
            MaterialsDataGrid.ItemsSource = table.DefaultView;
        }

        private DataTable GetDataTable(string sql, params NpgsqlParameter[] parameters)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            using (var command = new NpgsqlCommand(sql, connection))
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                using (var adapter = new NpgsqlDataAdapter(command))
                {
                    var table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
        }

        private int ExecuteNonQuery(string sql, params NpgsqlParameter[] parameters)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            using (var command = new NpgsqlCommand(sql, connection))
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        private static int? ToNullableInt(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return null;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            return int.TryParse(value.ToString(), out var parsed) ? parsed : (int?)null;
        }

        private static int? GetSelectedComboValue(ComboBox comboBox)
        {
            if (comboBox.SelectedValue == null)
            {
                return null;
            }

            if (comboBox.SelectedValue is int id)
            {
                return id;
            }

            return int.TryParse(comboBox.SelectedValue.ToString(), out var parsed) ? parsed : (int?)null;
        }

        private void SearchCourseButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCourses(CourseSearchTextBox.Text);
        }

        private void ResetCourseSearchButton_Click(object sender, RoutedEventArgs e)
        {
            CourseSearchTextBox.Text = string.Empty;
            LoadCourses(null);
        }

        private void AddCourseButton_Click(object sender, RoutedEventArgs e)
        {
            var name = CourseNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Enter a course name.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DurationTextBox.Text.Trim(), out var duration))
            {
                MessageBox.Show("Enter a numeric duration.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var teacherId = GetSelectedComboValue(CourseTeacherComboBox);
            if (teacherId == null)
            {
                MessageBox.Show("Select a teacher.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExecuteNonQuery(
                "INSERT INTO Courses (course_name, duration, teacher_id) VALUES (@name, @duration, @teacherId)",
                new NpgsqlParameter("name", name),
                new NpgsqlParameter("duration", duration),
                new NpgsqlParameter("teacherId", teacherId));

            LoadCourses(null);
            LoadCourseLookup();
            LoadStudents();
            LoadMaterials();
            ClearCourseForm();
        }

        private void UpdateCourseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCourseId == null)
            {
                MessageBox.Show("Select a course to update.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var name = CourseNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Enter a course name.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DurationTextBox.Text.Trim(), out var duration))
            {
                MessageBox.Show("Enter a numeric duration.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var teacherId = GetSelectedComboValue(CourseTeacherComboBox);
            if (teacherId == null)
            {
                MessageBox.Show("Select a teacher.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExecuteNonQuery(
                @"UPDATE Courses
                  SET course_name = @name,
                      duration = @duration,
                      teacher_id = @teacherId
                  WHERE course_id = @id",
                new NpgsqlParameter("name", name),
                new NpgsqlParameter("duration", duration),
                new NpgsqlParameter("teacherId", teacherId),
                new NpgsqlParameter("id", _selectedCourseId.Value));

            LoadCourses(null);
            LoadCourseLookup();
            LoadStudents();
            LoadMaterials();
            ClearCourseForm();
        }

        private void DeleteCourseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCourseId == null)
            {
                MessageBox.Show("Select a course to delete.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Delete selected course?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            ExecuteNonQuery("DELETE FROM Courses WHERE course_id = @id",
                new NpgsqlParameter("id", _selectedCourseId.Value));

            LoadCourses(null);
            LoadCourseLookup();
            LoadStudents();
            LoadMaterials();
            ClearCourseForm();
        }

        private void ClearCourseFormButton_Click(object sender, RoutedEventArgs e)
        {
            ClearCourseForm();
        }

        private void ClearCourseForm()
        {
            _selectedCourseId = null;
            CourseNameTextBox.Text = string.Empty;
            DurationTextBox.Text = string.Empty;
            CourseTeacherComboBox.SelectedIndex = -1;
            CoursesDataGrid.SelectedItem = null;
        }

        private void CoursesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(CoursesDataGrid.SelectedItem is DataRowView row))
            {
                return;
            }

            _selectedCourseId = ToNullableInt(row["course_id"]);
            CourseNameTextBox.Text = row["course_name"]?.ToString() ?? string.Empty;
            DurationTextBox.Text = row["duration"]?.ToString() ?? string.Empty;
            CourseTeacherComboBox.SelectedValue = ToNullableInt(row["teacher_id"]);
        }

        private void AddStudentButton_Click(object sender, RoutedEventArgs e)
        {
            var firstName = StudentFirstNameTextBox.Text.Trim();
            var lastName = StudentLastNameTextBox.Text.Trim();
            var email = StudentEmailTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("Enter student first and last name.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Enter student email.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var courseId = GetSelectedComboValue(StudentCourseComboBox);
            if (courseId == null)
            {
                MessageBox.Show("Select a course for the student.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExecuteNonQuery(
                @"INSERT INTO Students (first_name, last_name, email, course_id)
                  VALUES (@firstName, @lastName, @email, @courseId)",
                new NpgsqlParameter("firstName", firstName),
                new NpgsqlParameter("lastName", lastName),
                new NpgsqlParameter("email", email),
                new NpgsqlParameter("courseId", courseId));

            LoadStudents();
            ClearStudentForm();
        }

        private void UpdateStudentButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudentId == null)
            {
                MessageBox.Show("Select a student to update.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var firstName = StudentFirstNameTextBox.Text.Trim();
            var lastName = StudentLastNameTextBox.Text.Trim();
            var email = StudentEmailTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("Enter student first and last name.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Enter student email.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var courseId = GetSelectedComboValue(StudentCourseComboBox);
            if (courseId == null)
            {
                MessageBox.Show("Select a course for the student.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExecuteNonQuery(
                @"UPDATE Students
                  SET first_name = @firstName,
                      last_name = @lastName,
                      email = @email,
                      course_id = @courseId
                  WHERE student_id = @id",
                new NpgsqlParameter("firstName", firstName),
                new NpgsqlParameter("lastName", lastName),
                new NpgsqlParameter("email", email),
                new NpgsqlParameter("courseId", courseId),
                new NpgsqlParameter("id", _selectedStudentId.Value));

            LoadStudents();
            ClearStudentForm();
        }

        private void DeleteStudentButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudentId == null)
            {
                MessageBox.Show("Select a student to delete.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Delete selected student?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            ExecuteNonQuery("DELETE FROM Students WHERE student_id = @id",
                new NpgsqlParameter("id", _selectedStudentId.Value));

            LoadStudents();
            ClearStudentForm();
        }

        private void ClearStudentFormButton_Click(object sender, RoutedEventArgs e)
        {
            ClearStudentForm();
        }

        private void ClearStudentForm()
        {
            _selectedStudentId = null;
            StudentFirstNameTextBox.Text = string.Empty;
            StudentLastNameTextBox.Text = string.Empty;
            StudentEmailTextBox.Text = string.Empty;
            StudentCourseComboBox.SelectedIndex = -1;
            StudentsDataGrid.SelectedItem = null;
        }

        private void StudentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(StudentsDataGrid.SelectedItem is DataRowView row))
            {
                return;
            }

            _selectedStudentId = ToNullableInt(row["student_id"]);
            StudentFirstNameTextBox.Text = row["first_name"]?.ToString() ?? string.Empty;
            StudentLastNameTextBox.Text = row["last_name"]?.ToString() ?? string.Empty;
            StudentEmailTextBox.Text = row["email"]?.ToString() ?? string.Empty;
            StudentCourseComboBox.SelectedValue = ToNullableInt(row["course_id"]);
        }

        private void AddTeacherButton_Click(object sender, RoutedEventArgs e)
        {
            var firstName = TeacherFirstNameTextBox.Text.Trim();
            var lastName = TeacherLastNameTextBox.Text.Trim();
            var subject = TeacherSubjectTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("Enter teacher first and last name.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                MessageBox.Show("Enter a subject.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExecuteNonQuery(
                "INSERT INTO Teachers (first_name, last_name, subject) VALUES (@firstName, @lastName, @subject)",
                new NpgsqlParameter("firstName", firstName),
                new NpgsqlParameter("lastName", lastName),
                new NpgsqlParameter("subject", subject));

            LoadTeachers();
            LoadCourses(null);
            ClearTeacherForm();
        }

        private void UpdateTeacherButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTeacherId == null)
            {
                MessageBox.Show("Select a teacher to update.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var firstName = TeacherFirstNameTextBox.Text.Trim();
            var lastName = TeacherLastNameTextBox.Text.Trim();
            var subject = TeacherSubjectTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("Enter teacher first and last name.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                MessageBox.Show("Enter a subject.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExecuteNonQuery(
                @"UPDATE Teachers
                  SET first_name = @firstName,
                      last_name = @lastName,
                      subject = @subject
                  WHERE teacher_id = @id",
                new NpgsqlParameter("firstName", firstName),
                new NpgsqlParameter("lastName", lastName),
                new NpgsqlParameter("subject", subject),
                new NpgsqlParameter("id", _selectedTeacherId.Value));

            LoadTeachers();
            LoadCourses(null);
            ClearTeacherForm();
        }

        private void DeleteTeacherButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTeacherId == null)
            {
                MessageBox.Show("Select a teacher to delete.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Delete selected teacher?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            ExecuteNonQuery("DELETE FROM Teachers WHERE teacher_id = @id",
                new NpgsqlParameter("id", _selectedTeacherId.Value));

            LoadTeachers();
            LoadCourses(null);
            ClearTeacherForm();
        }

        private void ClearTeacherFormButton_Click(object sender, RoutedEventArgs e)
        {
            ClearTeacherForm();
        }

        private void ClearTeacherForm()
        {
            _selectedTeacherId = null;
            TeacherFirstNameTextBox.Text = string.Empty;
            TeacherLastNameTextBox.Text = string.Empty;
            TeacherSubjectTextBox.Text = string.Empty;
            TeachersDataGrid.SelectedItem = null;
        }

        private void TeachersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(TeachersDataGrid.SelectedItem is DataRowView row))
            {
                return;
            }

            _selectedTeacherId = ToNullableInt(row["teacher_id"]);
            TeacherFirstNameTextBox.Text = row["first_name"]?.ToString() ?? string.Empty;
            TeacherLastNameTextBox.Text = row["last_name"]?.ToString() ?? string.Empty;
            TeacherSubjectTextBox.Text = row["subject"]?.ToString() ?? string.Empty;
        }

        private void ChooseMaterialFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select material file",
                Filter = "All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedMaterialSourcePath = dialog.FileName;
                MaterialFilePathTextBox.Text = dialog.FileName;
            }
        }

        private void AttachMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            var courseId = GetSelectedComboValue(MaterialCourseComboBox);
            if (courseId == null)
            {
                MessageBox.Show("Select a course for the material.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedMaterialSourcePath) || !File.Exists(_selectedMaterialSourcePath))
            {
                MessageBox.Show("Choose a file to attach.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var materialsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Materials");
            Directory.CreateDirectory(materialsRoot);

            var originalFileName = Path.GetFileName(_selectedMaterialSourcePath);
            var storedFileName = $"{Guid.NewGuid():N}_{originalFileName}";
            var relativePath = Path.Combine("Materials", storedFileName);
            var storedFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

            File.Copy(_selectedMaterialSourcePath, storedFullPath, true);

            ExecuteNonQuery(
                @"INSERT INTO CourseMaterials (course_id, file_name, file_path)
                  VALUES (@courseId, @fileName, @filePath)",
                new NpgsqlParameter("courseId", courseId),
                new NpgsqlParameter("fileName", originalFileName),
                new NpgsqlParameter("filePath", relativePath));

            LoadMaterials();
            ClearMaterialForm();
        }

        private void DeleteMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaterialId == null)
            {
                MessageBox.Show("Select a material to delete.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Delete selected material?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            ExecuteNonQuery("DELETE FROM CourseMaterials WHERE material_id = @id",
                new NpgsqlParameter("id", _selectedMaterialId.Value));

            TryDeleteStoredMaterial();
            LoadMaterials();
            ClearMaterialForm();
        }

        private void ClearMaterialFormButton_Click(object sender, RoutedEventArgs e)
        {
            ClearMaterialForm();
        }

        private void ClearMaterialForm()
        {
            _selectedMaterialId = null;
            _selectedMaterialSourcePath = null;
            _selectedMaterialStoredPath = null;
            MaterialFilePathTextBox.Text = string.Empty;
            MaterialCourseComboBox.SelectedIndex = -1;
            MaterialsDataGrid.SelectedItem = null;
        }

        private void MaterialsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(MaterialsDataGrid.SelectedItem is DataRowView row))
            {
                return;
            }

            _selectedMaterialId = ToNullableInt(row["material_id"]);
            MaterialCourseComboBox.SelectedValue = ToNullableInt(row["course_id"]);
            _selectedMaterialStoredPath = row["file_path"]?.ToString();
            MaterialFilePathTextBox.Text = _selectedMaterialStoredPath ?? string.Empty;
        }

        private void OpenMaterialFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedMaterialStoredPath))
            {
                MessageBox.Show("Select a material to open.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fullPath = Path.IsPathRooted(_selectedMaterialStoredPath)
                ? _selectedMaterialStoredPath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _selectedMaterialStoredPath);

            if (!File.Exists(fullPath))
            {
                MessageBox.Show("Stored file not found on disk.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
        }

        private void TryDeleteStoredMaterial()
        {
            if (string.IsNullOrWhiteSpace(_selectedMaterialStoredPath))
            {
                return;
            }

            var fullPath = Path.IsPathRooted(_selectedMaterialStoredPath)
                ? _selectedMaterialStoredPath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _selectedMaterialStoredPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        private void OpenCourseStudentsWindow_Click(object sender, RoutedEventArgs e)
        {
            var window = new CourseStudentsWindow(_connectionString)
            {
                Owner = this
            };
            window.ShowDialog();
        }
    }
}
