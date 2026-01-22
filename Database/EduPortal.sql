CREATE TABLE IF NOT EXISTS Teachers (
    teacher_id SERIAL PRIMARY KEY,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    subject VARCHAR(150) NOT NULL
);

CREATE TABLE IF NOT EXISTS Courses (
    course_id SERIAL PRIMARY KEY,
    course_name VARCHAR(100) NOT NULL,
    duration INTEGER,
    teacher_id INTEGER REFERENCES Teachers(teacher_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS Students (
    student_id SERIAL PRIMARY KEY,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL,
    course_id INTEGER REFERENCES Courses(course_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS CourseMaterials (
    material_id SERIAL PRIMARY KEY,
    course_id INTEGER NOT NULL REFERENCES Courses(course_id) ON DELETE CASCADE,
    file_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(500) NOT NULL,
    uploaded_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_courses_name ON Courses (course_name);
CREATE INDEX IF NOT EXISTS idx_students_course ON Students (course_id);
CREATE INDEX IF NOT EXISTS idx_materials_course ON CourseMaterials (course_id);
