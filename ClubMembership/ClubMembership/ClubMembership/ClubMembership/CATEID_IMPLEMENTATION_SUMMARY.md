# CateTid Implementation for User Registration

## Overview
This document summarizes the implementation of CateTid functionality where:
- When a user account is created via `http://localhost:16187/Register/Index`, the `CateTid` is automatically set to 3 in the `AspNetUsers` table
- The corresponding `CateTDesc` from `CategoryTypeMaster` table (where `CateTid = 3`) is stored in the `MemberShipMaster` table

## What Has Been Implemented

### 1. Database Schema Updates

#### AspNetUsers Table
- **Added Column**: `CateTid` (INT, nullable)
- **Foreign Key**: References `CategoryTypeMaster.CateTid`
- **SQL Script**: `SQL/Add_CateTid_To_AspNetUsers.sql`

```sql
-- Add CateTid column to AspNetUsers table
ALTER TABLE [dbo].[AspNetUsers] 
ADD [CateTid] INT NULL;

-- Add foreign key constraint
ALTER TABLE [dbo].[AspNetUsers]
ADD CONSTRAINT FK_AspNetUsers_CategoryTypeMaster 
FOREIGN KEY (CateTid) REFERENCES CategoryTypeMaster(CateTid);
```

#### MemberShipMaster Table
- **Added Column**: `CateTDesc` (NVARCHAR(200), nullable)
- **Purpose**: Stores the category description from CategoryTypeMaster
- **SQL Script**: `SQL/Add_CateDesc_To_MemberShipMaster.sql`

```sql
-- Add CateTDesc column to MemberShipMaster table
ALTER TABLE [dbo].[MemberShipMaster] 
ADD [CateTDesc] NVARCHAR(200) NULL;
```

### 2. Model Updates

#### ApplicationUser.cs
- **Added Property**: `CateTid` (int?, nullable)
- **Display Attribute**: "Category Type"
- **Maps to**: `AspNetUsers.CateTid` column

```csharp
// Category Type ID - links to CategoryTypeMaster table
[Display(Name = "Category Type")]
public int? CateTid { get; set; }
```

#### MemberShipMaster.cs
- **Added Property**: `CateTDesc` (string, nullable)
- **Column Mapping**: Maps to `CateTDesc` database column
- **Purpose**: Stores category description from CategoryTypeMaster

```csharp
// Category Description - stores the description from CategoryTypeMaster
[StringLength(200)]
[Column("CateTDesc")]
public string CateTDesc { get; set; }
```

#### ApplicationDbContext.cs
- **Added Mapping**: CateTid property to database column
- **Entity Configuration**: Explicit column mapping in OnModelCreating

```csharp
// Map the CateTid property to the database column
table.Property((ApplicationUser u) => u.CateTid).HasColumnName("CateTid");
```

### 3. Controller Updates

#### RegisterController.cs
- **Registration Logic**: Sets `CateTid = 3` when creating user account
- **Category Description**: Retrieves and stores `CateTDesc` from `CategoryTypeMaster` where `CateTid = 3`
- **MemberShipMaster**: Stores the category description in the `CateTDesc` field

```csharp
// Set CateTid to 3 as requested
user.CateTid = 3;

// Get the category description for CateTid = 3
var categoryType = clubDb.Database.SqlQuery<string>(
    "SELECT CateTDesc FROM CategoryTypeMaster WHERE CateTid = 3"
).FirstOrDefault();
cateDesc = categoryType ?? "Default Category";

// Store in MemberShipMaster
member.CateTDesc = cateDesc;
```

#### MembersController.cs
- **Member Creation**: Sets `CateTid = 3` when creating user account via Members/Create
- **Category Description**: Retrieves and stores `CateTDesc` from `CategoryTypeMaster` where `CateTid = 3`
- **Consistent Logic**: Same implementation as RegisterController

### 4. Migration Files

#### 202501010000002_AddCateTidToAspNetUsers.cs
- **Entity Framework Migration**: Adds CateTid column to AspNetUsers table
- **Foreign Key**: Creates foreign key constraint to CategoryTypeMaster
- **Rollback Support**: Includes Down() method to remove changes

