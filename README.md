# PetPal - Pet Management System

PetPal is a comprehensive pet management system designed to help pet owners keep track of their pets' information, health records, medications, appointments, and more. This application provides a user-friendly interface for managing all aspects of pet care.

## 🐾 Project Overview

PetPal allows pet owners to store and manage detailed information about their pets, including:
- Basic pet details (name, species, breed, etc.)
- Health records and veterinary visits
- Medication tracking
- Appointment scheduling
- Multi-owner pet management

The system is built with a .NET Core backend API and is designed to be used with a frontend client application (not included in this repository).

## Features

- **User Authentication**: Secure registration and login system
- **Pet Management**: Add, view, update, and delete pets
- **Multi-Owner Support**: Share pet profiles with family members or co-owners
- **Health Records**: Track veterinary visits and health information
- **Medication Management**: Record and track pet medications
- **Appointment Scheduling**: Manage veterinary and other pet-related appointments
- **Role-Based Access Control**: Admin and regular user roles with appropriate permissions

## Technologies Used

- **Backend**: .NET 8.0, ASP.NET Core
- **API Style**: Minimal API
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core
- **Authentication**: ASP.NET Identity with cookie authentication
- **Object Mapping**: AutoMapper
- **Data Format**: JSON

## Getting Started

📝 If you are on a Mac and are running AirPlay, it defaults to port 5000, which is what this API will run on. Disable AirPlay while running the API.

### Cloning the Repository

Clone the project to the directory of your choice, and then:

```sh
cd pet-pal-server
```

### Setting Up the Database

1. Create a connection string user secret. Remember to modify it by putting your password in there before running the commands.
   ```sh
   dotnet user-secrets init
   dotnet user-secrets set 'ConnectionStrings:PetPalDbConnectionString' 'Host=localhost;Port=5432;Username=postgres;Password=your_password;Database=PetPal'
   ```

2. Apply the database migrations:
   ```sh
   cd PetPal.API
   dotnet ef database update
   ```

### Running the API

There is a `launch.json` and `tasks.json` file already in the repostitory, so you can immediately start the program in debug mode.

1. The API will be available at `http://localhost:5000`

## API Endpoints

The API provides the following main endpoint groups:

### Authentication
- `POST /auth/register` - Register a new user
- `POST /auth/login` - Log in a user
- `POST /auth/logout` - Log out a user

### Pets
- `GET /user/pets` - Get all pets for the current user
- `GET /pets/{id}` - Get a specific pet by ID
- `POST /pets` - Create a new pet
- `PUT /pets/{id}` - Update a pet
- `DELETE /pets/{id}` - Delete a pet
- `POST /pets/{petId}/owners` - Add an owner to a pet
- `DELETE /pets/{petId}/owners/{ownerId}` - Remove an owner from a pet

### Health Records
- `GET /pets/{petId}/healthrecords` - Get all health records for a pet
- `GET /healthrecords/{id}` - Get a specific health record
- `POST /pets/{petId}/healthrecords` - Create a new health record
- `PUT /healthrecords/{id}` - Update a health record
- `DELETE /healthrecords/{id}` - Delete a health record

### Appointments
- `GET /pets/{petId}/appointments` - Get all appointments for a pet
- `GET /appointments/{id}` - Get a specific appointment
- `POST /pets/{petId}/appointments` - Create a new appointment
- `PUT /appointments/{id}` - Update an appointment
- `DELETE /appointments/{id}` - Delete an appointment


## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
