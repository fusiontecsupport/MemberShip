using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ClubMembership.Models
{
    [Table("GALLERYMASTER")]
    public class GalleryMaster
    {
        [Key]
        public int GalleryId { get; set; }

        [DisplayName("Heading")]
        [Required(ErrorMessage = "Heading is required")]
        [StringLength(200, ErrorMessage = "Heading cannot exceed 200 characters")]
        public string Heading { get; set; }

        [DisplayName("Caption")]
        [StringLength(500, ErrorMessage = "Caption cannot exceed 500 characters")]
        public string Caption { get; set; }

        [DisplayName("Description")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; }

        [DisplayName("Category Types")]
        public string CategoryTypeIds { get; set; } // Comma-separated list of category IDs

        [DisplayName("Main Image")]
        public string MainImage { get; set; }

        [DisplayName("Additional Images")]
        public string AdditionalImages { get; set; } // Comma-separated image paths

        [DisplayName("Category")]
        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        public string Category { get; set; }

        [DisplayName("Status")]
        public short Status { get; set; } // 0 = Active, 1 = Inactive

        [DisplayName("Created By")]
        public string CreatedBy { get; set; }

        [DisplayName("Created Date")]
        public DateTime CreatedDate { get; set; }

        [DisplayName("Modified By")]
        public string ModifiedBy { get; set; }

        [DisplayName("Modified Date")]
        public DateTime? ModifiedDate { get; set; }

        [DisplayName("Company ID")]
        public int CompanyId { get; set; }

        // Navigation property for related images
        [NotMapped]
        public List<string> ImageList
        {
            get
            {
                if (!string.IsNullOrEmpty(AdditionalImages))
                {
                    return AdditionalImages.Split(',').Select(x => x.Trim()).ToList();
                }
                return new List<string>();
            }
            set
            {
                AdditionalImages = value != null ? string.Join(",", value) : "";
            }
        }

        // Navigation property for CategoryType IDs
        [NotMapped]
        public List<int> CategoryTypeIdList
        {
            get
            {
                if (!string.IsNullOrEmpty(CategoryTypeIds))
                {
                    return CategoryTypeIds.Split(',')
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Select(x => int.Parse(x))
                        .ToList();
                }
                return new List<int>();
            }
            set
            {
                CategoryTypeIds = value != null ? string.Join(",", value) : "";
            }
        }

        // Navigation property for CategoryType descriptions
        [NotMapped]
        public List<string> CategoryTypeDescriptions { get; set; }

        // Legacy property for backward compatibility
        [NotMapped]
        public int? CategoryTypeId 
        { 
            get 
            { 
                var list = CategoryTypeIdList;
                return list.Any() ? list.First() : (int?)null;
            }
            set 
            { 
                if (value.HasValue)
                {
                    var list = new List<int> { value.Value };
                    CategoryTypeIds = string.Join(",", list);
                }
                else
                {
                    CategoryTypeIds = null;
                }
            }
        }

        // Legacy property for backward compatibility
        [NotMapped]
        public string CategoryTypeDescription 
        { 
            get 
            { 
                var descriptions = CategoryTypeDescriptions;
                return descriptions != null && descriptions.Any() ? string.Join(", ", descriptions) : "";
            }
        }
    }
}

