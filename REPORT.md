# Education Portal - Practical Work Report

## 1. Overview
This project implements an educational portal for managing courses, students,
teachers, and course materials. The application is built with C# (WPF) and
PostgreSQL using the Npgsql driver.

## 2. Environment and Tools
- C# (.NET Framework 4.8, WPF)
- PostgreSQL (with pgAdmin)
- Npgsql (PostgreSQL ADO.NET driver)

## 3. Database Setup
1. Create a database named `EduPortal` in PostgreSQL.
2. Run the SQL script:
   - `Database/EduPortal.sql`

Example:
```
CREATE TABLE IF NOT EXISTS Courses (
    course_id SERIAL PRIMARY KEY,
    course_name VARCHAR(100) NOT NULL,
    duration INTEGER,
    teacher_id INTEGER
);
```

## 4. Connection String
Edit `App.config`:
```
Host=localhost;Port=5432;Database=EduPortal;Username=postgres;Password=your_password
```

## 5. GUI Description
Main window includes four tabs:
- Courses
- Students
- Teachers
- Materials

Each tab contains:
- TextBox inputs
- ComboBox selectors
- Buttons for actions (Add/Update/Delete/Clear)
- DataGrid for data display

Additional window:
- Students in Course (JOIN query)

## 6. Button Logic Summary
### Courses
- **Add**: INSERT course into `Courses`
- **Update**: UPDATE selected course
- **Delete**: DELETE selected course
- **Search**: filter courses by name (ILIKE)
- **View students in course**: opens JOIN window

### Students
- **Add/Update/Delete/Clear**: CRUD for `Students`

### Teachers
- **Add/Update/Delete/Clear**: CRUD for `Teachers`

### Materials
- **Choose file**: selects a file to attach
- **Attach**: copies file to `Materials` folder and INSERTs into `CourseMaterials`
- **Open file**: opens stored file via OS shell
- **Delete**: removes record and deletes stored file

## 7. SQL Queries Used (Examples)
- SELECT with JOIN:
```
SELECT s.student_id, s.first_name, s.last_name, s.email, c.course_name
FROM Students s
INNER JOIN Courses c ON s.course_id = c.course_id
WHERE c.course_id = @courseId;
```
- INSERT:
```
INSERT INTO Courses (course_name, duration, teacher_id)
VALUES (@name, @duration, @teacherId);
```
- UPDATE:
```
UPDATE Students
SET first_name = @firstName, last_name = @lastName, email = @email, course_id = @courseId
WHERE student_id = @id;
```
- DELETE:
```
DELETE FROM Teachers WHERE teacher_id = @id;
```

## 8. Additional Features
- Students-in-course window (JOIN)
- Course search by name
- File attachments for courses (materials)

## 9. Testing Checklist
- Add/Edit/Delete Courses
- Add/Edit/Delete Students
- Add/Edit/Delete Teachers
- Search Courses
- View Students in Course
- Attach/Open/Delete Materials

## 10. Screenshot Placeholder
Add a screenshot of the main window after testing.
Save as `screenshot.png` and include in the submission archive.

## 11. How to Run
1. Install Npgsql via NuGet (Manage NuGet Packages).
2. Update connection string in `App.config`.
3. Run the SQL script to create tables.
4. Press F5 in Visual Studio.
