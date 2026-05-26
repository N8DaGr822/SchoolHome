using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class StudentServiceTests
{
    [Fact]
    public async Task CreateStudentAsync_StampsCreatedAtAndSavesStudent()
    {
        var repository = new FakeStudentRepository();
        var service = new StudentService(repository);
        var student = new Student
        {
            FirstName = "Ava",
            LastName = "Brown",
            DateOfBirth = new DateTime(2015, 4, 12),
            GradeLevel = "4th",
            EnrollmentDate = DateTime.Today
        };

        var created = await service.CreateStudentAsync(student);

        Assert.Equal(1, created.Id);
        Assert.NotEqual(default, created.CreatedAt);
        Assert.Single(await repository.GetAllAsync());
    }

    [Fact]
    public async Task GetStudentGradesAsync_ReturnsGradesFromRepository()
    {
        var repository = new FakeStudentRepository();
        var service = new StudentService(repository);
        var student = await service.CreateStudentAsync(new Student
        {
            FirstName = "Mia",
            LastName = "Green",
            DateOfBirth = new DateTime(2014, 1, 20),
            GradeLevel = "5th",
            EnrollmentDate = DateTime.Today,
            Grades =
            [
                new Grade { Id = 1, GradeValue = "A", Subject = "Math", StudentId = 1 }
            ]
        });

        var grades = (await service.GetStudentGradesAsync(student.Id)).ToList();

        Assert.Single(grades);
        Assert.Equal("A", grades[0].GradeValue);
    }

    private sealed class FakeStudentRepository : IStudentRepository
    {
        private readonly List<Student> _students = new();

        public Task<Student?> GetByIdAsync(int id)
        {
            return Task.FromResult(_students.FirstOrDefault(s => s.Id == id));
        }

        public Task<IEnumerable<Student>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Student>>(_students);
        }

        public Task<Student> AddAsync(Student entity)
        {
            entity.Id = _students.Select(s => s.Id).DefaultIfEmpty(0).Max() + 1;
            foreach (var grade in entity.Grades)
            {
                grade.StudentId = entity.Id;
            }

            _students.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(Student entity)
        {
            var index = _students.FindIndex(s => s.Id == entity.Id);
            if (index >= 0)
            {
                _students[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _students.RemoveAll(s => s.Id == id);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id)
        {
            return Task.FromResult(_students.Any(s => s.Id == id));
        }

        public Task<IEnumerable<Student>> GetByGradeLevelAsync(string gradeLevel)
        {
            return Task.FromResult<IEnumerable<Student>>(_students.Where(s => s.GradeLevel == gradeLevel));
        }

        public Task<IEnumerable<Student>> GetActiveStudentsAsync()
        {
            return Task.FromResult<IEnumerable<Student>>(_students);
        }

        public Task<Student?> GetWithCoursesAsync(int id)
        {
            return GetByIdAsync(id);
        }

        public Task<Student?> GetWithAssignmentsAsync(int id)
        {
            return GetByIdAsync(id);
        }

        public Task<Student?> GetWithGradesAsync(int id)
        {
            return GetByIdAsync(id);
        }
    }
}
