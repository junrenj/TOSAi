using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using TOSAi.TeacherApp.Models;

namespace TOSAi.TeacherApp.Services;

public static class CsvScoreTemplateService
{
    private static readonly string[] Header =
    [
        "考试名称",
        "考试日期",
        "年级",
        "班级",
        "学号",
        "姓名",
        "学科",
        "分数",
        "满分"
    ];

    public static void ExportTemplate(string filePath)
    {
        string[] lines =
        [
            ToCsvLine(Header),
            ToCsvLine(["2026 春季期中考试", "2026-04-20", "初二", "初二 1 班", "20260101", "李明", "语文", "91", "120"]),
            ToCsvLine(["2026 春季期中考试", "2026-04-20", "初二", "初二 1 班", "20260101", "李明", "数学", "98", "120"])
        ];

        File.WriteAllLines(filePath, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    public static ObservableCollection<ScoreImportRow> Import(string filePath)
    {
        using TextFieldParser parser = new(filePath, Encoding.UTF8)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true
        };
        parser.SetDelimiters(",");

        string[]? header = parser.ReadFields();
        ValidateHeader(header);

        ObservableCollection<ScoreImportRow> rows = [];
        int rowNumber = 1;

        while (!parser.EndOfData)
        {
            rowNumber++;
            string[]? fields = parser.ReadFields();
            if (fields is null || fields.Length == 0 || fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (fields.Length != Header.Length)
            {
                throw new InvalidDataException($"第 {rowNumber} 行字段数量不正确，应为 {Header.Length} 列。");
            }

            rows.Add(ParseRow(fields, rowNumber));
        }

        return rows;
    }

    private static void ValidateHeader(string[]? header)
    {
        if (header is null || header.Length != Header.Length)
        {
            throw new InvalidDataException("模板表头不正确，请先导出模板再填写成绩。");
        }

        for (int i = 0; i < Header.Length; i++)
        {
            if (!string.Equals(header[i].Trim(), Header[i], StringComparison.Ordinal))
            {
                throw new InvalidDataException($"第 {i + 1} 列应为“{Header[i]}”，当前为“{header[i]}”。");
            }
        }
    }

    private static ScoreImportRow ParseRow(string[] fields, int rowNumber)
    {
        string examName = Required(fields[0], rowNumber, "考试名称");
        DateOnly examDate = ParseDate(fields[1], rowNumber);
        string gradeName = Required(fields[2], rowNumber, "年级");
        string className = Required(fields[3], rowNumber, "班级");
        string studentId = Required(fields[4], rowNumber, "学号");
        string studentName = Required(fields[5], rowNumber, "姓名");
        string subjectName = Required(fields[6], rowNumber, "学科");
        double score = ParseNumber(fields[7], rowNumber, "分数");
        double fullScore = ParseNumber(fields[8], rowNumber, "满分");

        if (score < 0 || fullScore <= 0 || score > fullScore)
        {
            throw new InvalidDataException($"第 {rowNumber} 行分数范围不正确，请确认 0 <= 分数 <= 满分。");
        }

        return new ScoreImportRow
        {
            ExamName = examName,
            ExamDate = examDate,
            GradeName = gradeName,
            ClassName = className,
            StudentId = studentId,
            StudentName = studentName,
            SubjectName = subjectName,
            Score = score,
            FullScore = fullScore
        };
    }

    private static string Required(string value, int rowNumber, string columnName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"第 {rowNumber} 行“{columnName}”不能为空。");
        }

        return value.Trim();
    }

    private static DateOnly ParseDate(string value, int rowNumber)
    {
        string trimmed = value.Trim().Trim('\uFEFF');
        string[] acceptedFormats =
        [
            "yyyy-MM-dd",
            "yyyy/M/d",
            "yyyy/MM/dd",
            "yyyy.M.d",
            "yyyy.MM.dd",
            "yyyy年M月d日",
            "yyyy年MM月dd日",
            "M/d/yyyy",
            "MM/dd/yyyy",
            "M-d-yyyy",
            "MM-dd-yyyy"
        ];

        if (DateOnly.TryParseExact(trimmed, acceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly exactDate))
        {
            return exactDate;
        }

        if (DateTime.TryParse(trimmed, CultureInfo.GetCultureInfo("zh-CN"), DateTimeStyles.None, out DateTime parsedDate) ||
            DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
        {
            return DateOnly.FromDateTime(parsedDate);
        }

        if (double.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out double oaDate) && oaDate > 1)
        {
            try
            {
                return DateOnly.FromDateTime(DateTime.FromOADate(oaDate));
            }
            catch (ArgumentException)
            {
                // Fall through to the user-facing validation error below.
            }
        }

        throw new InvalidDataException($"第 {rowNumber} 行“考试日期”格式不正确，当前值为“{value}”。请使用 2026-04-20 或 2026/4/20 这种日期格式。");
    }

    private static double ParseNumber(string value, int rowNumber, string columnName)
    {
        if (double.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out double number))
        {
            return number;
        }

        throw new InvalidDataException($"第 {rowNumber} 行“{columnName}”必须是数字。");
    }

    private static string ToCsvLine(IEnumerable<string> fields) => string.Join(",", fields.Select(Escape));

    private static string Escape(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
