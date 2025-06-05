using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PetPal.API.Models;
using System;
using System.Threading.Tasks;

namespace PetPal.API.Data;

public static class DbInitializer
{
    public static async Task Initialize(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PetPalDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Apply migrations if they are not applied
            context.Database.Migrate();

            // Seed roles
            await SeedRoles(roleManager);

            // Seed admin user
            await SeedAdminUser(userManager, context);

            // Seed sample data
            await SeedSampleData(context, userManager);

            logger.LogInformation("Database initialized successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }

    private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Admin", "User", "Veterinarian" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedAdminUser(UserManager<IdentityUser> userManager, PetPalDbContext context)
    {
        // Check if admin user exists
        var adminEmail = "admin@petpal.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            // Create admin user
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");

            if (result.Succeeded)
            {
                // Add admin role
                await userManager.AddToRoleAsync(adminUser, "Admin");

                // Create admin user profile
                var adminProfile = new UserProfile
                {
                    FirstName = "Admin",
                    LastName = "User",
                    Email = adminEmail,
                    Address = "123 Admin St",
                    Phone = "555-123-4567",
                    IdentityUserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.UserProfiles.Add(adminProfile);
                await context.SaveChangesAsync();

                // Verify the admin profile was created
                var verifyProfile = await context.UserProfiles.FirstOrDefaultAsync(up => up.IdentityUserId == adminUser.Id);
                if (verifyProfile == null)
                {
                    // If profile creation failed, log it and try again
                    Console.WriteLine("WARNING: Admin profile creation failed on first attempt. Trying again...");

                    // Try creating the profile again
                    context.UserProfiles.Add(adminProfile);
                    await context.SaveChangesAsync();
                }
            }
        }
        else
        {
            // Verify admin user has a profile
            var adminProfile = await context.UserProfiles.FirstOrDefaultAsync(up => up.IdentityUserId == adminUser.Id);
            if (adminProfile == null)
            {
                // Create admin user profile if it doesn't exist
                adminProfile = new UserProfile
                {
                    FirstName = "Admin",
                    LastName = "User",
                    Email = adminEmail,
                    Address = "123 Admin St",
                    Phone = "555-123-4567",
                    IdentityUserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.UserProfiles.Add(adminProfile);
                await context.SaveChangesAsync();
                Console.WriteLine("Created missing admin profile for existing admin user.");
            }
        }
    }

    private static async Task SeedSampleData(PetPalDbContext context, UserManager<IdentityUser> userManager)
    {
        // Only seed sample data if the database is empty
        if (await context.Pets.AnyAsync() || await context.Veterinarians.AnyAsync())
        {
            return;
        }

        // Seed veterinarians
        var veterinarians = await SeedSampleVeterinarians(context);

        // Seed sample users and get their profiles
        var userProfiles = await SeedSampleUsers(context, userManager);

        // Seed pets and get the created pets
        var pets = await SeedSamplePets(context, userProfiles);

        // Seed pet owners (relationships between pets and users)
        await SeedSamplePetOwners(context, pets, userProfiles);

        // Seed health records
        await SeedSampleHealthRecords(context, pets, veterinarians);

        // Seed appointments
        await SeedSampleAppointments(context, pets, veterinarians);

        // Seed medications
        await SeedSampleMedications(context, pets, veterinarians);
    }

    private static async Task<List<Veterinarian>> SeedSampleVeterinarians(PetPalDbContext context)
    {
        var veterinarians = new List<Veterinarian>
        {
            new Veterinarian
            {
                FirstName = "John",
                LastName = "Smith",
                Email = "john.smith@petpal.com",
                Phone = "555-111-2222",
                Specialty = "General",
                ClinicName = "PetPal Clinic",
                Address = "123 Main St",
                LicenseNumber = "VET12345"
            },
            new Veterinarian
            {
                FirstName = "Sarah",
                LastName = "Johnson",
                Email = "sarah.johnson@petpal.com",
                Phone = "555-333-4444",
                Specialty = "Surgery",
                ClinicName = "PetPal Clinic",
                Address = "123 Main St",
                LicenseNumber = "VET67890"
            },
            new Veterinarian
            {
                FirstName = "Michael",
                LastName = "Chen",
                Email = "michael.chen@petpal.com",
                Phone = "555-555-6666",
                Specialty = "Dermatology",
                ClinicName = "PetPal Specialty Clinic",
                Address = "456 Oak Ave",
                LicenseNumber = "VET24680"
            },
            new Veterinarian
            {
                FirstName = "Emily",
                LastName = "Rodriguez",
                Email = "emily.rodriguez@petpal.com",
                Phone = "555-777-8888",
                Specialty = "Cardiology",
                ClinicName = "PetPal Specialty Clinic",
                Address = "456 Oak Ave",
                LicenseNumber = "VET13579"
            }
        };

        context.Veterinarians.AddRange(veterinarians);
        await context.SaveChangesAsync();

        return veterinarians;
    }

    private static async Task<List<UserProfile>> SeedSampleUsers(PetPalDbContext context, UserManager<IdentityUser> userManager)
    {
        var userProfiles = new List<UserProfile>();

        // Sample user 1
        var userEmail1 = "user@petpal.com";
        var sampleUser1 = await userManager.FindByEmailAsync(userEmail1);

        if (sampleUser1 == null)
        {
            sampleUser1 = new IdentityUser
            {
                UserName = userEmail1,
                Email = userEmail1,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(sampleUser1, "User123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(sampleUser1, "User");

                var userProfile1 = new UserProfile
                {
                    FirstName = "Sample",
                    LastName = "User",
                    Email = userEmail1,
                    Address = "456 User Ave",
                    Phone = "555-987-6543",
                    IdentityUserId = sampleUser1.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.UserProfiles.Add(userProfile1);
                await context.SaveChangesAsync();
                userProfiles.Add(userProfile1);
            }
        }
        else
        {
            var existingProfile = await context.UserProfiles.FirstOrDefaultAsync(up => up.IdentityUserId == sampleUser1.Id);
            if (existingProfile != null)
            {
                userProfiles.Add(existingProfile);
            }
        }

        // Sample user 2
        var userEmail2 = "jane@petpal.com";
        var sampleUser2 = await userManager.FindByEmailAsync(userEmail2);

        if (sampleUser2 == null)
        {
            sampleUser2 = new IdentityUser
            {
                UserName = userEmail2,
                Email = userEmail2,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(sampleUser2, "User123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(sampleUser2, "User");

                var userProfile2 = new UserProfile
                {
                    FirstName = "Jane",
                    LastName = "Doe",
                    Email = userEmail2,
                    Address = "789 Maple St",
                    Phone = "555-123-7890",
                    IdentityUserId = sampleUser2.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.UserProfiles.Add(userProfile2);
                await context.SaveChangesAsync();
                userProfiles.Add(userProfile2);
            }
        }
        else
        {
            var existingProfile = await context.UserProfiles.FirstOrDefaultAsync(up => up.IdentityUserId == sampleUser2.Id);
            if (existingProfile != null)
            {
                userProfiles.Add(existingProfile);
            }
        }

        return userProfiles;
    }

    private static async Task<List<Pet>> SeedSamplePets(PetPalDbContext context, List<UserProfile> userProfiles)
    {
        var allPets = new List<Pet>();

        if (userProfiles.Count > 0)
        {
            // Pets for first user
            var petsForUser1 = new List<Pet>
            {
                new Pet
                {
                    Name = "Buddy",
                    Species = "Dog",
                    Breed = "Golden Retriever",
                    DateOfBirth = DateTime.Now.AddYears(-3),
                    Weight = 70.5m,
                    Color = "Golden",
                    ImageUrl = "https://example.com/buddy.jpg",
                    MicrochipNumber = "CHIP123456"
                },
                new Pet
                {
                    Name = "Whiskers",
                    Species = "Cat",
                    Breed = "Siamese",
                    DateOfBirth = DateTime.Now.AddYears(-2),
                    Weight = 10.2m,
                    Color = "Cream",
                    ImageUrl = "https://example.com/whiskers.jpg",
                    MicrochipNumber = "CHIP789012"
                }
            };

            context.Pets.AddRange(petsForUser1);
            await context.SaveChangesAsync();
            allPets.AddRange(petsForUser1);
        }

        if (userProfiles.Count > 1)
        {
            // Pets for second user
            var petsForUser2 = new List<Pet>
            {
                new Pet
                {
                    Name = "Max",
                    Species = "Dog",
                    Breed = "German Shepherd",
                    DateOfBirth = DateTime.Now.AddYears(-4),
                    Weight = 85.0m,
                    Color = "Black and Tan",
                    ImageUrl = "https://example.com/max.jpg",
                    MicrochipNumber = "CHIP345678"
                },
                new Pet
                {
                    Name = "Luna",
                    Species = "Cat",
                    Breed = "Maine Coon",
                    DateOfBirth = DateTime.Now.AddYears(-1).AddMonths(-6),
                    Weight = 15.7m,
                    Color = "Tabby",
                    ImageUrl = "https://example.com/luna.jpg",
                    MicrochipNumber = "CHIP901234"
                },
                new Pet
                {
                    Name = "Tweety",
                    Species = "Bird",
                    Breed = "Canary",
                    DateOfBirth = DateTime.Now.AddMonths(-8),
                    Weight = 0.2m,
                    Color = "Yellow",
                    ImageUrl = "https://example.com/tweety.jpg",
                    MicrochipNumber = null
                }
            };

            context.Pets.AddRange(petsForUser2);
            await context.SaveChangesAsync();
            allPets.AddRange(petsForUser2);
        }

        return allPets;
    }

    private static async Task SeedSamplePetOwners(PetPalDbContext context, List<Pet> pets, List<UserProfile> userProfiles)
    {
        if (userProfiles.Count == 0 || pets.Count == 0)
        {
            return;
        }

        // Assign first two pets to first user as primary owner
        if (pets.Count >= 2 && userProfiles.Count >= 1)
        {
            for (int i = 0; i < 2; i++)
            {
                var petOwner = new PetOwner
                {
                    PetId = pets[i].Id,
                    UserProfileId = userProfiles[0].Id,
                    IsPrimaryOwner = true
                };
                context.PetOwners.Add(petOwner);
            }
        }

        // Assign remaining pets to second user as primary owner
        if (userProfiles.Count >= 2)
        {
            for (int i = 2; i < pets.Count; i++)
            {
                var petOwner = new PetOwner
                {
                    PetId = pets[i].Id,
                    UserProfileId = userProfiles[1].Id,
                    IsPrimaryOwner = true
                };
                context.PetOwners.Add(petOwner);
            }

            // Add secondary ownership - User 2 is secondary owner of User 1's first pet
            if (pets.Count >= 1)
            {
                var secondaryOwner = new PetOwner
                {
                    PetId = pets[0].Id,
                    UserProfileId = userProfiles[1].Id,
                    IsPrimaryOwner = false
                };
                context.PetOwners.Add(secondaryOwner);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedSampleHealthRecords(PetPalDbContext context, List<Pet> pets, List<Veterinarian> veterinarians)
    {
        if (pets.Count == 0 || veterinarians.Count == 0)
        {
            return;
        }

        var healthRecords = new List<HealthRecord>();

        // Health records for first pet
        if (pets.Count >= 1 && veterinarians.Count >= 1)
        {
            healthRecords.AddRange(new List<HealthRecord>
            {
                new HealthRecord
                {
                    PetId = pets[0].Id,
                    RecordType = "Vaccination",
                    Description = "Rabies Vaccination",
                    RecordDate = DateTime.Now.AddMonths(-6),
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "Annual vaccination completed"
                },
                new HealthRecord
                {
                    PetId = pets[0].Id,
                    RecordType = "Vaccination",
                    Description = "Distemper Vaccination",
                    RecordDate = DateTime.Now.AddMonths(-6),
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "Booster required in 3 weeks"
                },
                new HealthRecord
                {
                    PetId = pets[0].Id,
                    RecordType = "Surgery",
                    Description = "Neutering",
                    RecordDate = DateTime.Now.AddYears(-2),
                    VeterinarianId = veterinarians[1].Id,
                    Notes = "Routine procedure, recovered well"
                }
            });
        }

        // Health records for second pet
        if (pets.Count >= 2 && veterinarians.Count >= 2)
        {
            healthRecords.AddRange(new List<HealthRecord>
            {
                new HealthRecord
                {
                    PetId = pets[1].Id,
                    RecordType = "Check-up",
                    Description = "Annual Check-up",
                    RecordDate = DateTime.Now.AddMonths(-2),
                    VeterinarianId = veterinarians[1].Id,
                    Notes = "All looks good"
                },
                new HealthRecord
                {
                    PetId = pets[1].Id,
                    RecordType = "Dental",
                    Description = "Dental Cleaning",
                    RecordDate = DateTime.Now.AddMonths(-2),
                    VeterinarianId = veterinarians[1].Id,
                    Notes = "Minor tartar buildup"
                }
            });
        }

        // Health records for third pet
        if (pets.Count >= 3 && veterinarians.Count >= 3)
        {
            healthRecords.AddRange(new List<HealthRecord>
            {
                new HealthRecord
                {
                    PetId = pets[2].Id,
                    RecordType = "Vaccination",
                    Description = "Rabies Vaccination",
                    RecordDate = DateTime.Now.AddMonths(-1),
                    VeterinarianId = veterinarians[2].Id,
                    Notes = "Initial vaccination"
                },
                new HealthRecord
                {
                    PetId = pets[2].Id,
                    RecordType = "Treatment",
                    Description = "Ear Infection",
                    RecordDate = DateTime.Now.AddMonths(-3),
                    VeterinarianId = veterinarians[2].Id,
                    Notes = "Prescribed antibiotics for 10 days"
                }
            });
        }

        context.HealthRecords.AddRange(healthRecords);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSampleAppointments(PetPalDbContext context, List<Pet> pets, List<Veterinarian> veterinarians)
    {
        if (pets.Count == 0 || veterinarians.Count == 0)
        {
            return;
        }

        var appointments = new List<Appointment>();

        // Appointments for first pet
        if (pets.Count >= 1 && veterinarians.Count >= 1)
        {
            appointments.AddRange(new List<Appointment>
            {
                new Appointment
                {
                    PetId = pets[0].Id,
                    VeterinarianId = veterinarians[0].Id,
                    AppointmentDate = DateTime.Now.AddDays(7),
                    AppointmentTime = new TimeSpan(14, 30, 0), // 2:30 PM
                    AppointmentType = "Check-up",
                    Notes = "Annual check-up",
                    Status = "Scheduled"
                },
                new Appointment
                {
                    PetId = pets[0].Id,
                    VeterinarianId = veterinarians[0].Id,
                    AppointmentDate = DateTime.Now.AddDays(-14),
                    AppointmentTime = new TimeSpan(10, 0, 0), // 10:00 AM
                    AppointmentType = "Emergency",
                    Notes = "Limping on right front paw",
                    Status = "Completed"
                }
            });
        }

        // Appointments for second pet
        if (pets.Count >= 2 && veterinarians.Count >= 2)
        {
            appointments.AddRange(new List<Appointment>
            {
                new Appointment
                {
                    PetId = pets[1].Id,
                    VeterinarianId = veterinarians[1].Id,
                    AppointmentDate = DateTime.Now.AddDays(14),
                    AppointmentTime = new TimeSpan(10, 0, 0), // 10:00 AM
                    AppointmentType = "Vaccination",
                    Notes = "Booster shots",
                    Status = "Scheduled"
                }
            });
        }

        // Appointments for third pet
        if (pets.Count >= 3 && veterinarians.Count >= 3)
        {
            appointments.AddRange(new List<Appointment>
            {
                new Appointment
                {
                    PetId = pets[2].Id,
                    VeterinarianId = veterinarians[2].Id,
                    AppointmentDate = DateTime.Now.AddDays(3),
                    AppointmentTime = new TimeSpan(15, 45, 0), // 3:45 PM
                    AppointmentType = "Follow-up",
                    Notes = "Follow-up for ear infection",
                    Status = "Scheduled"
                },
                new Appointment
                {
                    PetId = pets[2].Id,
                    VeterinarianId = veterinarians[3].Id,
                    AppointmentDate = DateTime.Now.AddDays(21),
                    AppointmentTime = new TimeSpan(11, 15, 0), // 11:15 AM
                    AppointmentType = "Consultation",
                    Notes = "Cardiology consultation",
                    Status = "Scheduled"
                }
            });
        }

        context.Appointments.AddRange(appointments);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSampleMedications(PetPalDbContext context, List<Pet> pets, List<Veterinarian> veterinarians)
    {
        if (pets.Count == 0 || veterinarians.Count == 0)
        {
            return;
        }

        var medications = new List<Medication>();

        // Medications for first pet
        if (pets.Count >= 1 && veterinarians.Count >= 1)
        {
            medications.AddRange(new List<Medication>
            {
                new Medication
                {
                    PetId = pets[0].Id,
                    Name = "Heartworm Prevention",
                    Dosage = "1 tablet",
                    Frequency = "Monthly",
                    StartDate = DateTime.Now.AddMonths(-3),
                    EndDate = null, // Ongoing
                    Instructions = "Give with food",
                    Prescriber = veterinarians[0].FirstName + " " + veterinarians[0].LastName,
                    IsActive = true
                },
                new Medication
                {
                    PetId = pets[0].Id,
                    Name = "Joint Supplement",
                    Dosage = "2 tablets",
                    Frequency = "Daily",
                    StartDate = DateTime.Now.AddMonths(-1),
                    EndDate = null, // Ongoing
                    Instructions = "Give with breakfast",
                    Prescriber = veterinarians[0].FirstName + " " + veterinarians[0].LastName,
                    IsActive = true
                }
            });
        }

        // Medications for second pet
        if (pets.Count >= 2 && veterinarians.Count >= 2)
        {
            medications.AddRange(new List<Medication>
            {
                new Medication
                {
                    PetId = pets[1].Id,
                    Name = "Flea Treatment",
                    Dosage = "1 application",
                    Frequency = "Monthly",
                    StartDate = DateTime.Now.AddMonths(-2),
                    EndDate = null, // Ongoing
                    Instructions = "Apply to back of neck",
                    Prescriber = veterinarians[1].FirstName + " " + veterinarians[1].LastName,
                    IsActive = true
                }
            });
        }

        // Medications for third pet
        if (pets.Count >= 3 && veterinarians.Count >= 3)
        {
            medications.AddRange(new List<Medication>
            {
                new Medication
                {
                    PetId = pets[2].Id,
                    Name = "Antibiotic",
                    Dosage = "1 tablet",
                    Frequency = "Twice daily",
                    StartDate = DateTime.Now.AddDays(-10),
                    EndDate = DateTime.Now.AddDays(4), // 14-day course
                    Instructions = "Give with food to prevent upset stomach",
                    Prescriber = veterinarians[2].FirstName + " " + veterinarians[2].LastName,
                    IsActive = true
                },
                new Medication
                {
                    PetId = pets[2].Id,
                    Name = "Anti-inflammatory",
                    Dosage = "0.5 tablet",
                    Frequency = "Once daily",
                    StartDate = DateTime.Now.AddDays(-10),
                    EndDate = DateTime.Now.AddDays(-3), // 7-day course
                    Instructions = "Give with food",
                    Prescriber = veterinarians[2].FirstName + " " + veterinarians[2].LastName,
                    IsActive = false // Completed
                }
            });
        }

        context.Medications.AddRange(medications);
        await context.SaveChangesAsync();
    }
}