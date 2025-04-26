using Renci.SshNet;
using VaultSharp;
using VaultSharp.V1;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines.KeyValue;

public class SftpSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string LocalTargetPath { get; set; }
}

public class DestinationSftpSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string RemoteTargetPath { get; set; }
}

public class SftpService
{
    private readonly SftpSettings _sftpSettings;
    private readonly DestinationSftpSettings _destinationSftpSettings;

    public SftpService(string vaultAddress, string vaultToken)
    {
        var authMethod = new TokenAuthMethodInfo(vaultToken);
        var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod);
        var vaultClient = new VaultClient(vaultClientSettings);
        var sourceResult = vaultClient.V1.Secrets.KeyValue.V2
            .ReadSecretAsync(path: "source", mountPoint: "devsecops-sftp-service")
            .GetAwaiter().GetResult();

        var sourceData = sourceResult.Data.Data;

        _sftpSettings = new SftpSettings
        {
            Host = sourceData["Host"].ToString(),
            Port = Int32.Parse(sourceData["Port"].ToString()),
            Username = sourceData["Username"].ToString(),
            Password = sourceData["Password"].ToString(),
        };

        var destResult = vaultClient.V1.Secrets.KeyValue.V2
            .ReadSecretAsync(path: "destination", mountPoint: "devsecops-sftp-service")
            .GetAwaiter().GetResult();

        var destData = destResult.Data.Data;

        _destinationSftpSettings = new DestinationSftpSettings
        {
            Host = destData["Host"].ToString(),
            Port = Int32.Parse(sourceData["Port"].ToString()),
            Username = destData["Username"].ToString(),
            Password = destData["Password"].ToString(),
        };
    }

    private SftpClient CreateClient()
    {
        return new SftpClient(_sftpSettings.Host, _sftpSettings.Port, _sftpSettings.Username, _sftpSettings.Password);
    }

    private SftpClient CreateDestinationClient()
    {
        return new SftpClient(_destinationSftpSettings.Host, _destinationSftpSettings.Port, _destinationSftpSettings.Username, _destinationSftpSettings.Password);
    }

    public IEnumerable<string> ListFiles(string remotePath)
    {
        using var client = CreateClient();
        client.Connect();

        var files = client.ListDirectory(remotePath)
            .Where(f => !f.Name.StartsWith("."))
            .Select(f => f.Name)
            .ToList();

        client.Disconnect();
        return files;
    }

    public async Task<List<string>> DownloadAllFilesAsync(string remotePath)
    {
        var downloadedFiles = new List<string>();
        var dateFolder = DateTime.UtcNow.ToString("yyyyMMdd");

        using var client = CreateClient();
        client.Connect();

        if (!client.Exists(remotePath))
            throw new DirectoryNotFoundException($"Remote path not found: {remotePath}");

        var files = client.ListDirectory(remotePath)
            .Where(f => !f.IsDirectory && !f.Name.StartsWith("."))
            .ToList();

        string folderToCreate = Path.Combine(_sftpSettings.LocalTargetPath, dateFolder);
        Directory.CreateDirectory(folderToCreate);

        foreach (var file in files)
        {
            string localFilePath = Path.Combine(folderToCreate, file.Name);

            using var localFile = File.OpenWrite(localFilePath);
            await Task.Run(() => client.DownloadFile(file.FullName, localFile));

            downloadedFiles.Add(localFilePath);
        }

        client.Disconnect();
        return downloadedFiles;
    }
}
