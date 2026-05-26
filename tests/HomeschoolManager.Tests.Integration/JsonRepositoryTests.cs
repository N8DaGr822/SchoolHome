using HomeschoolManager.Core.Entities;
using HomeschoolManager.Infrastructure.Data;
using HomeschoolManager.Infrastructure.Repositories;
using Xunit;

namespace HomeschoolManager.Tests.Integration;

public class JsonRepositoryTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _dataFilePath;

    public JsonRepositoryTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "TestData", Guid.NewGuid().ToString("N"));
        _dataFilePath = Path.Combine(_testDirectory, "homeschool-data.json");
    }

    [Fact]
    public async Task StudentRepository_PersistsAddedStudentAcrossStoreInstances()
    {
        var firstRepository = new JsonStudentRepository(new HomeschoolDataStore(_dataFilePath));
        var created = await firstRepository.AddAsync(new Student
        {
            FirstName = "Noah",
            LastName = "Parker",
            DateOfBirth = new DateTime(2013, 2, 8),
            GradeLevel = "6th",
            EnrollmentDate = DateTime.Today
        });

        var secondRepository = new JsonStudentRepository(new HomeschoolDataStore(_dataFilePath));
        var reloaded = await secondRepository.GetByIdAsync(created.Id);

        Assert.NotNull(reloaded);
        Assert.Equal("Noah", reloaded.FirstName);
        Assert.Equal("Parker", reloaded.LastName);
    }

    [Fact]
    public async Task AssignmentRepository_CompletedAssignmentIsRemovedFromOpenAssignments()
    {
        var repository = new JsonAssignmentRepository(new HomeschoolDataStore(_dataFilePath));
        var openAssignment = (await repository.GetOpenAssignmentsAsync()).First();
        openAssignment.Status = AssignmentStatus.Completed;

        await repository.UpdateAsync(openAssignment);

        var reopenedRepository = new JsonAssignmentRepository(new HomeschoolDataStore(_dataFilePath));
        var openAssignments = await reopenedRepository.GetOpenAssignmentsAsync();

        Assert.DoesNotContain(openAssignments, a => a.Id == openAssignment.Id);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
