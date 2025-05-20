-- Add sector column to Stocks table if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Stocks' AND COLUMN_NAME = 'sector'
)
BEGIN
    ALTER TABLE Stocks ADD sector nvarchar(max) NULL;
    PRINT 'Added sector column to Stocks table';
END
ELSE
BEGIN
    PRINT 'sector column already exists in Stocks table';
END

-- Update the migration history to reflect our changes
IF EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20250512074906_AddSectorColumnToStocks')
BEGIN
    PRINT 'Migration 20250512074906_AddSectorColumnToStocks already exists in __EFMigrationsHistory table';
END
ELSE
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20250512074906_AddSectorColumnToStocks', '8.0.5');
    PRINT 'Added migration record to __EFMigrationsHistory table';
END

-- Insert the new migration we just created
IF EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20250512135301_AddSectorToStocks')
BEGIN
    PRINT 'Migration 20250512135301_AddSectorToStocks already exists in __EFMigrationsHistory table';
END
ELSE
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20250512135301_AddSectorToStocks', '8.0.5');
    PRINT 'Added migration record to __EFMigrationsHistory table';
END
