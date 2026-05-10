using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;

namespace StudentManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentController : ControllerBase
{
    private readonly StudentManagementDbContext _context;
    private readonly StudentAttendanceService _attendanceService;

    public StudentController(StudentManagementDbContext context, StudentAttendanceService attendanceService)
    {
        _context = context;
        _attendanceService = attendanceService;
    }

    // GET: api/student
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> Index()
    {
        var students = await _context.Students
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.StudentId)
            .ToListAsync();

        return Ok(students);
    }

    // GET: api/student/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Student>> Details(int id)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == id && !s.IsDeleted);

        if (student == null)
            return NotFound(new { message = "Student not found." });

        return Ok(student);
    }

    // GET: api/student/search/{studentId}
    [HttpGet("search/{studentId}")]
    public async Task<ActionResult<Student>> Search(int studentId)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId && !s.IsDeleted);

        if (student == null)
            return NotFound(new { message = $"No student found with ID {studentId}." });

        return Ok(student);
    }

    // POST: api/student
    [HttpPost]
    public async Task<ActionResult<Student>> Create([FromBody] Student student)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(student.Name))
            return BadRequest(new { message = "Student name is required." });

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Details), new { id = student.StudentId }, student);
    }

    // PUT: api/student/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Edit(int id, [FromBody] Student updatedStudent)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == id && !s.IsDeleted);

        if (student == null)
            return NotFound(new { message = "Student not found." });

        if (string.IsNullOrWhiteSpace(updatedStudent.Name))
            return BadRequest(new { message = "Student name is required." });

        student.Name = updatedStudent.Name;
        student.BirthDate = updatedStudent.BirthDate;
        student.Address = updatedStudent.Address;
        student.Email = updatedStudent.Email;
        student.NationalId = updatedStudent.NationalId;
        student.TotalGrade = updatedStudent.TotalGrade;

        _context.Students.Update(student);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Student updated successfully.", data = student });
    }

    // DELETE: api/student/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == id && !s.IsDeleted);

        if (student == null)
            return NotFound(new { message = "Student not found." });

        // Soft delete
        student.IsDeleted = true;
        student.DeletedAt = DateTime.UtcNow;

        _context.Students.Update(student);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Student deleted successfully." });
    }

    // GET: api/student/{id}/attendance
    [HttpGet("{id}/attendance")]
    public async Task<ActionResult> GetStudentAttendance(int id)
    {
        var attendanceRecords = await _attendanceService.GetStudentAttendanceAsync(id);

        if (!attendanceRecords.Any())
            return NotFound(new { message = "No attendance records found for this student or student not found." });

        return Ok(attendanceRecords);
    }

    // GET: api/student/{id}/attendance-stats
    [HttpGet("{id}/attendance-stats")]
    public async Task<ActionResult> GetStudentAttendanceStats(int id)
    {
        var stats = await _attendanceService.GetStudentAttendanceStatsAsync(id);

        if (stats == null)
            return NotFound(new { message = "Student not found." });

        return Ok(stats);
    }

    // GET: api/student/{id}/profile
    [HttpGet("{id}/profile")]
    public async Task<ActionResult> GetStudentProfile(int id)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == id && !s.IsDeleted);

        if (student == null)
            return NotFound(new { message = "Student not found." });

        // Get attendance stats
        var attendanceStats = await _attendanceService.GetStudentAttendanceStatsAsync(id);

        return Ok(new
        {
            student = student,
            attendanceStats = attendanceStats
        });
    }
}
