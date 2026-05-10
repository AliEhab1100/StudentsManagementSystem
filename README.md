# Student Management System

ASP.NET Core MVC Student Management System built with Entity Framework Core and SQL Server.

---

## Project Overview

This project is a simple Student Management System that supports:

- Student attendance tracking
- Teacher attendance uploads using Excel files
- Student grade management
- Admin CRUD operations
- Login using Email + National ID
- Soft delete for Students and Teachers

---

# Technologies Used

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core
- SQL Server
- Bootstrap
- ClosedXML / Excel parsing
- Git & GitHub

---

# Roles

## Student
- Login using Email + National ID
- View profile information
- View attendance history
- View total grade

## Teacher
- Login using Email + National ID
- View profile
- Upload attendance Excel sheet
- Enter Course Title and Attendance Date

## Admin
- Login using Email + National ID
- Manage Students
- Manage Teachers
- Search Students/Teachers by ID
- Update student grades

---

# Database Schema

## Tables

### Students
- StudentId
- Name
- BirthDate
- Address
- Email
- NationalId
- TotalGrade
- IsDeleted
- DeletedAt

### Teachers
- TeacherId
- Name
- Address
- Email
- NationalId
- Position
- IsDeleted
- DeletedAt

### Admins
- AdminId
- Name
- Address
- Email
- NationalId

### AttendanceSessions
- SessionId
- TeacherId
- CourseTitle
- AttendanceDate
- UploadedFileName
- CreatedAt

### AttendanceRecords
- RecordId
- SessionId
- StudentId
- IsPresent

---

# Features

- Authentication without ASP.NET Identity
- Login using Email + National ID
- Student attendance retrieval using joins
- Teacher Excel attendance upload
- Admin CRUD operations
- Soft delete implementation
- SQL Server integration
- MVC architecture

---

# Soft Delete

Soft delete is applied only to:
- Students
- Teachers

Deleted rows are hidden instead of permanently removed.

---

# Project Structure

```text
Controllers/
Models/
Data/
Services/
ViewModels/
Views/
wwwroot/
