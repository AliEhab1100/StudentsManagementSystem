using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeacherController : ControllerBase
{
    private readonly StudentManagementDbContext _context;

    public TeacherController(StudentManagementDbContext context)
    {
        _context = context;
    }

    // GET: api/teacher
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Teacher>>> Index()
    {
        var teachers = await _context.Teachers
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.TeacherId)
            .ToListAsync();

        return Ok(teachers);
    }

    // GET: api/teacher/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Teacher>> Details(int id)
    {
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.TeacherId == id && !t.IsDeleted);

        if (teacher == null)
            return NotFound(new { message = "Teacher not found." });

        return Ok(teacher);
    }

    // GET: api/teacher/search/{teacherId}
    [HttpGet("search/{teacherId}")]
    public async Task<ActionResult<Teacher>> Search(int teacherId)
    {
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.TeacherId == teacherId && !t.IsDeleted);

        if (teacher == null)
            return NotFound(new { message = $"No teacher found with ID {teacherId}." });

        return Ok(teacher);
    }

    // POST: api/teacher
    [HttpPost]
    public async Task<ActionResult<Teacher>> Create([FromBody] Teacher teacher)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(teacher.Name))
            return BadRequest(new { message = "Teacher name is required." });

        _context.Teachers.Add(teacher);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Details), new { id = teacher.TeacherId }, teacher);
    }

    // PUT: api/teacher/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Edit(int id, [FromBody] Teacher updatedTeacher)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.TeacherId == id && !t.IsDeleted);

        if (teacher == null)
            return NotFound(new { message = "Teacher not found." });

        if (string.IsNullOrWhiteSpace(updatedTeacher.Name))
            return BadRequest(new { message = "Teacher name is required." });

        teacher.Name = updatedTeacher.Name;
        teacher.Address = updatedTeacher.Address;
        teacher.Email = updatedTeacher.Email;
        teacher.NationalId = updatedTeacher.NationalId;
        teacher.Position = updatedTeacher.Position;

        _context.Teachers.Update(teacher);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Teacher updated successfully.", data = teacher });
    }

    // DELETE: api/teacher/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.TeacherId == id && !t.IsDeleted);

        if (teacher == null)
            return NotFound(new { message = "Teacher not found." });

        // Soft delete
        teacher.IsDeleted = true;
        teacher.DeletedAt = DateTime.UtcNow;

        _context.Teachers.Update(teacher);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Teacher deleted successfully." });
    }
}
