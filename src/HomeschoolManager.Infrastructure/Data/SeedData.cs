using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Infrastructure.Data;

internal static class SeedData
{
    public static HomeschoolData Create()
    {
        return new HomeschoolData
        {
            Students =
            [
                new()
                {
                    Id = 1,
                    FirstName = "Emma",
                    LastName = "Johnson",
                    Email = "emma@example.com",
                    DateOfBirth = new DateTime(2014, 3, 15),
                    GradeLevel = "5th",
                    GPA = 3.8,
                    TotalCredits = 24,
                    EnrollmentDate = new DateTime(2020, 9, 1),
                    CreatedAt = new DateTime(2024, 8, 1)
                },
                new()
                {
                    Id = 2,
                    FirstName = "Liam",
                    LastName = "Smith",
                    Email = "liam@example.com",
                    DateOfBirth = new DateTime(2016, 7, 22),
                    GradeLevel = "3rd",
                    GPA = 3.6,
                    TotalCredits = 18,
                    EnrollmentDate = new DateTime(2021, 9, 1),
                    CreatedAt = new DateTime(2024, 8, 2)
                },
                new()
                {
                    Id = 3,
                    FirstName = "Sophia",
                    LastName = "Davis",
                    Email = "sophia@example.com",
                    DateOfBirth = new DateTime(2012, 11, 8),
                    GradeLevel = "7th",
                    GPA = 3.9,
                    TotalCredits = 30,
                    EnrollmentDate = new DateTime(2019, 9, 1),
                    CreatedAt = new DateTime(2024, 8, 3)
                }
            ],
            Courses =
            [
                new()
                {
                    Id = 1,
                    Name = "Algebra Fundamentals",
                    Description = "Introduction to algebraic concepts and problem-solving strategies.",
                    Subject = "Math",
                    GradeLevel = "8th",
                    StartDate = new DateTime(2024, 9, 1),
                    EndDate = new DateTime(2025, 6, 15),
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 8, 1),
                    LessonPlans =
                    [
                        new() { Id = 1, Title = "Introduction to Variables", Description = "Understanding variables and expressions", Objectives = "Students will identify variables and write simple expressions", Materials = "Whiteboard, markers, worksheets", Activities = "1. Review number concepts\n2. Introduce variables\n3. Practice problems", Assessment = "Worksheet completion", DurationMinutes = 45, WeekNumber = 1, DayNumber = 1, CourseId = 1 },
                        new() { Id = 2, Title = "Solving Linear Equations", Description = "Basic equation solving techniques", Objectives = "Students will solve simple linear equations", Materials = "Calculator, practice sheets", Activities = "1. Review variables\n2. Solve equations step by step\n3. Group practice", Assessment = "Quiz on equation solving", DurationMinutes = 60, WeekNumber = 1, DayNumber = 2, CourseId = 1 }
                    ]
                },
                new()
                {
                    Id = 2,
                    Name = "World History: Ancient Civilizations",
                    Description = "Exploration of ancient civilizations and their impact on modern society.",
                    Subject = "History",
                    GradeLevel = "6th",
                    StartDate = new DateTime(2024, 9, 1),
                    EndDate = new DateTime(2025, 6, 15),
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 8, 2),
                    LessonPlans =
                    [
                        new() { Id = 3, Title = "Introduction to Ancient Egypt", Description = "Overview of Egyptian civilization", Objectives = "Students will understand Egyptian geography and culture", Materials = "Maps, images, videos", Activities = "1. Map exploration\n2. Video presentation\n3. Discussion", Assessment = "Written summary", DurationMinutes = 50, WeekNumber = 1, DayNumber = 1, CourseId = 2 }
                    ]
                },
                new()
                {
                    Id = 3,
                    Name = "Creative Writing Workshop",
                    Description = "Developing writing skills through creative expression and storytelling.",
                    Subject = "Language Arts",
                    GradeLevel = "7th",
                    StartDate = new DateTime(2024, 9, 1),
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 8, 3)
                },
                new()
                {
                    Id = 4,
                    Name = "Biology: Cell Structure",
                    Description = "Study of cell biology and microscopic organisms.",
                    Subject = "Science",
                    GradeLevel = "9th",
                    StartDate = new DateTime(2024, 9, 1),
                    EndDate = new DateTime(2025, 1, 15),
                    IsActive = false,
                    CreatedAt = new DateTime(2024, 7, 1)
                }
            ],
            Assignments =
            [
                new() { Id = 1, StudentId = 1, CourseId = 1, Subject = "Math", Title = "Algebra Worksheet", Description = "Practice simplifying expressions.", DueDate = DateTime.Today.AddDays(2), AssignedDate = DateTime.Today.AddDays(-3), Status = AssignmentStatus.Assigned, CreatedAt = new DateTime(2024, 8, 5) },
                new() { Id = 2, StudentId = 2, CourseId = 4, Subject = "Science", Title = "Lab Report", Description = "Write observations from the cell structure lab.", DueDate = DateTime.Today.AddDays(5), AssignedDate = DateTime.Today.AddDays(-2), Status = AssignmentStatus.InProgress, CreatedAt = new DateTime(2024, 8, 5) },
                new() { Id = 3, StudentId = 3, CourseId = 3, Subject = "Language Arts", Title = "Essay", Description = "Draft a short personal narrative.", DueDate = DateTime.Today.AddDays(1), AssignedDate = DateTime.Today.AddDays(-1), Status = AssignmentStatus.Assigned, CreatedAt = new DateTime(2024, 8, 5) }
            ],
            Grades =
            [
                new() { Id = 1, StudentId = 1, AssignmentId = 1, Subject = "Math", Assignment = "Fractions Test", GradeValue = "A", Score = 96, Date = DateTime.Today.AddDays(-2), GradedDate = DateTime.Today.AddDays(-2), CreatedAt = new DateTime(2024, 8, 10) },
                new() { Id = 2, StudentId = 1, AssignmentId = 1, Subject = "Science", Assignment = "Experiment Report", GradeValue = "A-", Score = 91, Date = DateTime.Today.AddDays(-5), GradedDate = DateTime.Today.AddDays(-5), CreatedAt = new DateTime(2024, 8, 10) },
                new() { Id = 3, StudentId = 2, AssignmentId = 2, Subject = "Language Arts", Assignment = "Book Report", GradeValue = "B+", Score = 88, Date = DateTime.Today.AddDays(-7), GradedDate = DateTime.Today.AddDays(-7), CreatedAt = new DateTime(2024, 8, 10) },
                new() { Id = 4, StudentId = 3, AssignmentId = 3, Subject = "History", Assignment = "Timeline Project", GradeValue = "A", Score = 95, Date = DateTime.Today.AddDays(-10), GradedDate = DateTime.Today.AddDays(-10), CreatedAt = new DateTime(2024, 8, 10) }
            ]
        };
    }
}
