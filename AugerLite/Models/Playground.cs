using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Auger.Models
{
    public class Playground
    {
        public string UserName { get; set; }
        public int CourseId { get; set; }
        public int PlaygroundId { get; set; }
        public string Name { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public bool IsOwner { get; set; } = true;
        public bool IsShared { get; set; }

        public Playground(PlaygroundRepository repo)
        {
            this.UserName = repo.UserName;
            this.CourseId = repo.CourseId;
            this.PlaygroundId = repo.RepositoryId;
            this.Name = repo.GetName();
            this.CreationDate = repo.Folder.CreationTime;
            this.UpdateDate = repo.Folder.LastWriteTime;
            this.IsShared = repo.GetIsShared();
        }
    }
}