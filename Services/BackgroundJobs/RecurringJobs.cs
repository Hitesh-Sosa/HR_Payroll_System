using HrPayrollSystem.Data;
using HrPayrollSystem.Services;

namespace HrPayrollSystem.BackgroundJobs;

// Instance class — Hangfire creates it via DI and injects these dependencies automatically
public class RecurringJobs
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<RecurringJobs> _logger;

    public RecurringJobs(ApplicationDbContext context,
                         IEmailService emailService,
                         ILogger<RecurringJobs> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    // Nightly 23:30 — job id: "mark-yesterday-absents"
    public Task MarkYesterdayAbsents()
        => AttendanceJobs.MarkYesterdayAbsentsAsync(_context, _logger);

    // Monthly day 2 at 09:00 — job id: "email-monthly-payslips"
    public Task EmailMonthlyPayslips()
        => AttendanceJobs.EmailMonthlyPayslipsAsync(_context, _emailService, _logger);
}
