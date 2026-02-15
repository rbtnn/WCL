using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public class LauncherModel {
    private string xmlPath = "commands.xml";
    private string favPath = "favorites.txt";
    public List<CommandItem> AllItems { get; private set; }
    public HashSet<string> Favorites { get; private set; }
    public DateTime LastXmlWriteTime { get; private set; }

    public LauncherModel() {
        AllItems = new List<CommandItem>();
        Favorites = new HashSet<string>();
        LoadFavorites();
        LoadCommands();
    }

    public void LoadCommands() {
        if (!File.Exists(xmlPath)) return;
        try {
            XDocument doc = XDocument.Load(xmlPath);
            AllItems = doc.Descendants("Item").Select(x => new CommandItem {
                Name = (string)x.Element("Name") ?? "",
                Command = (string)x.Element("Command") ?? "",
                Remarks = (string)x.Element("Remarks") ?? ""
            }).ToList();
            LastXmlWriteTime = File.GetLastWriteTime(xmlPath);
        } catch { }
    }

    public void LoadFavorites() { if (File.Exists(favPath)) Favorites = new HashSet<string>(File.ReadAllLines(favPath)); }
    public void ToggleFavorite(string name) {
        if (Favorites.Contains(name)) Favorites.Remove(name); else Favorites.Add(name);
        File.WriteAllLines(favPath, Favorites.ToArray());
    }
    public bool IsXmlChanged() { return File.Exists(xmlPath) && File.GetLastWriteTime(xmlPath) != LastXmlWriteTime; }
    public List<CommandItem> GetFilteredList(string query) {
        string q = query.ToLower();
        return AllItems.Where(i => i.Name.ToLower().Contains(q) || (i.Remarks != null && i.Remarks.ToLower().Contains(q)) || (i.Command != null && i.Command.ToLower().Contains(q)))
            .OrderByDescending(i => Favorites.Contains(i.Name)).ToList();
    }
}
