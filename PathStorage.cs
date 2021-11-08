using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GoogleDrive
{
    public class PathStorage
    {
        private static PathStorage Reference { get; set; } = null;
        private static string SerializedStorage = @"PathStorage.json";
        public static PathStorage GetInstance()
        {
            if (Reference != null)
                return Reference;

            if (File.Exists(SerializedStorage))
            {
                Reference = new PathStorage();
                Reference.storage = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(SerializedStorage));
            }
            else
            {
                Reference = new PathStorage();
            }

            return Reference;
        }

        public static void Store()
        {
            var buffer = JsonSerializer.Serialize(Reference.storage);

            File.WriteAllText(SerializedStorage, buffer);
        }


        //private Directory Root { get; set; } 
        private Dictionary<string, string> storage = new Dictionary<string, string>();

        public string this[string key]
        {
            get { if (key == null) return null; else { storage.TryGetValue(key, out string ret); return ret; } }
            set { if (key != null) storage[key] = value; }
        }


        //public string GetId(string key)
        //{
        //    if (key == null || Root == null)
        //        return null;
        //    var arr = key.Split('/');
        //    var dir = Root;

        //    if (arr.Length == 1)
        //    {
        //        return arr.First() == dir.Name ? Root.Id : null;
        //    }

        //    for(int i = 0; i < arr.Length ; i++)
        //    {
        //        dir = dir.Children.FirstOrDefault(t=> arr[i] == t.Name);

        //        if (dir == null)
        //            return null;
        //    }

        //    return dir.Id;

        //}

        //public string SetId(string key, string id)
        //{
        //    if (key == null || Root == null)
        //        return null;
        //    var arr = key.Split('/');
        //    var dir = Root;



        //    for (int i = 0; i < arr.Length-1; i++)
        //    {
        //        if (arr.Length == 2)
        //        {
        //            break;
        //        }
        //        dir = dir.Children.FirstOrDefault(t => arr[i] == t.Name);

        //        if (dir == null)
        //            break;
        //    }

        //    if (dir == null)
        //        return null;

        //    dir.Children.Add(new Directory()
        //    {
        //        Id = id,
        //        Name = arr.Last()
        //    });

        //    return id;
        //}

        //public void SetRoot(string root, string rootId)
        //{
        //    Root = new Directory()
        //    {
        //        Name = root,
        //        Id = rootId
        //    };
        //}

    }

    class Directory
    {
        public Directory()
        {
            Children = new List<Directory>();
        }
        public string Name { get; set; }
        public string Id { get; set; }

        public List<Directory> Children { get; set; }
    }
}
