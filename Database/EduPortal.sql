CREATE TABLE IF NOT EXISTS Teachers (
    teacher_id INTEGER PRIMARY KEY AUTOINCREMENT,
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL,
    subject TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Courses (
    course_id INTEGER PRIMARY KEY AUTOINCREMENT,
    course_name TEXT NOT NULL,
    duration INTEGER,
    teacher_id INTEGER REFERENCES Teachers(teacher_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS Students (
    student_id INTEGER PRIMARY KEY AUTOINCREMENT,
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL,
    email TEXT NOT NULL,
    course_id INTEGER REFERENCES Courses(course_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS CourseMaterials (
    material_id INTEGER PRIMARY KEY AUTOINCREMENT,
    course_id INTEGER NOT NULL REFERENCES Courses(course_id) ON DELETE CASCADE,
    file_name TEXT NOT NULL,
    file_path TEXT NOT NULL,
    uploaded_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_courses_name ON Courses (course_name);
CREATE INDEX IF NOT EXISTS idx_students_course ON Students (course_id);
CREATE INDEX IF NOT EXISTS idx_materials_course ON CourseMaterials (course_id);