```csharp
public override void Up()
{
    AddColumn("dbo.AspNetUsers", "CateTid", c => c.Int(nullable: true));
    CreateIndex("dbo.AspNetUsers", "CateTid");
    AddForeignKey("dbo.AspNetUsers", "CateTid", "dbo.CategoryTypeMaster", "CateTid");
}
```

#### 202501010000003_AddCateDescToMemberShipMaster.cs
- **Entity Framework Migration**: Adds CateTDesc column to MemberShipMaster table
- **Column Type**: NVARCHAR(200), nullable
- **Rollback Support**: Includes Down() method to remove changes

```csharp
public override void Up()
{
    AddColumn("dbo.MemberShipMaster", "CateTDesc", c => c.String(maxLength: 200));
}
```

## Implementation Flow

### User Registration Process
1. **User Registration**: User fills registration form at `/Register/Index`
2. **Account Creation**: `RegisterController.Index()` creates ApplicationUser with `CateTid = 3`
3. **Category Lookup**: System queries `CategoryTypeMaster` for `CateTDesc` where `CateTid = 3`
4. **Member Creation**: Creates `MemberShipMaster` record with the retrieved `CateTDesc`
5. **Database Storage**: 
   - `AspNetUsers.CateTid = 3`
   - `MemberShipMaster.CateTDesc = [Description from CategoryTypeMaster]`

### Member Creation Process (Admin)
1. **Admin Access**: Admin creates member via `/Members/Create`
2. **Member Data**: Admin fills member information
3. **User Account**: System creates ApplicationUser with `CateTid = 3`
4. **Category Lookup**: System queries `CategoryTypeMaster` for `CateTDesc` where `CateTid = 3`
5. **Database Storage**: Same as registration process

## Database Relationships

```
AspNetUsers
├── CateTid (INT) ──┐
└── [other columns] │
                    │
CategoryTypeMaster  │
├── CateTid (PK) ←──┘
├── CateTCode
├── CateTDesc
└── [other columns]
                    │
MemberShipMaster    │
├── CateTDesc ←─────┘ (stores description, not ID)
└── [other columns]
```

## Required Database Scripts

### 1. Add CateTid Column to AspNetUsers
```sql
-- Execute: SQL/Add_CateTid_To_AspNetUsers.sql
-- This adds the CateTid column and foreign key constraint
```

### 2. Add CateTDesc Column to MemberShipMaster
```sql
-- Execute: SQL/Add_CateDesc_To_MemberShipMaster.sql
-- This adds the CateTDesc column to store category descriptions
```

### 3. Verify CategoryTypeMaster Data
```sql
-- Ensure CateTid = 3 exists in CategoryTypeMaster
SELECT CateTid, CateTDesc FROM CategoryTypeMaster WHERE CateTid = 3;
```

## Testing Verification

### After Implementation:
1. **Register New User**: Use `/Register/Index` to create account
2. **Check AspNetUsers**: Verify `CateTid = 3` for new user
3. **Check MemberShipMaster**: Verify `CateTDesc` contains description from CategoryTypeMaster
4. **SQL Verification**:
   ```sql
   -- Check user's CateTid
   SELECT UserName, CateTid FROM AspNetUsers WHERE UserName = '[username]';
   
   -- Check member's CateTDesc
   SELECT Member_Name, CateTDesc FROM MemberShipMaster WHERE UserName = '[username]';
   
   -- Verify category description source
   SELECT CateTid, CateTDesc FROM CategoryTypeMaster WHERE CateTid = 3;
   ```

## Error Handling

### Category Lookup Failure
- **Fallback Value**: "Default Category" if CategoryTypeMaster query fails
- **Try-Catch**: Wrapped in exception handling to prevent registration failure
- **Logging**: Errors logged for debugging purposes

### Database Constraints
- **Nullable CateTid**: Column allows NULL values for backward compatibility
- **Nullable CateTDesc**: Column allows NULL values for backward compatibility
- **Foreign Key**: Ensures data integrity with CategoryTypeMaster
- **Migration Safety**: Includes rollback procedures

## Notes
- **Default Value**: All new user registrations automatically get `CateTid = 3`
- **Backward Compatibility**: Existing users will have `CateTid = NULL` and `CateTDesc = NULL`
- **Data Integrity**: Foreign key ensures only valid category IDs are stored
- **Error Resilience**: System continues to work even if category lookup fails
- **Column Naming**: Uses `CateTDesc` to match existing database schema convention
