/*
DUMMY TRANSACTIONS SCRIPT FOR DASHBOARD CHART DATA

INSTRUCTIONS:
1. Open SQL Server Management Studio
2. Connect to your database
3. Select your database (tradeXdb)
4. Execute this script

This script will:
- Add dummy transactions from January to April 2024
- Use existing user and stock data
- Generate random transaction amounts and types
*/

-- Check if the database is using dbo schema or default schema
DECLARE @usersTableName NVARCHAR(100);
DECLARE @stocksTableName NVARCHAR(100);
DECLARE @transactionsTableName NVARCHAR(100);

-- Try to determine correct table names
SELECT TOP 1 @usersTableName = TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('Users', 'users', 'dbo.Users', 'AspNetUsers') 
  AND TABLE_TYPE = 'BASE TABLE';

SELECT TOP 1 @stocksTableName = TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('Stocks', 'stocks', 'dbo.Stocks') 
  AND TABLE_TYPE = 'BASE TABLE';

SELECT TOP 1 @transactionsTableName = TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('transactions', 'Transactions', 'dbo.Transactions') 
  AND TABLE_TYPE = 'BASE TABLE';

-- Verify we have all the tables we need
IF @usersTableName IS NULL OR @stocksTableName IS NULL OR @transactionsTableName IS NULL
BEGIN
    PRINT 'Required tables not found in the database.';
    PRINT 'Users table: ' + ISNULL(@usersTableName, 'NOT FOUND');
    PRINT 'Stocks table: ' + ISNULL(@stocksTableName, 'NOT FOUND');
    PRINT 'Transactions table: ' + ISNULL(@transactionsTableName, 'NOT FOUND');
    RETURN;
END

PRINT 'Using tables:';
PRINT '- Users: ' + @usersTableName;
PRINT '- Stocks: ' + @stocksTableName;
PRINT '- Transactions: ' + @transactionsTableName;

-- Dynamically create SQL to get first user ID
DECLARE @getUserSQL NVARCHAR(MAX) = N'
    SELECT TOP 1 @userId = user_id 
    FROM ' + @usersTableName + '
    ORDER BY user_id';

-- Execute the dynamic SQL with output parameter
DECLARE @userId INT;
EXEC sp_executesql @getUserSQL, N'@userId INT OUTPUT', @userId = @userId OUTPUT;

-- If no users exist, exit script
IF @userId IS NULL
BEGIN
    PRINT 'No users found in the database. Please create a user first.';
    RETURN;
END

PRINT 'Using User ID: ' + CAST(@userId AS VARCHAR);

-- Dynamically create SQL to get stock IDs
DECLARE @stockIds TABLE (stock_id INT);
DECLARE @getStocksSQL NVARCHAR(MAX) = N'
    INSERT INTO @stockIds
    SELECT stock_id 
    FROM ' + @stocksTableName;

-- Execute the dynamic SQL
EXEC sp_executesql @getStocksSQL;

-- If no stocks exist, exit script
IF NOT EXISTS (SELECT * FROM @stockIds)
BEGIN
    PRINT 'No stocks found in the database. Please add stocks first.';
    RETURN;
END

-- Variables for transaction generation
DECLARE @currentDate DATE = '2024-01-01';
DECLARE @endDate DATE = '2024-04-30';
DECLARE @transactionCount INT = 0;

-- Random selection variables
DECLARE @randomStockId INT;
DECLARE @randomQuantity INT;
DECLARE @randomPrice DECIMAL(18,2);
DECLARE @randomTransactionType VARCHAR(10);

-- Transaction types
DECLARE @transactionTypes TABLE (type VARCHAR(10));
INSERT INTO @transactionTypes VALUES ('buy'), ('sell'), ('addfunds');

PRINT 'Starting to generate dummy transactions';

-- Loop through each day from January to April
WHILE @currentDate <= @endDate
BEGIN
    -- Generate 1-3 transactions per day with 70% probability
    IF RAND() < 0.7
    BEGIN
        -- Random number of transactions per day (1-3)
        DECLARE @dailyTransactions INT = FLOOR(RAND() * 3) + 1;
        
        WHILE @dailyTransactions > 0
        BEGIN
            -- Select random stock
            SELECT TOP 1 @randomStockId = stock_id 
            FROM @stockIds
            ORDER BY NEWID();
            
            -- Generate random quantity (1-20)
            SET @randomQuantity = FLOOR(RAND() * 20) + 1;
            
            -- Generate random price between 10-500
            SET @randomPrice = ROUND((RAND() * 490) + 10, 2);
            
            -- Select random transaction type
            SELECT TOP 1 @randomTransactionType = type 
            FROM @transactionTypes 
            ORDER BY NEWID();
            
            -- For sell transactions, make the price positive
            -- For buy transactions, make the price negative
            IF @randomTransactionType = 'buy'
                SET @randomPrice = -@randomPrice;
            ELSE IF @randomTransactionType = 'sell'
                SET @randomPrice = ABS(@randomPrice);
            ELSE IF @randomTransactionType = 'addfunds'
            BEGIN
                -- For addfunds, use NULL for stock_id and set price as positive
                SET @randomStockId = NULL;
                SET @randomPrice = ABS(@randomPrice) * 2; -- Higher amounts for fund additions
            END;
            
            -- Dynamically create SQL for insert
            DECLARE @insertSQL NVARCHAR(MAX) = N'
                INSERT INTO ' + @transactionsTableName + ' (
                    user_id, 
                    stock_id, 
                    quantity, 
                    price, 
                    transaction_time, 
                    transaction_type
                )
                VALUES (
                    @userId,
                    @randomStockId,
                    @randomQuantity,
                    @randomPrice,
                    @transactionTime,
                    @randomTransactionType
                )';
            
            -- Execute the insert with parameters
            EXEC sp_executesql @insertSQL, 
                N'@userId INT, @randomStockId INT, @randomQuantity INT, @randomPrice DECIMAL(18,2), @transactionTime DATETIME, @randomTransactionType VARCHAR(10)',
                @userId, @randomStockId, @randomQuantity, @randomPrice, 
                DATEADD(hh, FLOOR(RAND() * 24), @currentDate),
                @randomTransactionType;
            
            SET @transactionCount = @transactionCount + 1;
            SET @dailyTransactions = @dailyTransactions - 1;
        END;
    END;
    
    -- Move to next day
    SET @currentDate = DATEADD(DAY, 1, @currentDate);
END;

PRINT 'Successfully added ' + CAST(@transactionCount AS VARCHAR) + ' dummy transactions';
PRINT 'Refresh your Admin Dashboard to see the updated chart!'; 