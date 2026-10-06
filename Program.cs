using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HrPayrollSystem.Data;
using HrPayrollSystem.Services;
using HrPayrollSystem.Repositories;
using HrPayrollSystem.BackgroundJobs;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. Database Configuration
// ============================================

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found."
    );

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
 

// ============================================
// 2. Identity Configuration
// ============================================

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Development: do not require email confirmation
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();


// ============================================
// 3. Attendance Service - Dependency Injection
// ============================================

 
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<PayslipPdfService>();
builder.Services.AddScoped<RecurringJobs>();


// ===== Hangfire (background jobs) =====
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// Email options (from appsettings.json)
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();




// ============================================
// 4. MVC Configuration
// ============================================

builder.Services.AddControllersWithViews();


// ============================================
// 5. Build Application
// ============================================

var app = builder.Build();


// ============================================
// 6. Seed Database
// ============================================

// Creates roles, admin user, department, employee,
// leave types and leave balances if they are missing.
await DbSeeder.SeedAsync(
    app.Services.GetRequiredService<IServiceScopeFactory>()
);


// ============================================
// 7. HTTP Request Pipeline
// ============================================

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");

    // Enable HSTS for production
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();


// ============================================
// 8. Authentication & Authorization
// ============================================

app.UseAuthentication();

app.UseAuthorization();


// ===== Hangfire dashboard (local dev: open access) =====
app.UseHangfireDashboard("/hangfire");

// ===== Recurring jobs (Hangfire activates RecurringJobs via DI) =====
RecurringJob.AddOrUpdate<RecurringJobs>(
    "mark-yesterday-absents",
    x => x.MarkYesterdayAbsents(),
    "30 23 * * *");

RecurringJob.AddOrUpdate<RecurringJobs>(
    "email-monthly-payslips",
    x => x.EmailMonthlyPayslips(),
    "0 9 2 * *");

// ============================================
// 9. Area Routing
// ============================================

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);


// ============================================
// 10. Default MVC Routing
// ============================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);


// ============================================
// 11. Razor Pages / Identity
// ============================================

app.MapRazorPages();


// ============================================
// 12. Run Application
// ============================================

app.Run();