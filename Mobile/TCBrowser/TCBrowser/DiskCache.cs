namespace Examples.Mobile
{
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// The disk cache.
    /// </summary>
    public class DiskCache
    {
        private static readonly IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();

        private readonly string path;

        public DiskCache(string path)
        {
            this.path = path;
            if (!storage.DirectoryExists(path))
            {
                storage.CreateDirectory(path);
            }
        }

        public bool Exists(string key)
        {
            var path = this.GetPath(key);

            return storage.FileExists(path);
        }

        public Task<Stream> GetStream(string key)
        {
            var path = this.GetPath(key);
            var stm = storage.OpenFile(path, FileMode.Open, FileAccess.Read);
            return Task.FromResult<Stream>(stm);
        }

        public async Task WriteStream(string key, Stream stm)
        {
            var path = this.GetPath(key);

            try
            {
                using (var fs = storage.OpenFile(path, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    await stm.CopyToAsync(fs, 16384).ConfigureAwait(false);
                }
            }
            catch
            {
            }
        }

        private static string MD5(string input)
        {
            using (var md5Hasher = System.Security.Cryptography.MD5.Create())
            {
                var data = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sBuilder = new StringBuilder();

                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                return sBuilder.ToString();
            }
        }

        private string GetPath(string key)
        {
            return Path.Combine(this.path, MD5(key));
        }
    }
}