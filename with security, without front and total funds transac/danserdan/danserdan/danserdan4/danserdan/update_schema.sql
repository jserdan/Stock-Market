USE [tradeXdb];
GO

-- Add IsAvailable column to Stocks table with default value of true
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Stocks]') AND name = 'IsAvailable')
BEGIN
    ALTER TABLE [dbo].[Stocks]
    ADD [IsAvailable] BIT NOT NULL DEFAULT 1;
    
    PRINT 'Added IsAvailable column to Stocks table';
END
ELSE
BEGIN
    PRINT 'IsAvailable column already exists in Stocks table';
END

-- Add transaction_type column to transactions table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[transactions]') AND name = 'transaction_type')
BEGIN
    ALTER TABLE [dbo].[transactions]
    ADD [transaction_type] NVARCHAR(50) NULL;
    
    PRINT 'Added transaction_type column to transactions table';
END
ELSE
BEGIN
    PRINT 'transaction_type column already exists in transactions table';
END

-- Make stock_id column nullable in transactions table
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[transactions]') AND name = 'stock_id' AND is_nullable = 0)
BEGIN
    -- Drop any foreign key constraints first
    DECLARE @constraintName NVARCHAR(200);
    
    SELECT @constraintName = name
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'[dbo].[transactions]')
    AND referenced_object_id = OBJECT_ID(N'[dbo].[Stocks]');
    
    IF @constraintName IS NOT NULL
    BEGIN
        DECLARE @sql NVARCHAR(500) = N'ALTER TABLE [dbo].[transactions] DROP CONSTRAINT ' + @constraintName;
        EXEC sp_executesql @sql;
        PRINT 'Dropped foreign key constraint: ' + @constraintName;
    END
    
    -- Alter the column to be nullable
    ALTER TABLE [dbo].[transactions]
    ALTER COLUMN [stock_id] INT NULL;
    
    PRINT 'Modified stock_id column in transactions table to be nullable';
END
ELSE
BEGIN
    PRINT 'stock_id column is already nullable in transactions table';
END
