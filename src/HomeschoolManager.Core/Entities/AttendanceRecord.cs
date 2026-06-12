namespace HomeschoolManager.Core.Entities;

using System.ComponentModel.DataAnnotations;
using HomeschoolManager.Core.Interfaces;

public class AttendanceRecord : IEntity
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Student is required.")]
    public int StudentId { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.Today;

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    [Range(0, 1440, ErrorMessage = "Minutes must be between 0 and 1440.")]
    public int? Minutes { get; set; }

    [StringLength(1000)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Student Student { get; set; } = null!;
}

public enum AttendanceStatus
{
    Present,
    Absent,
    Sick,
    FieldTrip,
    Holiday,
    Partial
}
