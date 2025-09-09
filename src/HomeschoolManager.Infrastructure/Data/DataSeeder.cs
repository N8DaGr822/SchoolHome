using Microsoft.EntityFrameworkCore;
using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(HomeschoolDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await context.Students.AnyAsync())
        {
            return; // Data already seeded
        }

        // Seed comprehensive sample students
        var students = new List<Student>
        {
            new Student
            {
                FirstName = "Emma",
                LastName = "Johnson",
                Email = "emma.johnson@homeschool.com",
                DateOfBirth = new DateTime(2010, 5, 15),
                GradeLevel = "5th",
                GPA = 3.8,
                TotalCredits = 12,
                EnrollmentDate = new DateTime(2024, 8, 15),
                CreatedAt = DateTime.UtcNow
            },
            new Student
            {
                FirstName = "Liam",
                LastName = "Smith",
                Email = "liam.smith@homeschool.com",
                DateOfBirth = new DateTime(2009, 8, 22),
                GradeLevel = "6th",
                GPA = 3.6,
                TotalCredits = 14,
                EnrollmentDate = new DateTime(2024, 8, 15),
                CreatedAt = DateTime.UtcNow
            },
            new Student
            {
                FirstName = "Sophia",
                LastName = "Brown",
                Email = "sophia.brown@homeschool.com",
                DateOfBirth = new DateTime(2011, 3, 10),
                GradeLevel = "4th",
                GPA = 3.9,
                TotalCredits = 10,
                EnrollmentDate = new DateTime(2024, 8, 15),
                CreatedAt = DateTime.UtcNow
            },
            new Student
            {
                FirstName = "Noah",
                LastName = "Davis",
                Email = "noah.davis@homeschool.com",
                DateOfBirth = new DateTime(2012, 11, 8),
                GradeLevel = "3rd",
                GPA = 3.7,
                TotalCredits = 8,
                EnrollmentDate = new DateTime(2024, 8, 15),
                CreatedAt = DateTime.UtcNow
            },
            new Student
            {
                FirstName = "Olivia",
                LastName = "Wilson",
                Email = "olivia.wilson@homeschool.com",
                DateOfBirth = new DateTime(2008, 2, 14),
                GradeLevel = "7th",
                GPA = 3.9,
                TotalCredits = 16,
                EnrollmentDate = new DateTime(2024, 8, 15),
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Students.AddRangeAsync(students);

        // Seed comprehensive sample courses
        var courses = new List<Course>
        {
            new Course
            {
                Name = "Algebra Fundamentals",
                Description = "Introduction to algebraic concepts and problem-solving strategies.",
                Subject = "Math",
                GradeLevel = "8th",
                StartDate = new DateTime(2024, 9, 1),
                EndDate = new DateTime(2025, 6, 15),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Course
            {
                Name = "English 101",
                Description = "Fundamental English language skills and literature appreciation.",
                Subject = "Language Arts",
                GradeLevel = "8th",
                StartDate = new DateTime(2024, 9, 1),
                EndDate = new DateTime(2025, 6, 15),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Course
            {
                Name = "World History: Ancient Civilizations",
                Description = "Exploration of ancient civilizations and their impact on modern society.",
                Subject = "History",
                GradeLevel = "6th",
                StartDate = new DateTime(2024, 9, 1),
                EndDate = new DateTime(2025, 6, 15),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Course
            {
                Name = "Art Fundamentals",
                Description = "Introduction to various art forms and creative expression.",
                Subject = "Art",
                GradeLevel = "8th",
                StartDate = new DateTime(2024, 9, 1),
                EndDate = new DateTime(2025, 6, 15),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Course
            {
                Name = "Biology: Cell Structure",
                Description = "Study of cell biology and microscopic organisms.",
                Subject = "Science",
                GradeLevel = "9th",
                StartDate = new DateTime(2024, 9, 1),
                EndDate = new DateTime(2025, 1, 15),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Course
            {
                Name = "Mathematics 5",
                Description = "Fifth grade mathematics covering fractions, decimals, and basic algebra",
                Subject = "Math",
                GradeLevel = "5th",
                StartDate = new DateTime(2024, 8, 15),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Course
            {
                Name = "English Language Arts 5",
                Description = "Reading comprehension, writing, and grammar for fifth grade",
                Subject = "Language Arts",
                GradeLevel = "5th",
                StartDate = new DateTime(2024, 8, 15),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Course
            {
                Name = "Science 5",
                Description = "Earth science, life science, and physical science concepts",
                Subject = "Science",
                GradeLevel = "5th",
                StartDate = new DateTime(2024, 8, 15),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Courses.AddRangeAsync(courses);

        // Save changes to get IDs
        await context.SaveChangesAsync();

        // Create comprehensive sample assignments
        var assignments = new List<Assignment>
        {
            new Assignment
            {
                Title = "Math Worksheet - Fractions",
                Description = "Complete the fraction addition and subtraction worksheet",
                DueDate = DateTime.UtcNow.AddDays(3),
                AssignedDate = DateTime.UtcNow,
                Status = AssignmentStatus.Assigned,
                CourseId = courses[5].Id, // Math 5
                StudentId = students[0].Id, // Emma
                Subject = "Math",
                Grade = "",
                StudentName = "Emma Johnson",
                CreatedAt = DateTime.UtcNow
            },
            new Assignment
            {
                Title = "Book Report - Charlotte's Web",
                Description = "Write a 2-page book report on Charlotte's Web",
                DueDate = DateTime.UtcNow.AddDays(7),
                AssignedDate = DateTime.UtcNow,
                Status = AssignmentStatus.InProgress,
                CourseId = courses[6].Id, // English 5
                StudentId = students[0].Id, // Emma
                Subject = "Language Arts",
                Grade = "",
                StudentName = "Emma Johnson",
                CreatedAt = DateTime.UtcNow
            },
            new Assignment
            {
                Title = "Science Experiment Report",
                Description = "Write a report about the plant growth experiment",
                DueDate = DateTime.UtcNow.AddDays(1),
                AssignedDate = DateTime.UtcNow.AddDays(-3),
                Status = AssignmentStatus.InProgress,
                CourseId = courses[7].Id, // Science 5
                StudentId = students[1].Id, // Liam
                Subject = "Science",
                Grade = "",
                StudentName = "Liam Smith",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new Assignment
            {
                Title = "History Timeline Project",
                Description = "Create a timeline of major events in American history",
                DueDate = DateTime.UtcNow.AddDays(5),
                AssignedDate = DateTime.UtcNow.AddDays(-2),
                Status = AssignmentStatus.Assigned,
                CourseId = courses[2].Id, // World History
                StudentId = students[2].Id, // Sophia
                Subject = "History",
                Grade = "",
                StudentName = "Sophia Brown",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Assignment
            {
                Title = "Art Portfolio - Watercolors",
                Description = "Create a portfolio of watercolor paintings",
                DueDate = DateTime.UtcNow.AddDays(10),
                AssignedDate = DateTime.UtcNow.AddDays(-1),
                Status = AssignmentStatus.Assigned,
                CourseId = courses[3].Id, // Art
                StudentId = students[4].Id, // Olivia
                Subject = "Art",
                Grade = "",
                StudentName = "Olivia Wilson",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Assignment
            {
                Title = "Cell Structure Lab Report",
                Description = "Analyze and document cell structure observations",
                DueDate = DateTime.UtcNow.AddDays(-2),
                AssignedDate = DateTime.UtcNow.AddDays(-7),
                Status = AssignmentStatus.Completed,
                CourseId = courses[4].Id, // Biology
                StudentId = students[4].Id, // Olivia
                Subject = "Science",
                Grade = "A",
                StudentName = "Olivia Wilson",
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            }
        };

        await context.Assignments.AddRangeAsync(assignments);

        // Create comprehensive sample lesson plans
        var lessonPlans = new List<LessonPlan>
        {
            new LessonPlan
            {
                Title = "Introduction to Variables",
                Description = "Understanding variables and expressions",
                Objectives = "Students will identify variables and write simple expressions",
                Materials = "Whiteboard, markers, worksheets",
                Activities = "1. Review number concepts\n2. Introduce variables\n3. Practice problems",
                Assessment = "Worksheet completion",
                DurationMinutes = 45,
                WeekNumber = 1,
                DayNumber = 1,
                CourseId = courses[0].Id, // Algebra
                CreatedAt = DateTime.UtcNow
            },
            new LessonPlan
            {
                Title = "Solving Linear Equations",
                Description = "Basic equation solving techniques",
                Objectives = "Students will solve simple linear equations",
                Materials = "Calculator, practice sheets",
                Activities = "1. Review variables\n2. Solve equations step by step\n3. Group practice",
                Assessment = "Quiz on equation solving",
                DurationMinutes = 60,
                WeekNumber = 1,
                DayNumber = 2,
                CourseId = courses[0].Id, // Algebra
                CreatedAt = DateTime.UtcNow
            },
            new LessonPlan
            {
                Title = "Introduction to Ancient Egypt",
                Description = "Overview of Egyptian civilization",
                Objectives = "Students will understand Egyptian geography and culture",
                Materials = "Maps, images, videos",
                Activities = "1. Map exploration\n2. Video presentation\n3. Discussion",
                Assessment = "Written summary",
                DurationMinutes = 50,
                WeekNumber = 1,
                DayNumber = 1,
                CourseId = courses[2].Id, // World History
                CreatedAt = DateTime.UtcNow
            },
            new LessonPlan
            {
                Title = "Reading Comprehension Strategies",
                Description = "Techniques for understanding complex texts",
                Objectives = "Students will apply reading strategies to improve comprehension",
                Materials = "Reading passages, highlighters, notebooks",
                Activities = "1. Strategy introduction\n2. Guided practice\n3. Independent reading",
                Assessment = "Comprehension quiz",
                DurationMinutes = 50,
                WeekNumber = 1,
                DayNumber = 1,
                CourseId = courses[1].Id, // English 101
                CreatedAt = DateTime.UtcNow
            },
            new LessonPlan
            {
                Title = "Color Theory Basics",
                Description = "Understanding primary, secondary, and tertiary colors",
                Objectives = "Students will identify and mix colors effectively",
                Materials = "Paint, brushes, color wheel, paper",
                Activities = "1. Color wheel demonstration\n2. Color mixing practice\n3. Painting exercise",
                Assessment = "Color mixing worksheet",
                DurationMinutes = 60,
                WeekNumber = 1,
                DayNumber = 1,
                CourseId = courses[3].Id, // Art
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.LessonPlans.AddRangeAsync(lessonPlans);

        // Create sample grades
        var grades = new List<Grade>
        {
            new Grade
            {
                StudentId = students[0].Id, // Emma
                AssignmentId = assignments[0].Id, // Math Worksheet
                Score = 95.0m,
                GradeValue = "A",
                Subject = "Math",
                Assignment = "Math Worksheet - Fractions",
                Comments = "Excellent work on fraction operations",
                Date = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Grade
            {
                StudentId = students[4].Id, // Olivia
                AssignmentId = assignments[5].Id, // Cell Structure Lab
                Score = 88.0m,
                GradeValue = "B+",
                Subject = "Science",
                Assignment = "Cell Structure Lab Report",
                Comments = "Good observations, could improve on analysis",
                Date = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Grade
            {
                StudentId = students[1].Id, // Liam
                AssignmentId = assignments[2].Id, // Science Experiment
                Score = 92.0m,
                GradeValue = "A-",
                Subject = "Science",
                Assignment = "Science Experiment Report",
                Comments = "Well-structured report with clear conclusions",
                Date = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        await context.Grades.AddRangeAsync(grades);

        // Create student-course relationships
        var studentCourses = new[]
        {
            new { StudentId = students[0].Id, CourseId = courses[5].Id }, // Emma - Math 5
            new { StudentId = students[0].Id, CourseId = courses[6].Id }, // Emma - English 5
            new { StudentId = students[0].Id, CourseId = courses[7].Id }, // Emma - Science 5
            new { StudentId = students[1].Id, CourseId = courses[5].Id }, // Liam - Math 5
            new { StudentId = students[1].Id, CourseId = courses[7].Id }, // Liam - Science 5
            new { StudentId = students[2].Id, CourseId = courses[2].Id }, // Sophia - World History
            new { StudentId = students[4].Id, CourseId = courses[0].Id }, // Olivia - Algebra
            new { StudentId = students[4].Id, CourseId = courses[1].Id }, // Olivia - English 101
            new { StudentId = students[4].Id, CourseId = courses[3].Id }, // Olivia - Art
            new { StudentId = students[4].Id, CourseId = courses[4].Id }  // Olivia - Biology
        };

        // Add student-course relationships using raw SQL since we're using a many-to-many relationship
        foreach (var sc in studentCourses)
        {
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO StudentCourses (StudentsId, CoursesId) VALUES ({0}, {1})",
                sc.StudentId, sc.CourseId);
        }

        await context.SaveChangesAsync();

        Console.WriteLine("Comprehensive sample data seeded successfully!");
        Console.WriteLine($"Seeded {students.Count} students, {courses.Count} courses, {assignments.Count} assignments, {lessonPlans.Count} lesson plans, and {grades.Count} grades.");
    }
}
