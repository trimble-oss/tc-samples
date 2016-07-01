namespace Examples.Mobile
{
    using System;
    using System.Threading.Tasks;
    using System.IO;
    using System.Net.Http;
    ////using Android.Graphics;
    ////using Android.Graphics.Drawables;

    using Trimble.Connect.Client;

    /// <summary>
    /// The thumbnails sources.
    /// </summary>
    public class ThumbnailImageSource
    {
        private static readonly DiskCache Cache = new DiskCache("ThumbnailImageCache");

        private IProjectClient client;

        public bool CachingEnabled { get; set; }

        public TimeSpan CacheValidity { get; set; }

        public ThumbnailImageSource(IProjectClient client)
        {
            this.client = client;
            this.CachingEnabled = false;
            this.CacheValidity = TimeSpan.FromDays(1);
        }

        ////public async Task<Drawable> GetDrawble(string url)
        ////{
        ////    var stm = await this.GetStream(url);
        ////    return new BitmapDrawable(stm);
        ////}

        ////public async Task<Bitmap> GetBitmap(string url)
        ////{
        ////    var stm = await this.GetStream(url);
        ////    return await BitmapFactory.DecodeStreamAsync(stm);
        ////}

        public async Task<Stream> GetStream(string url)
        {
            try
            {
                Stream stm = null;

                if (this.CachingEnabled)
                {
                    stm = await this.GetStreamFromCache(url).ConfigureAwait(false);
                }

                if (stm == null || stm.Length == 0)
                {
                    stm = await this.GetStreamFromWeb(url).ConfigureAwait(false);

                    if (this.CachingEnabled && stm != null && stm.Length > 0)
                    {
                        await this.SaveToCache(url, stm);
                        stm = await this.GetStreamFromCache(url).ConfigureAwait(false);
                    }
                }

                return stm;
            }
            catch
            {
                return null;
            }
        }

        private async Task<Stream> GetStreamFromWeb(string url)
        {
            var thumbnail = await client.DownloadThumbnailAsync(url).ConfigureAwait(false);
            return thumbnail.Item1;
        }

        private Task<Stream> GetStreamFromCache(string url)
        {
            return Cache.GetStream(url);
        }

        private async Task SaveToCache(string url, Stream stm)
        {
            await Cache.WriteStream(url, stm);
        }
    }
}