# Content Management System

This document describes the new Content Management features added to the Club Membership system.

## Overview

The Content Management system provides three main modules for managing club content:

1. **Announcements** - For posting club announcements and news
2. **Events** - For managing club events with time and location details
3. **Gallery** - For organizing and displaying club photos and media

## Features

### Common Features (All Modules)
- **Heading**: Main title for the content (required)
- **Caption**: Short subtitle or summary
- **Description**: Detailed content description
- **Main Image**: Primary image for the content
- **Additional Images**: Support for multiple image uploads
- **Status**: Active/Inactive toggle
- **Audit Fields**: Created by, created date, modified by, modified date
- **Company ID**: Multi-tenant support

### Announcements Module
- **Purpose**: Post club announcements and news
- **Features**:
  - Simple announcement posting
  - Image support for visual announcements
  - Status management for publishing control

### Events Module
- **Purpose**: Manage club events and activities
- **Additional Features**:
  - **Event Time**: Date and time of the event (required)
  - **Event Plan**: Detailed event schedule or plan
  - **Event Location**: Venue or location details
  - **Event-specific image management**

### Gallery Module
- **Purpose**: Organize and display club photos and media
- **Additional Features**:
  - **Category**: Organize gallery items by category
  - **Multiple image support** for photo collections
  - **Visual gallery management**

## Database Tables

### ANNOUNCEMENTMASTER
```sql
- AnnouncementId (Primary Key)
- Heading (varchar 200)
- Caption (varchar 500)
- Description (varchar 2000)
- MainImage (varchar)
- AdditionalImages (varchar - comma-separated)
- Status (smallint)
- CreatedBy (varchar)
- CreatedDate (datetime)
- ModifiedBy (varchar)
- ModifiedDate (datetime)
- CompanyId (int)
```

### EVENTMASTER
```sql
- EventId (Primary Key)
- Heading (varchar 200)
- Caption (varchar 500)
- Description (varchar 2000)
- MainImage (varchar)
- AdditionalImages (varchar - comma-separated)
- EventTime (datetime)
- EventPlan (varchar 1000)
- EventLocation (varchar 200)
- Status (smallint)
- CreatedBy (varchar)
- CreatedDate (datetime)
- ModifiedBy (varchar)
- ModifiedDate (datetime)
- CompanyId (int)
```

### GALLERYMASTER
```sql
- GalleryId (Primary Key)
- Heading (varchar 200)
- Caption (varchar 500)
- Description (varchar 2000)
- MainImage (varchar)
- AdditionalImages (varchar - comma-separated)
- Category (varchar 100)
- Status (smallint)
- CreatedBy (varchar)
- CreatedDate (datetime)
- ModifiedBy (varchar)
- ModifiedDate (datetime)
- CompanyId (int)
```

## File Structure

### Controllers
- `Controllers/Masters/AnnouncementMasterController.cs`
- `Controllers/Masters/EventMasterController.cs`
- `Controllers/Masters/GalleryMasterController.cs`

### Models
- `Models/AnnouncementMaster.cs`
- `Models/EventMaster.cs`
- `Models/GalleryMaster.cs`

### Views
- `Views/AnnouncementMaster/Index.cshtml`
- `Views/AnnouncementMaster/Form.cshtml`
- `Views/EventMaster/Index.cshtml`
- `Views/EventMaster/Form.cshtml`
- `Views/GalleryMaster/Index.cshtml`
- `Views/GalleryMaster/Form.cshtml`

### Upload Directories
- `Uploads/Announcements/` - For announcement images
- `Uploads/Events/` - For event images
- `Uploads/Gallery/` - For gallery images

## Navigation

The Content Management modules are accessible through the main navigation menu under:
**Content Management** dropdown with options:
- Announcements
- Events
- Gallery

## Usage

### Creating Content
1. Navigate to the desired module (Announcements, Events, or Gallery)
2. Click "New [Item]" button
3. Fill in the required fields:
   - Heading (required)
   - Caption (optional)
   - Description (optional)
   - Upload main image (optional)
   - Upload additional images (optional)
   - For Events: Set event time and location
   - For Gallery: Set category
4. Set status (Active/Inactive)
5. Click "Save"

### Managing Content
- **View**: All content is displayed in a responsive data table
- **Edit**: Click the edit icon to modify existing content
- **Delete**: Click the delete icon to remove content (with confirmation)
- **Search**: Use the search functionality to find specific content
- **Sort**: Click column headers to sort by different criteria

### Image Management
- **Main Image**: Single image upload for primary display
- **Additional Images**: Multiple image upload support
- **Preview**: Images are previewed before upload
- **Storage**: Images are stored in organized directories by module
- **Display**: Images are displayed as thumbnails in lists and full size in forms

## Security

### Authorization
Each module requires specific roles for access:
- `AnnouncementMasterIndex` - View announcements
- `AnnouncementMasterCreate` - Create/edit announcements
- `AnnouncementMasterEdit` - Edit announcements
- `AnnouncementMasterDelete` - Delete announcements

Similar roles exist for Events and Gallery modules.

### File Upload Security
- Only image files are accepted
- Files are validated before upload
- Unique file names are generated to prevent conflicts
- Files are stored in secure, organized directories

## Technical Implementation

### Entity Framework
- Models use Entity Framework Code First approach
- Database migrations handle schema changes
- Proper relationships and constraints are maintained

### File Upload
- Uses ASP.NET MVC file upload capabilities
- Client-side preview with JavaScript
- Server-side validation and processing
- Organized file storage structure

### UI/UX
- Bootstrap 5 responsive design
- DataTables for enhanced table functionality
- Font Awesome icons for visual consistency
- Modern card-based layout
- Intuitive form design with validation

## Migration Instructions

1. **Database Migration**:
   ```bash
   Update-Database -Verbose
   ```

2. **File Permissions**:
   Ensure the application has write permissions to the `Uploads/` directory and its subdirectories.

3. **Role Configuration**:
   Add the required roles to your role management system:
   - AnnouncementMasterIndex, AnnouncementMasterCreate, AnnouncementMasterEdit, AnnouncementMasterDelete
   - EventMasterIndex, EventMasterCreate, EventMasterEdit, EventMasterDelete
   - GalleryMasterIndex, GalleryMasterCreate, GalleryMasterEdit, GalleryMasterDelete

## Future Enhancements

Potential improvements for the Content Management system:
- **Rich Text Editor**: WYSIWYG editor for descriptions
- **Image Cropping**: Client-side image editing
- **Bulk Operations**: Mass upload and management
- **Categories**: Enhanced categorization system
- **Scheduling**: Auto-publish/unpublish content
- **API Integration**: REST API for external access
- **Search**: Advanced search with filters
- **Analytics**: Content performance tracking

