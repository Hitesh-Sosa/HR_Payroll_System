using HrPayrollSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HrPayrollSystem.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();

        var roleManager =
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var context =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

       
        await context.Database.MigrateAsync();

        string[] roles =
        {
            "Admin",
            "HR",
            "Manager",
            "Employee"
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }


        const string adminEmail = "admin123@gmail.com";
        const string adminPassword = "Sosa@123";

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(
                admin,
                adminPassword
            );

            if (!result.Succeeded)
            {
                throw new Exception(
                    "Admin creation failed: " +
                    string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description)
                    )
                );
            }
        }

        
        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
         

        // 3. IT DEPARTMENT
        var department = await context.Departments
            .FirstOrDefaultAsync(d => d.Name == "IT");

        if (department == null)
        {
            department = new Department
            {
                Name = "IT"
            };

            context.Departments.Add(department);

            await context.SaveChangesAsync();
        }


         
        // 4. EMP001 EMPLOYEE
         
        var employee = await context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeCode == "EMP001");

        if (employee == null)
        {
            employee = new Employee
            {
                ApplicationUserId = admin.Id,
                EmployeeCode = "EMP001",
                FullName = "System Admin",
                Designation = "Administrator",
                JoiningDate = new DateTime(2024, 1, 1),
                GrossMonthlySalary = 50000m,
                DepartmentId = department.Id,
                IsActive = true
            };

            context.Employees.Add(employee);

            await context.SaveChangesAsync();
        }
    }
}