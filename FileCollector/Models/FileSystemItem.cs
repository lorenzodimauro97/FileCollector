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
        public bool IsPartiallySelected { get; set; }
        public bool IsExpanded { get; set; }
        public FileSystemItem? Parent { get; set; }
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
            IsPartiallySelected = false;
            IsExpanded = false;
        }


        public void SetSelectionStatus(bool selected, List<string> masterSelectedList)
        {
            IsSelected = selected;
            IsPartiallySelected = false;
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
            
            var allChildrenSelected = Parent.Children.All(c => c.IsSelected && !c.IsPartiallySelected);
            var noChildrenSelectedOrPartial = Parent.Children.All(c => !c.IsSelected && !c.IsPartiallySelected);

            var newIsSelected = allChildrenSelected;
            var newIsPartiallySelected = !allChildrenSelected && !noChildrenSelectedOrPartial;
            
            if (Parent.IsSelected != newIsSelected || Parent.IsPartiallySelected != newIsPartiallySelected)
            {
                Parent.IsSelected = newIsSelected;
                Parent.IsPartiallySelected = newIsPartiallySelected;

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