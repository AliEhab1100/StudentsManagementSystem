using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly StudentManagementDbContext _context;
    private readonly ExcelParsingService _excelParsingService;
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public AttendanceController(StudentManagementDbContext context, ExcelParsingService excelParsingService)
    {
        _context = context;
        _excelParsingService = excelParsingService;
    }

    // GET: api/attendance/sessions
    [HttpGet("sessions")]
    public async Task<ActionResult<IEnumerable<AttendanceSession>>> GetSessions()
    {
        var sessions = await _context.AttendanceSessions
            .Include(s => s.Teacher)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return Ok(sessions);
    }

    // GET: api/attendance/sessions/{sessionId}
    [HttpGet("sessions/{sessionId}")]
    public async Task<ActionResult<AttendanceSession>> GetSession(int sessionId)
    {
        var session = await _context.AttendanceSessions
            .Include(s => s.Teacher)
            .Include(s => s.AttendanceRecords)
            .ThenInclude(ar => ar.Student)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            return NotFound(new { message = "Attendance session not found." });

        return Ok(session);
    }

    // GET: api/attendance/records/{sessionId}
    [HttpGet("records/{sessionId}")]
    public async Task<ActionResult<IEnumerable<AttendanceRecord>>> GetSessionRecords(int sessionId)
    {
        var records = await _context.AttendanceRecords
            .Where(ar => ar.SessionId == sessionId)
            .Include(ar => ar.Student)
            .OrderBy(ar => ar.Student.Name)
            .ToListAsync();

        if (!records.Any())
            return NotFound(new { message = "No attendance records found for this session." });

        return Ok(records);
    }

    // POST: api/attendance/upload
    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> UploadAttendance([FromForm] AttendanceUploadViewModel model)
    {
        // Validate input
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (model.ExcelFile == null || model.ExcelFile.Length == 0)
            return BadRequest(new { message = "Excel file is required." });

        if (model.ExcelFile.Length > MaxFileSize)
            return BadRequest(new { message = "File size exceeds 5 MB limit." });

        if (string.IsNullOrWhiteSpace(model.CourseTitle))
            return BadRequest(new { message = "Course title is required." });

        if (model.TeacherId <= 0)
            return BadRequest(new { message = "Valid teacher ID is required." });

        if (model.AttendanceDate == default)
            return BadRequest(new { message = "Attendance date is required." });

        // Check if teacher exists and is not deleted
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.TeacherId == model.TeacherId && !t.IsDeleted);

        if (teacher == null)
            return NotFound(new { message = "Teacher not found or has been deleted." });

        try
        {
            // Parse Excel file
            var attendanceRecords = _excelParsingService.ParseAttendanceFile(model.ExcelFile);

            // Validate that students exist in the database
            var studentIds = attendanceRecords.Select(ar => ar.StudentId).Distinct().ToList();
            var existingStudents = await _context.Students
                .Where(s => studentIds.Contains(s.StudentId) && !s.IsDeleted)
                .Select(s => s.StudentId)
                .ToListAsync();

            var invalidStudentIds = studentIds.Except(existingStudents).ToList();
            if (invalidStudentIds.Any())
                return BadRequest(new { message = $"Invalid or deleted student IDs found: {string.Join(", ", invalidStudentIds)}" });

            // Create AttendanceSession
            var session = new AttendanceSession
            {
                TeacherId = model.TeacherId,
                CourseTitle = model.CourseTitle,
                AttendanceDate = model.AttendanceDate,
                UploadedFileName = model.ExcelFile.FileName,
                CreatedAt = DateTime.UtcNow
            };

            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            // Create AttendanceRecords
            var records = attendanceRecords
                .Where(ar => existingStudents.Contains(ar.StudentId))
                .Select(ar => new AttendanceRecord
                {
                    SessionId = session.SessionId,
                    StudentId = ar.StudentId,
                    IsPresent = ar.IsPresent
                })
                .ToList();

            _context.AttendanceRecords.AddRange(records);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Attendance uploaded successfully.",
                session = new
                {
                    session.SessionId,
                    session.TeacherId,
                    session.CourseTitle,
                    session.AttendanceDate,
                    session.UploadedFileName,
                    session.CreatedAt
                },
                totalRecords = records.Count,
                presentCount = records.Count(r => r.IsPresent),
                absentCount = records.Count(r => !r.IsPresent)
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while processing the attendance file: {ex.Message}" });
        }
    }

    // DELETE: api/attendance/sessions/{sessionId}
    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(int sessionId)
    {
        var session = await _context.AttendanceSessions
            .Include(s => s.AttendanceRecords)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            return NotFound(new { message = "Attendance session not found." });

        _context.AttendanceRecords.RemoveRange(session.AttendanceRecords);
        _context.AttendanceSessions.Remove(session);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Attendance session and all related records deleted successfully." });
    }
}
