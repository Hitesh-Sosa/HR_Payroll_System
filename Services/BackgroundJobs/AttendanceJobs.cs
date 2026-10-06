using HrPayrollSystem.Data;
using HrPayrollSystem.Models;
using HrPayrollSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace HrPayrollSystem.BackgroundJobs;

public static class AttendanceJobs
{
    // ===== NIGHTLY JOB: mark absents for YESTERDAY =====
    public static async Task MarkYesterdayAbsentsAsync(
        ApplicationDbContext context,
        ILogger logger)
    {
        var yesterday = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));

        // RULE: weekends/holidays are not working days
        if (yesterday.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            logger.LogInformation("Absence job skipped: {Date} was a weekend.", yesterday);
            return;
        }

        var isHoliday = await context.Holidays.AnyAsync(h => h.Date == yesterday);
        if (isHoliday)
        {
            logger.LogInformation("Absence job skipped: {Date} was a holiday.", yesterday);
            return;
        }

        var activeEmployees = await context.Employees
            .Where(e => e.IsActive && e.JoiningDate <= yesterday.ToDateTime(TimeOnly.MinValue))
            .ToListAsync();

        var existingRows = await context.Attendances
            .Where(a => a.Date == yesterday)
            .Select(a => a.EmployeeId)
            .ToListAsync();
        var existingSet = existingRows.ToHashSet();

        int created = 0;
        foreach (var employee in activeEmployees)
        {
            if (existingSet.Contains(employee.Id)) continue;   // has a row (present/late/leave)

            context.Attendances.Add(new Attendance
            {
                EmployeeId = employee.Id,
                Date = yesterday,
                Status = AttendanceStatus.Absent
            });
            created++;
        }

        await context.SaveChangesAsync();
        logger.LogInformation(
            "Absence job done for {Date}: {Created} absent row(s) created out of {Total} active employees.",
            yesterday, created, activeEmployees.Count);
    }

    // ===== MONTHLY JOB: email all payslips of a month =====
    public static async Task EmailMonthlyPayslipsAsync(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger logger)
    {
        // Target: previous month
        var target = DateTime.Now.AddMonths(-1);
        var year = target.Year;
        var month = target.Month;

        var payslips = await context.Payslips
            .Include(p => p.Employee)
            .Where(p => p.Year == year && p.Month == month)
            .ToListAsync();

        if (payslips.Count == 0)
        {
            logger.LogInformation("Payslip email job: no payslips exist for {Month:00}/{Year}. Did payroll run?",
                month, year);
            return;
        }

        foreach (var payslip in payslips)
        {
            var email = payslip.Employee?.FullName;   // placeholder guard
            var user = await context.Users.FindAsync(payslip.Employee!.ApplicationUserId);
            var toEmail = user?.Email;

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                logger.LogWarning("Payslip email skipped for {Code}: no email on account.",
                    payslip.Employee!.EmployeeCode);
                continue;
            }

            await emailService.SendPayslipEmailAsync(
                toEmail, payslip.Employee!.FullName, year, month, payslip.NetPay);
        }

        logger.LogInformation("Payslip email job complete for {Month:00}/{Year}: {Count} email(s).",
            month, year, payslips.Count);
    }
}