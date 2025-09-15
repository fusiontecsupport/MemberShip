# Base URL Implementation Guide

## Overview
This guide explains how base URL handling has been implemented across the entire ClubMembership application to ensure images and other assets work correctly both locally and in production.

## What's Been Implemented

### 1. Web.config Configuration
Added the BaseURL setting in `Web.config`:
```xml
<add key="BaseURL" value="https://fusiontecsoftware.com/sndp" />
```

### 2. Global JavaScript Solution (Layout)
Added comprehensive JavaScript in `Views/Shared/_Layout.cshtml` that automatically:
- Fixes all `<img>` tags with relative URLs
- Fixes background images in CSS
- Runs on page load, after AJAX requests, and periodically
- Works across ALL pages automatically

### 3. Server-Side Helper Methods
Created `Helpers/UrlHelper.cs` with extension methods:
- `Url.GetBaseUrl()` - Gets the current application's base URL
- `Url.ToAbsoluteUrl(path)` - Converts relative paths to absolute URLs
- `Url.ImageUrl(path)` - Specifically for image paths

### 4. View Integration
Updated `Views/_ViewStart.cshtml` to include the helper namespace globally.

## How to Use in Views

### Option 1: Use Helper Methods (Recommended for Server-Side)
```cshtml
<!-- For images -->
<img src="@Url.ImageUrl("/Uploads/Gallery/image.jpg")" alt="Gallery Image" />

<!-- For any relative path -->
<a href="@Url.ToAbsoluteUrl("/Content/css/style.css")">Link</a>

<!-- Get base URL -->
<script>
    var baseUrl = '@Url.GetBaseUrl()';
</script>
```

### Option 2: Let JavaScript Handle It (Automatic)
Simply use relative paths as normal - the global JavaScript will fix them:
```cshtml
<img src="/Uploads/Gallery/image.jpg" alt="Gallery Image" />
```

## Examples by Page Type

### Home/Index.cshtml
```cshtml
@if (!string.IsNullOrEmpty(item.MainImage))
{
    <img class="feed-img" src="@Url.ImageUrl(item.MainImage)" alt="@item.Heading" />
}
```

### GalleryMaster/Form.cshtml
```cshtml
@foreach (var imagePath in Model.ImageList)
{
    <img src="@Url.ImageUrl(imagePath)" alt="Additional Image" />
}
```

### Any Other View
```cshtml
<!-- For profile images -->
<img src="@Url.ImageUrl(Model.ProfileImage)" alt="Profile" />

<!-- For uploaded files -->
<a href="@Url.ToAbsoluteUrl(Model.DocumentPath)">Download</a>

<!-- For CSS backgrounds -->
<div style="background-image: url('@Url.ImageUrl("/Content/images/bg.jpg")')"></div>
```

## URL Resolution Examples

### Local Development
- Input: `/Uploads/Gallery/image.jpg`
- Output: `http://localhost:16187/Uploads/Gallery/image.jpg`

### Production (IIS Virtual Directory)
- Input: `/Uploads/Gallery/image.jpg`
- Output: `https://fusiontecsoftware.com/sndp/Uploads/Gallery/image.jpg`

## Benefits

1. **Automatic**: Global JavaScript handles most cases without code changes
2. **Flexible**: Helper methods available for server-side rendering
3. **Consistent**: Same behavior across all pages
4. **Maintainable**: Centralized logic in layout and helpers
5. **Performance**: Minimal overhead, runs efficiently

## Migration Guide

### For Existing Views
1. **No action needed** - Global JavaScript handles existing relative URLs
2. **Optional improvement** - Replace relative paths with `@Url.ImageUrl()` for better server-side rendering

### For New Views
1. Use `@Url.ImageUrl(path)` for images
2. Use `@Url.ToAbsoluteUrl(path)` for other assets
3. Or use relative paths and let JavaScript handle them

## Troubleshooting

### Images Not Loading
1. Check if the path is correct
2. Verify the file exists in the specified location
3. Check browser console for JavaScript errors

### Base URL Not Working
1. Ensure `Web.config` has the correct BaseURL setting
2. Verify the layout file includes the JavaScript
3. Check that `_ViewStart.cshtml` includes the helper namespace

### Development vs Production
- Local: Uses `http://localhost:port/`
- Production: Uses `https://fusiontecsoftware.com/sndp/`
- Both work automatically based on the hosting environment

## Files Modified

1. `Web.config` - Added BaseURL setting
2. `Views/Shared/_Layout.cshtml` - Added global JavaScript
3. `Helpers/UrlHelper.cs` - Created helper methods
4. `Views/_ViewStart.cshtml` - Added helper namespace
5. `Views/Home/Index.cshtml` - Updated to use helpers
6. `Views/GalleryMaster/Form.cshtml` - Updated to use helpers

## Testing

Test the implementation by:
1. Running locally - images should work with localhost URLs
2. Deploying to IIS - images should work with production URLs
3. Checking different pages - all should handle images correctly
4. Testing AJAX-loaded content - JavaScript should fix dynamic content
