using HrPayrollSystem.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HrPayrollSystem.Services;

public class PayslipPdfService
{
    public byte[] Generate(Payslip payslip)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var employee = payslip.Employee;
        var monthName = new DateTime(payslip.Year, payslip.Month, 1).ToString("MMMM yyyy");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                // ===== HEADER =====
                page.Header().Column(col =>
                {
                    col.Item().AlignCenter()
                        .Text(t => t.Span("HR PAYROLL SYSTEM").FontSize(16).Bold().FontColor(Colors.Blue.Darken2));

                    col.Item().AlignCenter()
                        .Text(t => t.Span($"Payslip - {monthName}").FontSize(12).SemiBold());

                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                // ===== CONTENT =====
                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(12);

                    // --- Employee info ---
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(t =>
                            {
                                t.Span("Employee: ").SemiBold();
                                t.Span(employee?.FullName ?? "-").Bold();
                            });
                            c.Item().Text(t =>
                            {
                                t.Span("Code: ");
                                t.Span(employee?.EmployeeCode ?? "-");
                            });
                            c.Item().Text(t =>
                            {
                                t.Span("Designation: ");
                                t.Span(employee?.Designation ?? "-");
                            });
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignRight().Text(t =>
                            {
                                t.Span("Department: ");
                                t.Span(employee?.Department?.Name ?? "-");
                            });
                            c.Item().AlignRight().Text(t =>
                            {
                                t.Span("Generated: ");
                                t.Span(payslip.GeneratedOn.ToLocalTime().ToString("dd MMM yyyy"));
                            });
                        });
                    });

                    // --- Attendance summary cards ---
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().InfoCard("Working Days", payslip.WorkingDays.ToString());
                        row.ConstantItem(10);
                        row.RelativeItem().InfoCard("LOP Days", payslip.LopDays.ToString("0.##"));
                        row.ConstantItem(10);
                        row.RelativeItem().InfoCard("Paid Days", payslip.PaidDays.ToString("0.##"));
                    });

                    // --- Earnings table ---
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Earnings");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Amount (Rs.)");
                        });

                        AddRow(table, "Basic", payslip.EarnedBasic);
                        AddRow(table, "HRA", payslip.EarnedHra);
                        AddRow(table, "Conveyance", payslip.EarnedConveyance);
                        AddRow(table, "Special Allowance", payslip.EarnedSpecial);

                        table.Cell().Element(TotalStyle).Text("Gross Earned");
                        table.Cell().Element(TotalStyle).AlignRight().Text(payslip.EarnedGross.ToString("N2"));
                    });

                    // --- Deductions table ---
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Deductions");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Amount (Rs.)");
                        });

                        AddRow(table, "Provident Fund (12% of earned Basic)", payslip.PfDeduction);
                        AddRow(table, "Professional Tax", payslip.ProfessionalTax);

                        table.Cell().Element(TotalStyle).Text("Total Deductions");
                        table.Cell().Element(TotalStyle).AlignRight().Text(payslip.TotalDeductions.ToString("N2"));
                    });

                    // --- NET PAY banner ---
                    col.Item().Background(Colors.Blue.Lighten4).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Text(t => t.Span("NET PAY").FontSize(13).Bold());
                        row.RelativeItem().AlignRight()
                            .Text(t => t.Span($"Rs. {payslip.NetPay:N2}").FontSize(13).Bold());
                    });

                    // --- Footer note ---
                    col.Item().PaddingTop(8).Text(t =>
                        t.Span("This is a system-generated payslip and does not require a signature.")
                         .FontSize(8).FontColor(Colors.Grey.Darken1));
                });

                // ===== FOOTER =====
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    // ===== style helpers (static = no captured state, compiler-friendly) =====
    private static IContainer HeaderStyle(IContainer container)
        => container.Background(Colors.Grey.Lighten2).Padding(5);

    private static IContainer CellStyle(IContainer container)
        => container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(5);

    private static IContainer TotalStyle(IContainer container)
        => container.Background(Colors.Grey.Lighten3).Padding(5);

    private static void AddRow(TableDescriptor table, string label, decimal amount)
    {
        table.Cell().Element(CellStyle).Text(label);
        table.Cell().Element(CellStyle).AlignRight().Text(amount.ToString("N2"));
    }
}

// InfoCard is now correctly defined on IContainer (was wrongly on RowDescriptor before)
public static class PayslipPdfExtensions
{
    public static void InfoCard(this IContainer container, string label, string value)
    {
        container.Background(Colors.Grey.Lighten3).Padding(6).Column(col =>
        {
            col.Item().Text(t => t.Span(label).FontSize(8).FontColor(Colors.Grey.Darken2));
            col.Item().Text(t => t.Span(value).FontSize(12).Bold());
        });
    }
}