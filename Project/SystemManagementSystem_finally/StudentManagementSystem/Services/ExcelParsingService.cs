using System.Data;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace StudentManagementSystem.Services;

public class ExcelParsingService
{
    /// <summary>
    /// Parses an Excel file and extracts attendance records.
    /// Expected format: Column A = StudentId (int), Column B = IsPresent (yes/no or 1/0)
    /// </summary>
    public List<(int StudentId, bool IsPresent)> ParseAttendanceFile(IFormFile file)
    {
        var records = new List<(int StudentId, bool IsPresent)>();

        if (file == null || file.Length == 0)
            throw new ArgumentException("Excel file is required and must not be empty.");

        if (!IsValidExcelFile(file.FileName))
            throw new ArgumentException("Invalid file format. Please upload an Excel file (.xlsx).");

        try
        {
            using (var stream = file.OpenReadStream())
            using (var document = SpreadsheetDocument.Open(stream, false))
            {
                var workbookPart = document.WorkbookPart;
                if (workbookPart == null)
                    throw new InvalidOperationException("Excel file is corrupted or invalid.");

                var worksheet = workbookPart.WorksheetParts.FirstOrDefault();
                if (worksheet == null)
                    throw new InvalidOperationException("No worksheet found in the Excel file.");

                var sheetData = worksheet.Worksheet.Elements<SheetData>().FirstOrDefault();
                if (sheetData == null)
                    throw new InvalidOperationException("No data found in the worksheet.");

                var rows = sheetData.Elements<Row>();
                bool isFirstRow = true;

                foreach (var row in rows)
                {
                    // Skip header row
                    if (isFirstRow)
                    {
                        isFirstRow = false;
                        continue;
                    }

                    var cells = row.Elements<Cell>().ToList();
                    if (cells.Count < 2)
                        continue;

                    // Extract StudentId from first cell
                    var studentIdCell = cells[0];
                    if (!int.TryParse(GetCellValue(workbookPart, studentIdCell), out int studentId))
                        continue;

                    // Extract IsPresent from second cell
                    var isPresentCell = cells[1];
                    string isPresentValue = GetCellValue(workbookPart, isPresentCell).ToLower().Trim();

                    bool isPresent = isPresentValue switch
                    {
                        "yes" or "y" or "1" or "true" => true,
                        "no" or "n" or "0" or "false" => false,
                        _ => false
                    };

                    records.Add((studentId, isPresent));
                }
            }

            if (records.Count == 0)
                throw new InvalidOperationException("No valid attendance records found in the Excel file.");

            return records;
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"Error parsing Excel file: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An unexpected error occurred while parsing the Excel file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the value of a cell from the Excel document.
    /// </summary>
    private string GetCellValue(WorkbookPart workbookPart, Cell cell)
    {
        if (cell.DataType == null)
            return cell.CellValue?.Text ?? string.Empty;

        if (cell.DataType == CellValues.SharedString)
        {
            int index = int.Parse(cell.CellValue?.Text ?? "0");
            var sharedStringPart = workbookPart.GetPartById(
                workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault()
                    ?.Id ?? string.Empty) as SharedStringTablePart;

            if (sharedStringPart?.SharedStringTable != null)
            {
                var item = sharedStringPart.SharedStringTable.Elements<SharedStringItem>().ElementAt(index);
                return item?.Text?.Text ?? string.Empty;
            }
        }

        return cell.CellValue?.Text ?? string.Empty;
    }

    /// <summary>
    /// Validates that the file is an Excel file.
    /// </summary>
    private bool IsValidExcelFile(string fileName)
    {
        var validExtensions = new[] { ".xlsx", ".xlsm" };
        var fileExtension = Path.GetExtension(fileName).ToLower();
        return validExtensions.Contains(fileExtension);
    }
}
