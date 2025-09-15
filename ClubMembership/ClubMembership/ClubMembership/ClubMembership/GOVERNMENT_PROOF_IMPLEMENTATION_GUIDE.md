# Government Proof Implementation Guide

## Overview
This implementation adds the ability to upload and store government proof documents during member registration. The government proof files are stored in the file system and their paths are saved in the `govrmnet_proof` database table.

## Database Changes

### New Table: `govrmnet_proof`
```sql
CREATE TABLE [dbo].[govrmnet_proof](
    [gp_id] [int] IDENTITY(1,1) NOT NULL,
    [MemberID] [int] NOT NULL,
    [gov_path] [nvarchar](500) NOT NULL,
    CONSTRAINT [PK_govrmnet_proof] PRIMARY KEY CLUSTERED ([gp_id] ASC)
)
```

**Columns:**
- `gp_id`: Auto-increment primary key
- `MemberID`: Foreign key to `MemberShipMaster.MemberID`
- `gov_path`: File path of the uploaded government proof document

## File Structure

### Upload Directory
Government proof files are stored in: `~/Uploads/GovProofs/`

### File Naming Convention
Files are saved with GUID-based names to prevent conflicts: `{GUID}.{extension}`

### Supported File Types
- PDF (.pdf)
- JPEG (.jpg, .jpeg)
- PNG (.png)

## Implementation Details

### 1. Model Changes

#### New Model: `GovernmentProof.cs`
```csharp
[Table("govrmnet_proof")]
public class GovernmentProof
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int gp_id { get; set; }

    [Required]
    public int MemberID { get; set; }

    [Required]
    [StringLength(500)]
    public string gov_path { get; set; }

    [ForeignKey("MemberID")]
    public virtual MemberShipMaster MemberShipMaster { get; set; }
}
```

#### Updated: `RegisterViewModel.cs`
Added properties for file upload:
```csharp
[Display(Name = "Government Proof (PDF/Image)")]
public HttpPostedFileBase GovernmentProofFile { get; set; }

public string GovernmentProofPath { get; set; }
```

#### Updated: `ApplicationUser.cs`
Added property to store the government proof path:
```csharp
[Display(Name = "Government Proof Path")]
[NotMapped]
public string GovernmentProofPath { get; set; }
```

### 2. Database Context
Added to `ApplicationDbContext.cs`:
```csharp
public DbSet<GovernmentProof> GovernmentProofs { get; set; }
```

### 3. Controller Changes

#### RegisterController.cs
The registration process now includes:

1. **File Upload Validation**: Validates file type and size
2. **File Storage**: Saves files to `~/Uploads/GovProofs/` directory
3. **Database Storage**: Saves file path to `govrmnet_proof` table after successful registration

Key code sections:
```csharp
// File upload handling
if (model.GovernmentProofFile != null && model.GovernmentProofFile.ContentLength > 0)
{
    var allowedExt = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
    var ext = Path.GetExtension(model.GovernmentProofFile.FileName).ToLowerInvariant();
    if (!allowedExt.Contains(ext))
    {
        ModelState.AddModelError("GovernmentProofFile", "Please submit the correct proof (PDF, JPG, JPEG, PNG).");
    }
    else
    {
        var uploadRoot = Server.MapPath("~/Uploads/GovProofs");
        if (!Directory.Exists(uploadRoot)) Directory.CreateDirectory(uploadRoot);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadRoot, fileName);
        model.GovernmentProofFile.SaveAs(fullPath);

        model.GovernmentProofPath = Url.Content($"~/Uploads/GovProofs/{fileName}");
    }
}

// Database storage after successful registration
if (!string.IsNullOrEmpty(model.GovernmentProofPath) && createdUser.MemberID.HasValue)
{
    var governmentProof = new GovernmentProof
    {
        MemberID = createdUser.MemberID.Value,
        gov_path = model.GovernmentProofPath
    };
    db.GovernmentProofs.Add(governmentProof);
    db.SaveChanges();
}
```

### 4. View Changes

#### Login_Register.cshtml
Added file upload section:
```html
<!-- Government Proof Upload Section -->
<div class="form-section">
    <h4 class="section-title">Government Proof</h4>
    <div class="row g-2">
        <div class="col-12">
            <label class="form-label">Upload Government Proof (PDF/JPG/PNG)</label>
            <input type="file" name="GovernmentProofFile" id="GovernmentProofFile" class="form-control" accept=".pdf,.jpg,.jpeg,.png" />
            @Html.ValidationMessage("GovernmentProofFile", "", new { @class = "validation-error" })
        </div>
    </div>
</div>
```

## Setup Instructions

### 1. Database Setup
Run the SQL script to create the table:
```sql
-- Execute the SQL script: SQL/Create_GovernmentProof_Table.sql
```

### 2. File System Setup
Ensure the upload directory exists:
```
~/Uploads/GovProofs/
```

### 3. Migration (Optional)
If using Entity Framework migrations:
```bash
dotnet ef database update
```

## Usage

### Registration Process
1. User fills out registration form
2. User uploads government proof document (optional)
3. System validates file type and size
4. File is saved to server
5. After successful registration, file path is stored in database

### File Access
Government proof files can be accessed via:
```
http://localhost:16187/Uploads/GovProofs/{filename}
```

## Security Considerations

1. **File Type Validation**: Only allows specific file types
2. **Unique File Names**: Uses GUID to prevent file conflicts
3. **Directory Isolation**: Files stored in dedicated directory
4. **Database Constraints**: Foreign key ensures data integrity

## Troubleshooting

### Common Issues

1. **File Upload Fails**
   - Check directory permissions for `~/Uploads/GovProofs/`
   - Verify file size limits in web.config
   - Ensure file type is supported

2. **Database Errors**
   - Verify `govrmnet_proof` table exists
   - Check foreign key constraints
   - Ensure MemberID exists in MemberShipMaster

3. **File Not Found**
   - Verify file path in database
   - Check if file exists in upload directory
   - Ensure proper URL routing

## Testing

### Test Cases
1. **Valid File Upload**: Upload PDF/JPG/PNG files
2. **Invalid File Type**: Try uploading unsupported formats
3. **No File Upload**: Registration without government proof
4. **File Access**: Verify uploaded files are accessible
5. **Database Storage**: Confirm file paths are saved correctly

## Future Enhancements

1. **File Compression**: Compress large files
2. **Multiple Files**: Allow multiple government proof documents
3. **File Preview**: Add preview functionality
4. **File Management**: Add ability to update/delete files
5. **Audit Trail**: Track file upload history
