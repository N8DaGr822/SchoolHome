using Microsoft.EntityFrameworkCore;
using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Infrastructure.Data;

public class HomeschoolDbContext : DbContext
{
    public HomeschoolDbContext(DbContextOptions<HomeschoolDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<LessonPlan> LessonPlans { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Student entity
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.GradeLevel).IsRequired().HasMaxLength(50);
            entity.Property(e => e.GPA).HasPrecision(3, 2);
            
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Course entity
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(100);
            entity.Property(e => e.GradeLevel).IsRequired().HasMaxLength(50);
        });

        // Configure Assignment entity
        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Subject).HasMaxLength(100);
            entity.Property(e => e.Grade).HasMaxLength(50);
            entity.Property(e => e.StudentName).HasMaxLength(200);
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(e => e.Course)
                  .WithMany(c => c.Assignments)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Student)
                  .WithMany(s => s.Assignments)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Grade entity
        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Score).HasPrecision(5, 2);
            entity.Property(e => e.Comments).HasMaxLength(500);
            entity.Property(e => e.Subject).HasMaxLength(100);
            entity.Property(e => e.Assignment).HasMaxLength(200);
            entity.Property(e => e.GradeValue).HasMaxLength(50);

            entity.HasOne(e => e.AssignmentEntity)
                  .WithMany(a => a.Grades)
                  .HasForeignKey(e => e.AssignmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Student)
                  .WithMany(s => s.Grades)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure LessonPlan entity
        modelBuilder.Entity<LessonPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Objectives).HasMaxLength(1000);
            entity.Property(e => e.Materials).HasMaxLength(1000);
            entity.Property(e => e.Activities).HasMaxLength(2000);
            entity.Property(e => e.Assessment).HasMaxLength(1000);

            entity.HasOne(e => e.Course)
                  .WithMany(c => c.LessonPlans)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure many-to-many relationship between Students and Courses
        modelBuilder.Entity<Student>()
            .HasMany(s => s.Courses)
            .WithMany(c => c.Students)
            .UsingEntity(j => j.ToTable("StudentCourses"));
    }
}
