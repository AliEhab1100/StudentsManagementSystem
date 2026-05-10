namespace StudentManagementSystem.ViewModels;

public class AttendanceUploadViewModel
{
    // TeacherId should come from logged-in session/user context
    public int TeacherId { get; set; }
    public string CourseTitle { get; set; } = null!;
    public DateTime AttendanceDate { get; set; }
    public IFormFile ExcelFile { get; set; } = null!;
}
