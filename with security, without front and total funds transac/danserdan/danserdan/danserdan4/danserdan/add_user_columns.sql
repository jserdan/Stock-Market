-- Check if firstName column exists, if not add it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'firstName')
BEGIN
    ALTER TABLE users ADD firstName NVARCHAR(50) NOT NULL DEFAULT '';
END

-- Check if lastName column exists, if not add it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'lastName')
BEGIN
    ALTER TABLE users ADD lastName NVARCHAR(50) NOT NULL DEFAULT '';
END

-- Update the migration history to mark the migration as applied
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20250505201909_UseFirstNameLastNameDirectly')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20250505201909_UseFirstNameLastNameDirectly', '7.0.0');
END
