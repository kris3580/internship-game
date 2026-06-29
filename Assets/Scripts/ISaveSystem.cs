public interface ISaveSystem
{
    bool HasKey(string key);
    int GetInt(string key, int defaultValue = 0);
    void SetInt(string key, int value);
    long GetLong(string key, long defaultValue = 0L);
    void SetLong(string key, long value);
    string GetString(string key, string defaultValue = "");
    void SetString(string key, string value);
    void DeleteKey(string key);
    void Save();
}
