using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileCollector.Models
{
    public class FileSystemItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public bool IsSelected { get; set; }
        public bool IsExpanded { get; set; }
        private FileSystemItem? Parent { get; set; }
        public List<FileSystemItem> Children { get; set; } = [];

        public FileSystemItem(string fullPath, bool isDirectory, FileSystemItem? parent = null)
        {
            FullPath = fullPath;
            Name = Path.GetFileName(fullPath);
            if (string.IsNullOrEmpty(Name) && isDirectory)
            {
                Name = fullPath;
            }

            IsDirectory = isDirectory;
            Parent = parent;
            IsSelected = false;
            IsExpanded = false;
        }


        public void SetSelectionStatus(bool selected, List<string> masterSelectedList)
        {
            IsSelected = selected;
            if (selected)
            {
                if (!masterSelectedList.Contains(FullPath))
                {
                    masterSelectedList.Add(FullPath);
                }
            }
            else
            {
                masterSelectedList.Remove(FullPath);
            }

            if (IsDirectory)
            {
                foreach (var child in Children)
                {
                    child.SetSelectionStatus(selected, masterSelectedList);
                }
            }
        }


        public void UpdateParentSelectionStatus(List<string> masterSelectedList)
        {
            if (Parent is not { IsDirectory: true }) return;


            var allChildrenSelected = Parent.Children.All(c => c.IsSelected);

            if (Parent.IsSelected != allChildrenSelected)
            {
                Parent.IsSelected = allChildrenSelected;
                if (Parent.IsSelected)
                {
                    if (!masterSelectedList.Contains(Parent.FullPath))
                    {
                        masterSelectedList.Add(Parent.FullPath);
                    }
                }
                else
                {
                    masterSelectedList.Remove(Parent.FullPath);
                }

                Parent.UpdateParentSelectionStatus(masterSelectedList);
            }
        }
    }
}