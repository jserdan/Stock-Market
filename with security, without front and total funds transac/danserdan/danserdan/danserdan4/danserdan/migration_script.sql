IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [users] (
    [user_id] int NOT NULL IDENTITY,
    [username] nvarchar(max) NOT NULL,
    [email] nvarchar(max) NOT NULL,
    [password_hash] nvarchar(max) NOT NULL,
    [balance] decimal(18,2) NOT NULL,
    [created_at] datetime2 NOT NULL,
    CONSTRAINT [PK_users] PRIMARY KEY ([user_id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250423031953_migratenabayawa', N'7.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[users]') AND [c].[name] = N'username');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [users] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [users] ALTER COLUMN [username] nvarchar(50) NOT NULL;
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[users]') AND [c].[name] = N'password_hash');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [users] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [users] ALTER COLUMN [password_hash] nvarchar(100) NOT NULL;
GO

CREATE TABLE [Stocks] (
    [stock_id] int NOT NULL IDENTITY,
    [symbol] nvarchar(max) NOT NULL,
    [company_name] nvarchar(max) NOT NULL,
    [market_price] decimal(18,2) NOT NULL,
    [last_updated] datetime2 NOT NULL,
    [open_price] decimal(18,2) NULL,
    [open_price_time] datetime2 NULL,
    CONSTRAINT [PK_Stocks] PRIMARY KEY ([stock_id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250505051408_AddOpenPriceFieldsToStocks', N'7.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250505052752_SeedInitialStocks', N'7.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250505054731_AddUserFields', N'7.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[users]') AND [c].[name] = N'balance');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [users] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [users] ALTER COLUMN [balance] decimal(18,2) NULL;
GO

ALTER TABLE [users] ADD [first_name] nvarchar(50) NOT NULL DEFAULT N'';
GO

ALTER TABLE [users] ADD [last_name] nvarchar(50) NOT NULL DEFAULT N'';
GO

CREATE TABLE [transactions] (
    [transaction_id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [stock_symbol] nvarchar(max) NOT NULL,
    [amount] decimal(18,2) NOT NULL,
    [quantity] int NOT NULL,
    [transaction_type] nvarchar(max) NOT NULL,
    [transaction_date] datetime2 NOT NULL,
    CONSTRAINT [PK_transactions] PRIMARY KEY ([transaction_id]),
    CONSTRAINT [FK_transactions_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([user_id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_transactions_user_id] ON [transactions] ([user_id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250505173511_AddTransactionsTable', N'7.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250505201604_AddFirstNameLastNameColumns', N'7.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

EXEC sp_rename N'[users].[last_name]', N'lastName', N'COLUMN';
GO

EXEC sp_rename N'[users].[first_name]', N'firstName', N'COLUMN';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250505201909_UseFirstNameLastNameDirectly', N'7.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250509143315_FixTransactionColumns', N'7.0.0');
GO

COMMIT;
GO

