using HrPayrollSystem.Data;
using HrPayrollSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HrPayrollSystem.Services;

public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _context;

    // ===== Company policy constants =====
    // TimeOnly/TimeSpan cannot be 'const' (only simple types can) → 'static readonly'
    private static readonly TimeOnly WorkStartTime = new(9, 0);          // office opens 09:00
    private static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(15); // until 09:15 = fine
    private const decimal HalfDayThresholdHours = 4.0m;                  // < 4h = half day
    private const decimal FullDayHours = 8.0m;                           // standard workday

    public AttendanceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Attendance> CheckInAsync(int employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        // RULE 1: employee can check in only once per day
        var existing = await GetTodayAttendanceAsync(employeeId);
        if (existing != null)
        {
            throw new InvalidOperationException(
                "You have already checked in today. Check-in is allowed only once per day.");
        }

        var now = TimeOnly.FromDateTime(DateTime.Now);

        // RULE 2: late detection (grace period)
        // After 09:15 = Late, 09:15 or earlier = Present
        var status = now > WorkStartTime.Add(GracePeriod)
            ? AttendanceStatus.Late
            : AttendanceStatus.Present;

        var attendance = new Attendance
        {
            EmployeeId = employeeId,
            Date = today,
            CheckIn = now,
            Status = status
        };

        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync();

        return attendance;
    }

    public async Task<Attendance> CheckOutAsync(int employeeId)
    {
        // RULE 3: must have checked in first
        var attendance = await GetTodayAttendanceAsync(employeeId)
            ?? throw new InvalidOperationException(
                "You must check in before checking out.");

        // RULE 4: cannot check out twice
        if (attendance.CheckOut != null)
        {
            throw new InvalidOperationException(
                "You have already checked out today.");
        }

        // RULE 5: check-in time must exist (defensive guard)
        if (attendance.CheckIn == null)
        {
            throw new InvalidOperationException(
                "Check-in time is missing.");
        }

        var now = TimeOnly.FromDateTime(DateTime.Now);
        var checkIn = attendance.CheckIn.Value;

        // RULE 6: compute hours worked (minutes-based calculation)
        var checkInMinutes =
            checkIn.Hour * 60 +
            checkIn.Minute +
            checkIn.Second / 60.0;

        var checkOutMinutes =
            now.Hour * 60 +
            now.Minute +
            now.Second / 60.0;

        var hoursWorked = (decimal)((checkOutMinutes - checkInMinutes) / 60.0);

        // RULE 7: check-out cannot be earlier than check-in
        // (edge case caught in review — prevents negative hours)
        if (hoursWorked < 0)
        {
            throw new InvalidOperationException(
                "Check-out time cannot be earlier than check-in time.");
        }

        attendance.CheckOut = now;
        attendance.HoursWorked = Math.Round(hoursWorked, 2);

        // ==============================
        // ATTENDANCE STATUS RULES
        // ==============================
        // < 4 hours  → HalfDay (less than half a day's work → half-day pay in payroll)
        // 4–8 hours  → keep Present/Late from check-in (worked at least half a day)
        // 8+ hours   → keep Present/Late (full day; overtime handled in payroll later)
        if (hoursWorked < HalfDayThresholdHours)
        {
            attendance.Status = AttendanceStatus.HalfDay;
        }
        // else: status stays as set at check-in (Present or Late)

        await _context.SaveChangesAsync();

        return attendance;
    }

    public async Task<Attendance?> GetTodayAttendanceAsync(int employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        return await _context.Attendances
            .FirstOrDefaultAsync(a =>
                a.EmployeeId == employeeId &&
                a.Date == today);
    }

    public async Task<List<Attendance>> GetMonthlyAttendanceAsync(
        int employeeId,
        int year,
        int month)
    {
        return await _context.Attendances
            .Where(a =>
                a.EmployeeId == employeeId &&
                a.Date.Year == year &&
                a.Date.Month == month)
            .OrderBy(a => a.Date)
            .ToListAsync();
    }
}