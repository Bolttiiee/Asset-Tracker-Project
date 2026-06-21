-- Migration Script: Update Existing Data for Compliance
-- Run this script AFTER deploying the new code changes
-- This ensures existing data is compatible with new validation rules

USE AssetTrackerDB;
GO

-- ========================================
-- 1. Add "Cancelled" status to existing Checkouts if needed
-- ========================================
PRINT 'Step 1: Verifying Checkout statuses...';

-- Check if any checkouts have invalid statuses
SELECT CheckoutID, Status 
FROM Checkouts 
WHERE Status NOT IN ('Active', 'Returned', 'Cancelled', 'Overdue');

-- No updates needed - Checkout model already supports these statuses
GO

-- ========================================
-- 2. Verify Asset Statuses
-- ========================================
PRINT 'Step 2: Verifying Asset statuses...';

-- Check for any invalid asset statuses
SELECT AssetID, Name, Status 
FROM Assets 
WHERE Status NOT IN ('Available', 'Checked Out', 'Under Maintenance', 'Decommissioned');

-- If any invalid statuses found, you may need to update them manually
-- Example:
-- UPDATE Assets SET Status = 'Available' WHERE Status = 'Invalid Status Name';
GO

-- ========================================
-- 3. Clean up any potential data inconsistencies
-- ========================================
PRINT 'Step 3: Checking for data inconsistencies...';

-- Find assets marked as "Checked Out" but with no active checkout record
SELECT a.AssetID, a.Name, a.Status, a.AssignedTo
FROM Assets a
LEFT JOIN Checkouts c ON a.AssetID = c.AssetID AND c.Status = 'Active'
WHERE a.Status = 'Checked Out' 
  AND c.CheckoutID IS NULL;

-- OPTIONAL: Automatically fix these inconsistencies
-- Uncomment the following lines if you want to auto-fix
-- UPDATE Assets 
-- SET Status = 'Available', AssignedTo = NULL
-- WHERE AssetID IN (
--     SELECT a.AssetID
--     FROM Assets a
--     LEFT JOIN Checkouts c ON a.AssetID = c.AssetID AND c.Status = 'Active'
--     WHERE a.Status = 'Checked Out' AND c.CheckoutID IS NULL
-- );
GO

-- ========================================
-- 4. Verify ExpectedReturnDate is set for all active checkouts
-- ========================================
PRINT 'Step 4: Checking ExpectedReturnDate values...';

-- Find any active checkouts with NULL ExpectedReturnDate
SELECT CheckoutID, UserFullName, CheckoutTimestamp, ExpectedReturnDate
FROM Checkouts
WHERE Status = 'Active' 
  AND ExpectedReturnDate IS NULL;

-- OPTIONAL: Set default return date (14 days from checkout) for NULL values
-- Uncomment if you want to auto-fix
-- UPDATE Checkouts
-- SET ExpectedReturnDate = DATEADD(day, 14, CheckoutTimestamp)
-- WHERE Status = 'Active' 
--   AND ExpectedReturnDate IS NULL;
GO

-- ========================================
-- 5. Identify potential overdue checkouts
-- ========================================
PRINT 'Step 5: Identifying overdue checkouts...';

SELECT 
    c.CheckoutID,
    c.UserFullName,
    c.UserEmail,
    a.Name AS AssetName,
    a.SerialNumber,
    c.CheckoutTimestamp,
    c.ExpectedReturnDate,
    DATEDIFF(day, c.ExpectedReturnDate, GETUTCDATE()) AS DaysOverdue
FROM Checkouts c
INNER JOIN Assets a ON c.AssetID = a.AssetID
WHERE c.Status = 'Active'
  AND c.ExpectedReturnDate < CAST(GETUTCDATE() AS DATE)
ORDER BY DaysOverdue DESC;

-- These will be automatically handled by the OverdueReminderService
GO

-- ========================================
-- 6. Check for duplicate serial numbers
-- ========================================
PRINT 'Step 6: Checking for duplicate serial numbers...';

SELECT 
    SerialNumber, 
    COUNT(*) AS DuplicateCount,
    STRING_AGG(CAST(AssetID AS VARCHAR), ', ') AS AffectedAssetIDs
FROM Assets
GROUP BY SerialNumber
HAVING COUNT(*) > 1;

