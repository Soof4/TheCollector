using TShockAPI;

namespace TheCollector;

public class CSVManager
{
    private string _path = Path.Combine(TShock.SavePath, "TheCollector.csv");
    private List<List<object>> _buffer = new List<List<object>>();
    private List<List<object>> _buffer2 = new List<List<object>>();
    private bool _flag = false;
    private List<object> _headers = new List<object>() {
        "playtime",
        "slotID",
        "itemID",
        "stack",
        "isAmmo",
        "rarity",
        "bossProgression"
    };

    public void Initialize()
    {
        Save();
    }

    private void Save()
    {

        Task.Run(async () =>
        {
            while (true)
            {
                if (_flag) continue;
                _flag = true;

                // Write the headers if file is new
                if (!File.Exists(_path))
                {
                    using (var writer = new StreamWriter(_path))
                    {
                        string line = string.Join(",", _headers.Select(h => h.ToString()));
                        writer.WriteLine(line);
                    }
                }

                using (var writer = new StreamWriter(_path, append: true))
                {
                    foreach (var row in _buffer)
                    {
                        string line = string.Join(",", row.Select(f => f.ToString()));
                        writer.WriteLine(line);
                    }
                }

                _buffer.Clear();
                _flag = false;

                await Task.Delay(5 * 60 * 1000);
            }
        });
    }


    public void Add(List<object> row)
    {
        if (_flag)
        {
            _buffer2.Add(row);
        }
        else
        {
            _flag = true;
            _buffer.AddRange(_buffer2);
            _buffer2.Clear();
            _buffer.Add(row);
            _flag = false;
        }
    }
}
