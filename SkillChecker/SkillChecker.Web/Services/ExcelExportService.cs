using ClosedXML.Excel;
using SkillChecker.Common.Models;

namespace SkillChecker.Web.Services;

public static class ExcelExportService
{
    public static byte[] BuildExcel(List<TestResult> results)
    {
        using XLWorkbook workbook = new XLWorkbook();
        IXLWorksheet ws = workbook.Worksheets.Add("Результаты");

        ws.Cell(1, 1).Value = "ФИО";
        ws.Cell(1, 2).Value = "Группа";
        ws.Cell(1, 3).Value = "Тест";
        ws.Cell(1, 4).Value = "Дата";
        ws.Cell(1, 5).Value = "Правильно";
        ws.Cell(1, 6).Value = "Всего";
        ws.Cell(1, 7).Value = "Балл %";

        IXLRange headerRange = ws.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4A90D9");
        headerRange.Style.Font.FontColor = XLColor.White;

        for (int i = 0; i < results.Count; i++)
        {
            int row = i + 2;
            TestResult r = results[i];
            ws.Cell(row, 1).Value = r.StudentName;
            ws.Cell(row, 2).Value = r.Group;
            ws.Cell(row, 3).Value = r.TestName;
            ws.Cell(row, 4).Value = r.Date.ToString("dd.MM.yyyy HH:mm");
            ws.Cell(row, 5).Value = r.CorrectAnswers;
            ws.Cell(row, 6).Value = r.TotalQuestions;
            ws.Cell(row, 7).Value = r.Score;

            if (r.Score >= 70)
                ws.Cell(row, 7).Style.Font.FontColor = XLColor.FromHtml("#2E7D32");
            else if (r.Score >= 40)
                ws.Cell(row, 7).Style.Font.FontColor = XLColor.FromHtml("#F57C00");
            else
                ws.Cell(row, 7).Style.Font.FontColor = XLColor.FromHtml("#D32F2F");
        }

        ws.Columns().AdjustToContents();

        MemoryStream ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return ms.ToArray();
    }
}