-- If duplicates found, you'll need to manually resolve them
-- Suggestion: Rename one of the serial numbers (e.g., add "-2" suffix)
-- Example:
-- UPDATE Assets 
-- SET SerialNumber = SerialNumber + '-DUPLICATE-' + CAST(AssetID AS VARCHAR)
-- WHERE AssetID = [affected asset ID];
GO

-- ========================================
-- 7. Verify Audit Trail logging
-- ========================================
PRINT 'Step 7: Checking recent audit trail entries...';

SELECT TOP 10
    Action,
    AssetName,
    PerformedByEmail,
    Timestamp,
    Details
FROM AuditTrail
ORDER BY Timestamp DESC;

-- Audit trail should contain recent entries
GO

-- ========================================
-- 8. Summary Report
-- ========================================
PRINT 'Step 8: Generating summary report...';

SELECT 'Total Assets' AS Metric, COUNT(*) AS Value FROM Assets
UNION ALL
SELECT 'Available Assets', COUNT(*) FROM Assets WHERE Status = 'Available'
UNION ALL
SELECT 'Checked Out Assets', COUNT(*) FROM Assets WHERE Status = 'Checked Out'
UNION ALL
SELECT 'Under Maintenance', COUNT(*) FROM Assets WHERE Status = 'Under Maintenance'
UNION ALL
SELECT 'Decommissioned Assets', COUNT(*) FROM Assets WHERE Status = 'Decommissioned'
UNION ALL
SELECT 'Active Checkouts', COUNT(*) FROM Checkouts WHERE Status = 'Active'
UNION ALL
SELECT 'Overdue Checkouts', COUNT(*) 
FROM Checkouts 
WHERE Status = 'Active' AND ExpectedReturnDate < CAST(GETUTCDATE() AS DATE)
UNION ALL
SELECT 'Total Audit Trail Entries', COUNT(*) FROM AuditTrail;

PRINT '';
PRINT '========================================';
PRINT 'Migration Check Complete!';
PRINT 'Review the results above for any issues.';
PRINT '========================================';
GO

-- ========================================
-- 9. Test Queries for New Features
-- ========================================

-- Test available assets endpoint query
PRINT 'Test Query 1: Available Assets (used by /api/checkouts/available-assets)';
SELECT 
    AssetID,
    Name,
    Category,
    SerialNumber,
    Status,
    Value,
    PurchaseDate
FROM Assets
WHERE Status = 'Available'
ORDER BY Category, Name;
GO

-- Test overdue checkouts query
PRINT 'Test Query 2: Overdue Checkouts (used by /api/checkouts/overdue)';
SELECT 
    c.CheckoutID,
    c.AssetID,
    a.Name AS AssetName,
    a.SerialNumber,
    a.Category,
    c.UserFullName,
    c.UserEmail,
    c.CheckoutTimestamp,
    c.ExpectedReturnDate,
    DATEDIFF(day, c.ExpectedReturnDate, CAST(GETUTCDATE() AS DATE)) AS DaysOverdue
FROM Checkouts c
INNER JOIN Assets a ON c.AssetID = a.AssetID
WHERE c.Status = 'Active' 
  AND c.ExpectedReturnDate < CAST(GETUTCDATE() AS DATE)
ORDER BY DaysOverdue DESC;
GO

-- Test duplicate checkout check
PRINT 'Test Query 3: Duplicate Checkout Detection';
SELECT 
    c1.CheckoutID AS FirstCheckout,
    c2.CheckoutID AS SecondCheckout,
    c1.UserID,
    c1.AssetID,
    u.FullName,
    a.Name AS AssetName
FROM Checkouts c1
INNER JOIN Checkouts c2 ON c1.UserID = c2.UserID 
    AND c1.AssetID = c2.AssetID 
    AND c1.CheckoutID < c2.CheckoutID
INNER JOIN Users u ON c1.UserID = u.UserID
INNER JOIN Assets a ON c1.AssetID = a.AssetID
WHERE c1.Status = 'Active' AND c2.Status = 'Active';
-- Should return NO results (no duplicates allowed)
GO

PRINT '';
PRINT 'All migration checks completed successfully!';
PRINT 'If any issues were found, review the results and apply necessary fixes.';
