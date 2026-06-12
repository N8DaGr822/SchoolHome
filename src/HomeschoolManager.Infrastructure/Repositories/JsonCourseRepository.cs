using HomeschoolManager.Core.Entities;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonCourseRepository : JsonRepositoryBase<Course>
{
    public JsonCourseRepository(HomeschoolDataStore store)
        : base(store)
    {
    }

    private protected override List<Course> Items(HomeschoolData data) => data.Courses;

    protected override string EntityLabel => "Course";

    private protected override Course Hydrate(HomeschoolData data, Course entity) =>
        RepositoryProjection.HydrateCourse(data, entity);

    private protected override IEnumerable<Course> Order(HomeschoolData data, IEnumerable<Course> items) =>
        items.OrderBy(c => c.Name);

    private protected override void OnSaving(HomeschoolData data, Course entity)
    {
        foreach (var lessonPlan in entity.LessonPlans)
        {
            lessonPlan.CourseId = entity.Id;
        }
    }

    private protected override void OnDeleting(HomeschoolData data, int id)
    {
        var assignmentIds = data.Assignments
            .Where(a => a.CourseId == id)
            .Select(a => a.Id)
            .ToHashSet();

        data.Assignments.RemoveAll(a => a.CourseId == id);
        data.Grades.RemoveAll(g => assignmentIds.Contains(g.AssignmentId));
    }
}
