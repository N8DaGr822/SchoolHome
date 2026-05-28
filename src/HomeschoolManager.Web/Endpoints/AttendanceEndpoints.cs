using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Web.Endpoints;

public static class AttendanceEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/attendance");

        group.MapGet("/", async (
            IAttendanceService attendanceService,
            DateTime? date,
            int? year,
            int? month) =>
        {
            if (date.HasValue)
            {
                return Results.Ok(await attendanceService.GetAttendanceByDateAsync(date.Value));
            }

            if (year.HasValue && month.HasValue)
            {
                return Results.Ok(await attendanceService.GetAttendanceByMonthAsync(year.Value, month.Value));
            }

            return Results.Ok(await attendanceService.GetAllAttendanceAsync());
        });

        group.MapGet("/{id:int}", async (IAttendanceService attendanceService, int id) =>
        {
            var attendanceRecord = await attendanceService.GetAttendanceByIdAsync(id);
            return attendanceRecord == null ? Results.NotFound() : Results.Ok(attendanceRecord);
        });

        group.MapPost("/", async (IAttendanceService attendanceService, AttendanceRecord attendanceRecord) =>
        {
            try
            {
                var created = await attendanceService.CreateAttendanceAsync(attendanceRecord);
                return Results.Created($"/api/attendance/{created.Id}", created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        group.MapPut("/{id:int}", async (
            IAttendanceService attendanceService,
            int id,
            AttendanceRecord attendanceRecord) =>
        {
            try
            {
                attendanceRecord.Id = id;
                var updated = await attendanceService.UpdateAttendanceAsync(attendanceRecord);
                return Results.Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        group.MapDelete("/{id:int}", async (IAttendanceService attendanceService, int id) =>
        {
            await attendanceService.DeleteAttendanceAsync(id);
            return Results.NoContent();
        });

        return endpoints;
    }
}
