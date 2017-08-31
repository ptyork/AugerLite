using Auger.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Auger
{
    public class PlaygroundRepository : Repository
    {
        private static string _basePath;

        public static void Init(string basePath)
        {
            _basePath = basePath;
            Directory.CreateDirectory(_basePath);
        }

        public static bool Exists(int courseId, string userName, int repositoryId)
        {
            return System.IO.Directory.Exists($"{_basePath}\\{courseId}\\{userName.Trim().ToLowerInvariant()}\\{repositoryId}");
        }

        public static PlaygroundRepository Get(int courseId, string userName, int repositoryId, bool create = false)
        {
            return new PlaygroundRepository(courseId, userName, repositoryId, create);
        }

        public static Playground GetPlayground(int courseId, string userName, int repositoryId)
        {
            return new Playground(Get(courseId, userName, repositoryId, true));
        }

        public static List<Playground> GetAllPlaygrounds(int courseId, string userName)
        {
            if (string.IsNullOrWhiteSpace(_basePath))
            {
                throw new InvalidOperationException("This repository type has not been initialized.");
            }

            var playgrounds = new List<Playground>();
            var dir = Directory.CreateDirectory($"{_basePath}\\{courseId}\\{userName}");
            foreach (var subdir in dir.GetDirectories().OrderBy(d => d.Name))
            {
                int playgroundId;
                if (int.TryParse(subdir.Name, out playgroundId))
                {
                    var repo = new PlaygroundRepository(courseId, userName, playgroundId);
                    playgrounds.Add(repo.GetPlayground());
                }
            }
            return playgrounds;
        }

        public static PlaygroundRepository Create(int courseId, string userName, string name)
        {
            if (string.IsNullOrWhiteSpace(_basePath))
            {
                throw new InvalidOperationException("This repository type has not been initialized.");
            }

            var dir = Directory.CreateDirectory($"{_basePath}\\{courseId}\\{userName}");
            var max = 0;
            foreach (var subdir in dir.GetDirectories())
            {
                int id;
                if (int.TryParse(subdir.Name, out id))
                {
                    if (id > max)
                    {
                        max = id;
                    }
                }
            }
            var repo = new PlaygroundRepository(courseId, userName, ++max, true);
            repo.SetName(name);
            return repo;
        }

        public static void Delete(PlaygroundRepository repo)
        {
            repo.Folder.Delete(true);
        }

        public override string BasePath
        {
            get { return _basePath; }
        }

        public PlaygroundRepository(int courseId, string userName, int repositoryId, bool create = false)
            : base(courseId, userName, repositoryId, create)
        {   
        }

        public Playground GetPlayground()
        {
            return new Playground(this);
        }

        public string GetName()
        {
            string name = $"[PLAYGROUND {this.RepositoryId}]";

            var fileName = $"{this.FilePath}\\.playground-name.txt";
            if (File.Exists(fileName))
            {
                name = File.ReadAllText(fileName);
            }
            return name;
        }

        public void SetName(string name)
        {
            var fileName = $"{this.FilePath}\\.playground-name.txt";
            if (string.IsNullOrWhiteSpace(name))
            {
                File.Delete(fileName);
            }
            else
            {
                name = name?.Trim();
                FileAttributes attributes = FileAttributes.Normal;
                if (File.Exists(fileName))
                {
                    attributes = File.GetAttributes(fileName) & ~FileAttributes.Hidden;
                    File.SetAttributes(fileName, attributes);
                }
                File.WriteAllText(fileName, name);
                attributes = File.GetAttributes(fileName) | FileAttributes.Hidden;
                File.SetAttributes(fileName, attributes);
            }
        }
    }
}
