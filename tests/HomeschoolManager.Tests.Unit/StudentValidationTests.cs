using System.ComponentModel.DataAnnotations;
using HomeschoolManager.Core.Entities;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class StudentValidationTests
{
    [Fact]
    public void Student_EmailIsOptional()
    {
        var student = CreateValidStudent();
        student.Email = string.Empty;

        var results = Validate(student);

        Assert.Empty(results);
    }

    [Fact]
    public void Student_InvalidEmailIsRejectedWhenProvided()
    {
        var student = CreateValidStudent();
        student.Email = "not-an-email";

        var results = Validate(student);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Student.Email)));
    }

    private static Student CreateValidStudent()
    {
        return new Student
        {
            FirstName = "Ava",
            LastName = "Brown",
            DateOfBirth = new DateTime(2015, 4, 12),
            GradeLevel = "4th",
            EnrollmentDate = DateTime.Today
        };
    }

    private static List<ValidationResult> Validate(Student student)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(student);
        Validator.TryValidateObject(student, context, results, validateAllProperties: true);
        return results;
    }
}
