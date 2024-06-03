namespace GoogleDriveClone.Models
{
    public class Folder
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentFolderId { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsActive { get; set; }
    }
}
