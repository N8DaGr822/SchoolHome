using HomeschoolManager.Core.Entities;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

internal static class RepositoryProjection
{
    public static Student HydrateStudent(HomeschoolData data, Student source)
    {
        var student = HomeschoolDataStore.Clone(source);
        var assignments = data.Assignments
            .Where(a => a.StudentId == student.Id)
            .Select(a => HydrateAssignment(data, a, includeStudent: false))
            .ToList();
        var courseIds = assignments.Select(a => a.CourseId).Distinct().ToHashSet();

        student.Assignments = assignments;
        student.Grades = data.Grades
            .Where(g => g.StudentId == student.Id)
            .Select(HomeschoolDataStore.Clone)
            .ToList();
        student.AttendanceRecords = data.AttendanceRecords
            .Where(a => a.StudentId == student.Id)
            .OrderByDescending(a => a.Date)
            .Select(a => HydrateAttendanceRecord(data, a, includeStudent: false))
            .ToList();
        student.LearningTimeEntries = data.LearningTimeEntries
            .Where(e => e.StudentId == student.Id)
            .OrderByDescending(e => e.Date)
            .Select(e => HydrateLearningTimeEntry(data, e, includeStudent: false))
            .ToList();
        student.Courses = data.Courses
            .Where(c => courseIds.Contains(c.Id))
            .Select(c => HydrateCourse(data, c, includeAssignments: false))
            .ToList();

        return student;
    }

    public static Course HydrateCourse(HomeschoolData data, Course source, bool includeAssignments = true)
    {
        var course = HomeschoolDataStore.Clone(source);
        course.LessonPlans = course.LessonPlans.OrderBy(lp => lp.WeekNumber).ThenBy(lp => lp.DayNumber).ToList();

        if (!includeAssignments)
        {
            course.Assignments = new List<Assignment>();
            course.Students = new List<Student>();
            return course;
        }

        var assignments = data.Assignments
            .Where(a => a.CourseId == course.Id)
            .Select(a => HydrateAssignment(data, a, includeCourse: false))
            .ToList();
        var studentIds = assignments.Select(a => a.StudentId).Distinct().ToHashSet();

        course.Assignments = assignments;
        course.Students = data.Students
            .Where(s => studentIds.Contains(s.Id))
            .Select(s =>
            {
                var student = HomeschoolDataStore.Clone(s);
                student.Assignments = new List<Assignment>();
                student.Grades = new List<Grade>();
                student.Courses = new List<Course>();
                student.AttendanceRecords = new List<AttendanceRecord>();
                student.LearningTimeEntries = new List<LearningTimeEntry>();
                return student;
            })
            .ToList();

        return course;
    }

    public static Assignment HydrateAssignment(
        HomeschoolData data,
        Assignment source,
        bool includeStudent = true,
        bool includeCourse = true)
    {
        var assignment = HomeschoolDataStore.Clone(source);
        var course = data.Courses.FirstOrDefault(c => c.Id == assignment.CourseId);
        var student = data.Students.FirstOrDefault(s => s.Id == assignment.StudentId);

        if (string.IsNullOrWhiteSpace(assignment.Subject) && course != null)
        {
            assignment.Subject = course.Subject;
        }

        if (includeCourse && course != null)
        {
            assignment.Course = HydrateCourse(data, course, includeAssignments: false);
        }

        if (includeStudent && student != null)
        {
            assignment.Student = HomeschoolDataStore.Clone(student);
            assignment.Student.Assignments = new List<Assignment>();
            assignment.Student.Grades = new List<Grade>();
            assignment.Student.Courses = new List<Course>();
            assignment.Student.AttendanceRecords = new List<AttendanceRecord>();
        }

        assignment.Grades = data.Grades
            .Where(g => g.AssignmentId == assignment.Id)
            .Select(HomeschoolDataStore.Clone)
            .ToList();

        return assignment;
    }

    public static AttendanceRecord HydrateAttendanceRecord(
        HomeschoolData data,
        AttendanceRecord source,
        bool includeStudent = true)
    {
        var attendanceRecord = HomeschoolDataStore.Clone(source);
        var student = data.Students.FirstOrDefault(s => s.Id == attendanceRecord.StudentId);

        if (includeStudent && student != null)
        {
            attendanceRecord.Student = HomeschoolDataStore.Clone(student);
            attendanceRecord.Student.Assignments = new List<Assignment>();
            attendanceRecord.Student.Grades = new List<Grade>();
            attendanceRecord.Student.Courses = new List<Course>();
            attendanceRecord.Student.AttendanceRecords = new List<AttendanceRecord>();
            attendanceRecord.Student.LearningTimeEntries = new List<LearningTimeEntry>();
        }

        return attendanceRecord;
    }

    public static LearningTimeEntry HydrateLearningTimeEntry(
        HomeschoolData data,
        LearningTimeEntry source,
        bool includeStudent = true,
        bool includeCourse = true)
    {
        var learningTimeEntry = HomeschoolDataStore.Clone(source);
        var student = data.Students.FirstOrDefault(s => s.Id == learningTimeEntry.StudentId);
        var course = data.Courses.FirstOrDefault(c => c.Id == learningTimeEntry.SubjectId);

        if (string.IsNullOrWhiteSpace(learningTimeEntry.Subject) && course != null)
        {
            learningTimeEntry.Subject = string.IsNullOrWhiteSpace(course.Subject) ? course.Name : course.Subject;
        }

        if (includeStudent && student != null)
        {
            learningTimeEntry.Student = HomeschoolDataStore.Clone(student);
            learningTimeEntry.Student.Assignments = new List<Assignment>();
            learningTimeEntry.Student.Grades = new List<Grade>();
            learningTimeEntry.Student.Courses = new List<Course>();
            learningTimeEntry.Student.AttendanceRecords = new List<AttendanceRecord>();
            learningTimeEntry.Student.LearningTimeEntries = new List<LearningTimeEntry>();
        }

        if (includeCourse && course != null)
        {
            learningTimeEntry.Course = HydrateCourse(data, course, includeAssignments: false);
        }

        return learningTimeEntry;
    }
}
