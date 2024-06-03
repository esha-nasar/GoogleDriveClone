namespace GoogleDriveClone.Models
{
    public class File
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ParentFolderId { get; set; }
        public string FileExt { get; set; }
        public int FileSizeInKB { get; set; }
        public int CreatedBy { get; set; }
        public DateTime UploadedOn { get; set; }
        public bool IsActive { get; set; }
    }
}
