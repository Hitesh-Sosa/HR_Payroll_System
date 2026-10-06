using HrPayrollSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HrPayrollSystem.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Department> Departments { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<LeaveBalance> LeaveBalances { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<SalaryStructure> SalaryStructures { get; set; }
    public DbSet<Payslip> Payslips { get; set; }
    public DbSet<PayslipItem> PayslipItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Employee>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.ApplicationUserId);

        builder.Entity<Attendance>()
            .HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique();

        builder.Entity<Employee>()
            .Property(e => e.GrossMonthlySalary).HasPrecision(18, 2);

        builder.Entity<Attendance>()
            .Property(a => a.HoursWorked).HasPrecision(5, 2);

        builder.Entity<LeaveBalance>()
            .HasIndex(lb => new { lb.EmployeeId, lb.LeaveTypeId, lb.Year })
            .IsUnique();

        builder.Entity<SalaryStructure>()
            .HasIndex(s => s.EmployeeId)
            .IsUnique();

        builder.Entity<SalaryStructure>().Property(s => s.Basic).HasPrecision(18, 2);
        builder.Entity<SalaryStructure>().Property(s => s.HRA).HasPrecision(18, 2);
        builder.Entity<SalaryStructure>().Property(s => s.Conveyance).HasPrecision(18, 2);
        builder.Entity<SalaryStructure>().Property(s => s.SpecialAllowance).HasPrecision(18, 2);

        builder.Entity<LeaveBalance>().Property(lb => lb.RemainingDays).HasPrecision(5, 2);
        builder.Entity<LeaveRequest>().Property(lr => lr.NumberOfDays).HasPrecision(5, 2);

        builder.Entity<Holiday>()
            .HasIndex(h => h.Date)
            .IsUnique();

        // ===== NEW in S9 =====

        // One payslip per employee per month — double-run impossible at DB level
        builder.Entity<Payslip>()
            .HasIndex(p => new { p.EmployeeId, p.Year, p.Month })
            .IsUnique();

        builder.Entity<Payslip>().Property(p => p.LopDays).HasPrecision(5, 2);
        builder.Entity<Payslip>().Property(p => p.PaidDays).HasPrecision(5, 2);
        builder.Entity<Payslip>().Property(p => p.EarnedBasic).HasPrecision(18, 2);
        builder.Entity<Payslip>().Property(p => p.EarnedHra).HasPrecision(18, 2);
        builder.Entity<Payslip>().Property(p => p.EarnedConveyance).HasPrecision(18, 2);
        builder.Entity<Payslip>().Property(p => p.EarnedSpecial).HasPrecision(18, 2);
        builder.Entity<Payslip>().Property(p => p.EarnedGross).HasPrecision(18, 2);
        builder.Entity<Payslip>().Property(p => p.PfDeduction).HasPrecision(18, 2);
        builder.Entity<Payslip>().Property(p => p.ProfessionalTax).HasPrecision(18, 2);
        builder.Entity<Payslip>().Property(p => p.TotalDeductions).HasPrecision(18, 2);
        builder.Entity<Payslip>().Property(p => p.NetPay).HasPrecision(18, 2);

        builder.Entity<PayslipItem>()
            .Property(i => i.Amount).HasPrecision(18, 2);
    }
}