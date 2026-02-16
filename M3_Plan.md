# Milestone 3 Plan: Local Persistence with SQLite

## Overview
This milestone focuses on implementing local persistence for the Mac MD Windows application using SQLite database integration. This will allow users to save, load, and manage documents locally on their Windows machine.

## Objectives
1. Implement SQLite database schema for documents, projects, and tags
2. Create data access layer with CRUD operations
3. Integrate database with existing application models
4. Ensure data persistence across application sessions
5. Add proper error handling and connection management

## Key Components

### 3.1 Database Schema Design
- **Documents table**: Store markdown content, metadata (title, modified date, etc.)
- **Projects table**: Organize documents into projects/groups
- **Tags table**: Categorize documents with tags
- **Document-Tag relationship**: Many-to-many relationship between documents and tags
- **Project-Document relationship**: Many-to-many relationship between projects and documents

### 3.2 Data Access Services
- Create `DatabaseService` for database operations
- Implement repository pattern for each entity type
- Add connection management and error handling
- Ensure thread safety for database operations

### 3.3 Integration Points
- Connect database service to existing `Document`, `Project`, `Tag` models
- Implement data loading at application startup
- Add data saving capabilities to editor
- Create database initialization and migration logic

### 3.4 Testing Requirements
- Unit tests for database service methods
- Integration tests for CRUD operations
- Data persistence tests across application sessions
- Edge case testing (null values, constraint violations, etc.)

## Implementation Steps

### Step 1: Setup Database Dependencies
- Add SQLite NuGet packages (`Microsoft.Data.Sqlite`)
- Configure database connection string
- Create database initialization logic

### Step 2: Define Database Schema
- Create migration scripts for all tables
- Define relationships between entities
- Add indexes for performance

### Step 3: Implement Data Access Layer
- Create repository classes for each entity
- Implement CRUD operations
- Add data validation and error handling

### Step 4: Integrate with Application
- Update `MainWindow` to load data on startup
- Connect `EditorViewModel` to save data
- Implement data synchronization between UI and database

### Step 5: Testing and Validation
- Add comprehensive unit and integration tests
- Validate data persistence and retrieval
- Test error scenarios and edge cases

## Expected Outcome
By the end of this milestone, the application should be able to:
- Create, read, update, and delete documents in local SQLite database
- Load documents at application startup
- Save document changes to database
- Organize documents into projects and tag them for better management
- Maintain data integrity and handle errors gracefully

## Dependencies
- `Microsoft.Data.Sqlite` NuGet package
- Existing `Document`, `Project`, `Tag` models from Core
- Application's existing MVVM architecture