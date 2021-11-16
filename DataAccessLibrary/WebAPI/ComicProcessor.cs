using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace DataAccessLibrary.WebAPI
{
    public class ComicProcessor
    {
        public async Task<ComicViewModel> LoadComic(int comicNumber = 0)
        {
            string url = $"https://xkcd.com/info.0.json";

            using (HttpResponseMessage response = await ApiManager.ApiClient.GetAsync(url))
            {
                if(response.IsSuccessStatusCode)
                {
                    ComicViewModel comic = await response.Content.ReadAsAsync<ComicViewModel>();

                    return comic;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }
    }
}
