namespace ActDim.Practix.Service;

public class UserInfo
{
    public string Id { get; private set; }

    public string Username { get; private set; }

    public UserInfo(string id, string username)
    {
        Id = id;
        Username = username;
    }
}
