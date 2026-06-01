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
        student.PortfolioItems = data.PortfolioItems
            .Where(i => i.StudentId == student.Id)
            .OrderByDescending(i => i.Date)
            .Select(i => HydratePortfolioItem(data, i, includeStudent: false))
            .ToList();
        student.StudentCurricula = data.StudentCurricula
            .Where(c => c.StudentId == student.Id)
            .OrderBy(c => c.CurriculumResource?.Title ?? string.Empty)
            .Select(c => HydrateStudentCurriculum(data, c, includeStudent: false))
            .ToList();
        student.ParentNotes = data.ParentNotes
            .Where(n => n.StudentId == student.Id)
            .OrderByDescending(n => n.NoteDate)
            .Select(n => HydrateParentNote(data, n, includeStudent: false))
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
                student.PortfolioItems = new List<PortfolioItem>();
                student.StudentCurricula = new List<StudentCurriculum>();
                student.ParentNotes = new List<ParentNote>();
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
            assignment.Student.LearningTimeEntries = new List<LearningTimeEntry>();
            assignment.Student.PortfolioItems = new List<PortfolioItem>();
            assignment.Student.StudentCurricula = new List<StudentCurriculum>();
            assignment.Student.ParentNotes = new List<ParentNote>();
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
            attendanceRecord.Student.PortfolioItems = new List<PortfolioItem>();
            attendanceRecord.Student.StudentCurricula = new List<StudentCurriculum>();
            attendanceRecord.Student.ParentNotes = new List<ParentNote>();
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
            learningTimeEntry.Student.PortfolioItems = new List<PortfolioItem>();
            learningTimeEntry.Student.StudentCurricula = new List<StudentCurriculum>();
            learningTimeEntry.Student.ParentNotes = new List<ParentNote>();
        }

        if (includeCourse && course != null)
        {
            learningTimeEntry.Course = HydrateCourse(data, course, includeAssignments: false);
        }

        return learningTimeEntry;
    }

    public static PortfolioItem HydratePortfolioItem(
        HomeschoolData data,
        PortfolioItem source,
        bool includeStudent = true,
        bool includeCourse = true)
    {
        var portfolioItem = HomeschoolDataStore.Clone(source);
        var student = data.Students.FirstOrDefault(s => s.Id == portfolioItem.StudentId);
        var course = data.Courses.FirstOrDefault(c => c.Id == portfolioItem.SubjectId);

        if (string.IsNullOrWhiteSpace(portfolioItem.Subject) && course != null)
        {
            portfolioItem.Subject = string.IsNullOrWhiteSpace(course.Subject) ? course.Name : course.Subject;
        }

        if (includeStudent && student != null)
        {
            portfolioItem.Student = HomeschoolDataStore.Clone(student);
            portfolioItem.Student.Assignments = new List<Assignment>();
            portfolioItem.Student.Grades = new List<Grade>();
            portfolioItem.Student.Courses = new List<Course>();
            portfolioItem.Student.AttendanceRecords = new List<AttendanceRecord>();
            portfolioItem.Student.LearningTimeEntries = new List<LearningTimeEntry>();
            portfolioItem.Student.PortfolioItems = new List<PortfolioItem>();
            portfolioItem.Student.StudentCurricula = new List<StudentCurriculum>();
            portfolioItem.Student.ParentNotes = new List<ParentNote>();
        }

        if (includeCourse && course != null)
        {
            portfolioItem.Course = HydrateCourse(data, course, includeAssignments: false);
        }

        portfolioItem.Assignment = portfolioItem.AssignmentId.HasValue
            ? data.Assignments.FirstOrDefault(a => a.Id == portfolioItem.AssignmentId.Value)
            : null;
        portfolioItem.LessonPlan = portfolioItem.LessonPlanId.HasValue
            ? data.LessonPlans.FirstOrDefault(lp => lp.Id == portfolioItem.LessonPlanId.Value)
            : null;

        return portfolioItem;
    }

    public static CurriculumResource HydrateCurriculumResource(
        HomeschoolData data,
        CurriculumResource source,
        bool includeCourse = true,
        bool includeStudentCurricula = true)
    {
        var resource = HomeschoolDataStore.Clone(source);
        var course = data.Courses.FirstOrDefault(c => c.Id == resource.SubjectId);

        if (string.IsNullOrWhiteSpace(resource.Subject) && course != null)
        {
            resource.Subject = string.IsNullOrWhiteSpace(course.Subject) ? course.Name : course.Subject;
        }

        if (includeCourse && course != null)
        {
            resource.Course = HydrateCourse(data, course, includeAssignments: false);
        }

        resource.StudentCurricula = includeStudentCurricula
            ? data.StudentCurricula
                .Where(c => c.CurriculumResourceId == resource.Id)
                .Select(c => HydrateStudentCurriculum(data, c, includeResource: false))
                .ToList()
            : new List<StudentCurriculum>();

        return resource;
    }

    public static StudentCurriculum HydrateStudentCurriculum(
        HomeschoolData data,
        StudentCurriculum source,
        bool includeStudent = true,
        bool includeResource = true)
    {
        var studentCurriculum = HomeschoolDataStore.Clone(source);
        var student = data.Students.FirstOrDefault(s => s.Id == studentCurriculum.StudentId);
        var resource = data.CurriculumResources.FirstOrDefault(r => r.Id == studentCurriculum.CurriculumResourceId);

        if (includeStudent && student != null)
        {
            studentCurriculum.Student = HomeschoolDataStore.Clone(student);
            studentCurriculum.Student.Assignments = new List<Assignment>();
            studentCurriculum.Student.Grades = new List<Grade>();
            studentCurriculum.Student.Courses = new List<Course>();
            studentCurriculum.Student.AttendanceRecords = new List<AttendanceRecord>();
            studentCurriculum.Student.LearningTimeEntries = new List<LearningTimeEntry>();
            studentCurriculum.Student.PortfolioItems = new List<PortfolioItem>();
            studentCurriculum.Student.StudentCurricula = new List<StudentCurriculum>();
            studentCurriculum.Student.ParentNotes = new List<ParentNote>();
        }

        if (includeResource && resource != null)
        {
            studentCurriculum.CurriculumResource = HydrateCurriculumResource(data, resource, includeStudentCurricula: false);
        }

        return studentCurriculum;
    }

    public static ParentNote HydrateParentNote(
        HomeschoolData data,
        ParentNote source,
        bool includeStudent = true)
    {
        var note = HomeschoolDataStore.Clone(source);
        var student = data.Students.FirstOrDefault(s => s.Id == note.StudentId);

        if (includeStudent && student != null)
        {
            note.Student = HomeschoolDataStore.Clone(student);
            note.Student.Assignments = new List<Assignment>();
            note.Student.Grades = new List<Grade>();
            note.Student.Courses = new List<Course>();
            note.Student.AttendanceRecords = new List<AttendanceRecord>();
            note.Student.LearningTimeEntries = new List<LearningTimeEntry>();
            note.Student.PortfolioItems = new List<PortfolioItem>();
            note.Student.StudentCurricula = new List<StudentCurriculum>();
            note.Student.ParentNotes = new List<ParentNote>();
        }

        note.Course = note.SubjectId.HasValue
            ? data.Courses.FirstOrDefault(c => c.Id == note.SubjectId.Value)
            : null;
        note.Assignment = note.AssignmentId.HasValue
            ? data.Assignments.FirstOrDefault(a => a.Id == note.AssignmentId.Value)
            : null;
        note.LessonPlan = note.LessonPlanId.HasValue
            ? data.LessonPlans.FirstOrDefault(lp => lp.Id == note.LessonPlanId.Value)
            : null;

        return note;
    }

    public static Yearbook HydrateYearbook(
        HomeschoolData data,
        Yearbook source,
        bool includePages = true,
        bool includeAssets = true)
    {
        var yearbook = HomeschoolDataStore.Clone(source);

        yearbook.Student = yearbook.StudentId.HasValue
            ? data.Students.FirstOrDefault(s => s.Id == yearbook.StudentId.Value)
            : null;
        if (yearbook.Student is not null)
        {
            yearbook.Student.Assignments = new List<Assignment>();
            yearbook.Student.Grades = new List<Grade>();
            yearbook.Student.Courses = new List<Course>();
            yearbook.Student.AttendanceRecords = new List<AttendanceRecord>();
            yearbook.Student.LearningTimeEntries = new List<LearningTimeEntry>();
            yearbook.Student.PortfolioItems = new List<PortfolioItem>();
            yearbook.Student.StudentCurricula = new List<StudentCurriculum>();
            yearbook.Student.ParentNotes = new List<ParentNote>();
        }

        yearbook.Pages = includePages
            ? data.YearbookPages
                .Where(p => p.YearbookId == yearbook.Id)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Id)
                .Select(p => HydrateYearbookPage(data, p, includeYearbook: false))
                .ToList()
            : new List<YearbookPage>();
        yearbook.Assets = includeAssets
            ? data.YearbookAssets
                .Where(a => a.YearbookId == yearbook.Id)
                .Select(a => HydrateYearbookAsset(data, a, includeYearbook: false))
                .ToList()
            : new List<YearbookAsset>();
        foreach (var page in yearbook.Pages)
        {
            YearbookPageMigration.EnsureElements(page, yearbook.Assets);
        }

        return yearbook;
    }

    public static YearbookPage HydrateYearbookPage(
        HomeschoolData data,
        YearbookPage source,
        bool includeYearbook = true)
    {
        var page = HomeschoolDataStore.Clone(source);
        YearbookPageMigration.EnsureElements(page, data.YearbookAssets);
        if (includeYearbook)
        {
            var yearbook = data.Yearbooks.FirstOrDefault(y => y.Id == page.YearbookId);
            if (yearbook is not null)
            {
                page.Yearbook = HydrateYearbook(data, yearbook, includePages: false, includeAssets: false);
            }
        }

        return page;
    }

    public static YearbookAsset HydrateYearbookAsset(
        HomeschoolData data,
        YearbookAsset source,
        bool includeYearbook = true)
    {
        var asset = HomeschoolDataStore.Clone(source);
        if (includeYearbook)
        {
            var yearbook = data.Yearbooks.FirstOrDefault(y => y.Id == asset.YearbookId);
            if (yearbook is not null)
            {
                asset.Yearbook = HydrateYearbook(data, yearbook, includePages: false, includeAssets: false);
            }
        }

        asset.Page = asset.YearbookPageId.HasValue
            ? data.YearbookPages.FirstOrDefault(p => p.Id == asset.YearbookPageId.Value)
            : null;
        asset.PortfolioItem = asset.PortfolioItemId.HasValue
            ? data.PortfolioItems.FirstOrDefault(i => i.Id == asset.PortfolioItemId.Value)
            : null;

        return asset;
    }
}
