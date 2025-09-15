-- Setup Cascading Delete Constraints for Club Membership System
-- Run this script on your database to enable cascading deletes

-- First, drop existing foreign key constraints if they exist
-- (You may need to adjust constraint names based on your actual database)

-- Drop existing constraints (if they exist)
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_MemberShip_FDetails_MemberShipMaster')
    ALTER TABLE MemberShip_FDetails DROP CONSTRAINT FK_MemberShip_FDetails_MemberShipMaster;

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_MemberShip_PaymentDetails_MemberShipMaster')
    ALTER TABLE MemberShip_PaymentDetails DROP CONSTRAINT FK_MemberShip_PaymentDetails_MemberShipMaster;

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_MemberShip_ODetails_MemberShipMaster')
    ALTER TABLE MemberShip_ODetails DROP CONSTRAINT FK_MemberShip_ODetails_MemberShipMaster;

-- Add new foreign key constraints with CASCADE DELETE

-- Family Details cascading delete
ALTER TABLE MemberShip_FDetails
ADD CONSTRAINT FK_MemberShip_FDetails_MemberShipMaster
FOREIGN KEY (MemberID) REFERENCES MemberShipMaster(MemberID)
ON DELETE CASCADE;

-- Payment Details cascading delete
ALTER TABLE MemberShip_PaymentDetails
ADD CONSTRAINT FK_MemberShip_PaymentDetails_MemberShipMaster
FOREIGN KEY (MemberID) REFERENCES MemberShipMaster(MemberID)
ON DELETE CASCADE;

-- Organization Details cascading delete
ALTER TABLE MemberShip_ODetails
ADD CONSTRAINT FK_MemberShip_ODetails_MemberShipMaster
FOREIGN KEY (MemberID) REFERENCES MemberShipMaster(MemberID)
ON DELETE CASCADE;

-- Verify the constraints were created
SELECT 
    fk.name AS ForeignKeyName,
    tp.name AS ParentTable,
    cp.name AS ParentColumn,
    tr.name AS ReferencedTable,
    cr.name AS ReferencedColumn,
    fk.delete_referential_action_desc AS DeleteAction
FROM sys.foreign_keys fk
INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id
INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id
INNER JOIN sys.columns cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id
WHERE tr.name = 'MemberShipMaster'
ORDER BY tp.name;

PRINT 'Cascading delete constraints have been set up successfully!';
