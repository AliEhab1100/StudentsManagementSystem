using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly StudentManagementDbContext _context;
    private readonly AdminGradeService _gradeService;

    public AdminController(StudentManagementDbContext context, AdminGradeService gradeService)
    {
        _context = context;
        _gradeService = gradeService;
    }

    // GET: api/admin/students
    [HttpGet("students")]
    public async Task<ActionResult<IEnumerable<Student>>> GetAllStudents()
    {
        var students = await _context.Students
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.StudentId)
            .ToListAsync();

        return Ok(students);
    }

    // GET: api/admin/teachers
    [HttpGet("teachers")]
    public async Task<ActionResult<IEnumerable<Teacher>>> GetAllTeachers()
    {
        var teachers = await _context.Teachers
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.TeacherId)
            .ToListAsync();

        return Ok(teachers);
    }

    // GET: api/admin/students/{studentId}/grade-and-attendance
    [HttpGet("students/{studentId}/grade-and-attendance")]
    public async Task<ActionResult> GetStudentGradeAndAttendance(int studentId)
    {
        var result = await _gradeService.GetStudentGradeAndAttendanceAsync(studentId);

        if (result == null)
            return NotFound(new { message = "Student not found or has been deleted." });

        return Ok(result);
    }

    // PUT: api/admin/students/{studentId}/grade
    [HttpPut("students/{studentId}/grade")]
    public async Task<IActionResult> UpdateStudentGrade(int studentId, [FromBody] UpdateStudentGradeViewModel model)
    {
        if (model.StudentId != studentId)
            return BadRequest(new { message = "StudentId mismatch." });

        var (success, message, updatedGrade) = await _gradeService.UpdateStudentGradeAsync(
            studentId,
            model.TotalGrade
        );

        if (!success)
            return BadRequest(new { message = message });

        return Ok(new
        {
            message = message,
            studentId = studentId,
            updatedGrade = updatedGrade
        });
    }

    // POST: api/admin/students/grades/bulk-update
    [HttpPost("students/grades/bulk-update")]
    public async Task<IActionResult> BulkUpdateGrades([FromBody] Dictionary<int, decimal?> gradeUpdates)
    {
        if (gradeUpdates == null || gradeUpdates.Count == 0)
            return BadRequest(new { message = "No grade updates provided." });

        var (successCount, failureCount, errors) = await _gradeService.BulkUpdateGradesAsync(gradeUpdates);

        return Ok(new
        {
            message = $"Bulk update completed. {successCount} successful, {failureCount} failed.",
            successCount = successCount,
            failureCount = failureCount,
            errors = errors
        });
    }

    // GET: api/admin/admins
    [HttpGet("admins")]
    public async Task<ActionResult<IEnumerable<Admin>>> GetAllAdmins()
    {
        var admins = await _context.Admins
            .OrderBy(a => a.AdminId)
            .ToListAsync();

        return Ok(admins);
    }

    // GET: api/admin/admins/{adminId}
    [HttpGet("admins/{adminId}")]
    public async Task<ActionResult<Admin>> GetAdmin(int adminId)
    {
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.AdminId == adminId);

        if (admin == null)
            return NotFound(new { message = "Admin not found." });

        return Ok(admin);
    }

    // GET: api/admin/system-stats
    [HttpGet("system-stats")]
    public async Task<ActionResult> GetSystemStats()
    {
        var totalStudents = await _context.Students.CountAsync(s => !s.IsDeleted);
        var totalTeachers = await _context.Teachers.CountAsync(t => !t.IsDeleted);
        var totalAdmins = await _context.Admins.CountAsync();
        var totalAttendanceSessions = await _context.AttendanceSessions.CountAsync();
        var totalAttendanceRecords = await _context.AttendanceRecords.CountAsync();

        return Ok(new
        {
            statistics = new
            {
                TotalStudents = totalStudents,
                TotalTeachers = totalTeachers,
                TotalAdmins = totalAdmins,
                TotalAttendanceSessions = totalAttendanceSessions,
                TotalAttendanceRecords = totalAttendanceRecords
            }
        });
    }
}
