﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISCI
{
    public class LocalRepository: IDisposable
    {

        SortedDictionary<string,LocalRepositoryFile> Files { get; set; }
        string LocalFile;
        public string LocalFolder { get; private set; }
        public LocalRepository(string localFolder)
        {
            LocalFolder = localFolder + "\\Source\\";
            LocalFile = localFolder + "\\local-repository.json";
            if (System.IO.File.Exists(LocalFile))
            {
                Files = new SortedDictionary<string, LocalRepositoryFile>(JsonStorage.ReadFile<LocalRepositoryFile[]>(LocalFile).ToDictionary(x => x.Url));
            }
            else {
                Files = new SortedDictionary<string, LocalRepositoryFile>();
            }
        }



        public void UpdateFiles(IEnumerable<LocalRepositoryFile> updatedItems)
        {
            foreach (var item in updatedItems)
            {
                Files[item.Url] = item;
            }
            Save();
        }

        public IEnumerable<Change> GetChanges(IEnumerable<ISourceItem> remoteItems) 
        {
            List<Change> changes = new List<Change>();

            foreach (var remoteItem in remoteItems)
            {
                LocalRepositoryFile localFile;
                if (Files.TryGetValue(remoteItem.Url, out localFile))
                {
                    if (localFile.Version != remoteItem.Version)
                    {
                        Files.Remove(remoteItem.Url);
                        changes.Add(new Change { Type = ChangeType.Modified, RepositoryFile = localFile });
                    }
                }
                else
                {
                    localFile = new LocalRepositoryFile
                    {
                        Name = remoteItem.Name,
                        Folder = remoteItem.Folder,
                        Version = remoteItem.Version,
                        Url = remoteItem.Url,
                        IsDirectory = remoteItem.IsDirectory
                    };
                    changes.Add(new Change { Type = ChangeType.Added, RepositoryFile = localFile });
                }
            }

            foreach (var item in Files.Values)
            {
                if (!remoteItems.Any(x => x.Url == item.Url))
                {
                    changes.Add(new Change { Type = ChangeType.Removed, RepositoryFile = item });
                }
            }

            return changes;
        }

        public void Dispose()
        {
            Save();
        }

        private void Save()
        {
            JsonStorage.WriteFile(Files.Values, LocalFile);
        }


    }

    public class Change {
        public ChangeType Type { get; set; }
        public LocalRepositoryFile RepositoryFile { get; set; }
    }

    public enum ChangeType { 
        Added,
        Removed,
        Modified
    }

    public class LocalRepositoryFile : ISourceItem
    {
        public string Name { get; set; }
        public string Folder { get; set; }
        public string Version { get; set; }
        public string Url { get; set; }
        public bool IsDirectory { get; set; }
    }
}