// using System;
// using System.Collections.Generic;
// using Flow.Launcher.Plugin;
// using System.Linq;
// using System.Diagnostics;
// using Microsoft.Graph;
// using System.Net.Http;
// using System.Threading;
// using System.Threading.Tasks;
// using System.Net.Http.Headers;
////Note the spotify plugin use async stuff if what to use microsoft graph
//https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/
//https://github.com/taooceros/Flow.LibreTranslate/blob/main/Flow.LibreTranslate/Main.cs
// namespace Flow.Launcher.Plugin.OneNote
// {
//     public class OneNoteAsync : IAsyncPlugin
//     {

//         private PluginInitContext context;
//         private readonly string iconPath = "Images/logo.png";
//         private readonly string accessToken = "EwB4A8l6BAAUkj1NuJYtTVha+Mogk+HEiPbQo04AAch223OvaQFVqa9KOoKEucVfKwL7EHzOcdpR0UdonSch42UNo/xzbui6qd6H3mCljS/bypHwOGcZ3uBJ4z5QjpgJ3GDFxmxTgBVt3Bv6koH2N4PfRr3uuaAjTKeWJg2XaV3/Ss0TGzMEC/bM/V2BCMz1SI+XDQ57AMpanWMpok62GTtiSCYRfJRbSoe8LI8fK88KCet6GzDFXkgKX60XZ4XVWukTO/gl7c3li1SBm8kK3FeZOeN5sndVHTK9iNco/AWNtxmR2JMe4LC+d7STlE/IbXcFrThNLz/q+C6ms3LE3ILM0ebFlqZP7VuEYwtVDgQywwWIkHmtcc5mMHIZYloDZgAACEV70WYINiflSAJMROMWKyGnXOJ4t4vUh6o9qHc9vM6qVtVsC97mGWydK5MnTk/6QIc3qum06c4pJLrOLCO/lxqB6f9JcwyVAJ9r8GpxbTFLJMDQHLi4CIoByn0oDpmlkpTLQB7Md5Nx5mjIZok9xQRDP+Pcn8ljqQjF+F6zqG1aubWOvX8OHFbSyX7tawTtjbTw+jOcdIR+b+cG3LU8BtbJL53u7I6Hk9G4i/x2lyevbOte4lLKxWbs38PUL634SF6i3NUxxGN6QAtFGWmHDD4PI6L1MXddU3VRHtZJatBOAbIb8GmVO6EvEgaT5Y/0jgSPogNr2t3DXvm61+NWtNMu6Dr9+pssJ1f6aJ19Lchx50V7JZuGNlyF7PaamfD4IUEf9b0PTWvQK+w7nCniCgHaw5HGvoLf4qqwXIgDIiuwFIYgaNCVh1vE9PnL/mieZoYl6aXCtlJ2TdD7LPwLEX9YJyKTuxztVmXLKBgRhBRd0GCu8DohmXfBgSB5lrQOh7B4a75uB06mbBck8bBioI/D1/GcILUP7l2jxYuPko9/LY4DQvYYPAcqlpP3AxNtzFhDTdpGVHI5y8KtRxK4bgFdGD35boGZWTYVRUil07Lpd3nkApXk6YZ9gsD/vQbXLB/fdMczog86TAo1Qcmqlb3E6FqVhhuCjR/Ste1WP5M5SiIyHNoBfGZbszaTnuj7h9+oJnVq12vVGP1+eYekNTspM4ijXuSxRik/c5GQ/nwO7f5tKDi0pmg1VBbYZ2RjDRBTD6mDcE5f3q6yApIYEeU6aooC";
//         private GraphServiceClient graphClient;
//         private IOnenotePagesCollectionPage pages;

//         public Task InitAsync(PluginInitContext context)
//         {
//             this.context = context;
//             graphClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
//             {
//                 requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
//                 return Task.CompletedTask;
//             }));

//             var queryOptions = new List<QueryOption>()
//             {
//                 new QueryOption("select", "links,title"),
//                 //new QueryOption("search", "odin")
//             };
//             // option one cache all pages and search titles.
//             //option two use async search...
//             return Task.CompletedTask;
//             //pages = await graphClient.Me.Onenote.Pages.Request(queryOptions).GetAsync();
//         }
//         public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
//         {
//             if (token.IsCancellationRequested)
//                 return null;

//             if (string.IsNullOrWhiteSpace(query.Search))
//             {
//                 return new List<Result>() { new Result { Title = "Search OneNote" } };
//             }


//             try
//             {
//                 var queryOptions = new List<QueryOption>()
//                 {
//                     new QueryOption("select", "links,title"),
//                     new QueryOption("search", query.Search)
//                 };
//                 var pages = await graphClient.Me.Onenote.Pages.Request(queryOptions).GetAsync(token);
//                 return pages.Select(page => new Result
//                 {
//                     Title = page.Title,
//                     SubTitle = page.ParentSection.DisplayName,
//                     IcoPath = iconPath
//                 }).ToList();
//             }
//             catch (Exception e)
//             {
//                 context.API.LogException("OneNote", "Omegalol", e);
//                 return new List<Result>() { new Result { Title = "Oof error baby" } }; ;
//             }
//         }
//         #region  OLD
//         //     public void Init(PluginInitContext context)
//         //     {
//         //         this.context = context;
//         //         // graphClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) => {
//         //         //     requestMessage
//         //         //         .Headers
//         //         //         .Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

//         //         //     return Task.CompletedTask;
//         //         // }));
//         //         // //GraphServiceClient graphClient = new GraphServiceClient(authProvider);

//         //         // var queryOptions = new List<QueryOption>()
//         //         // {
//         //         //     new QueryOption("select", "links,title"),
//         //         //     new QueryOption("search", "odin")
//         //         // };

//         //         // var pages = await graphClient.Me.Onenote.Pages
//         //         //     .Request( queryOptions )
//         //         //     .Search("odin")
//         //         //     .GetAsync();

//         //         // var client = new HttpClient();
//         //         // var request = new HttpRequestMessage
//         //         // {
//         //         //     Method = HttpMethod.Get,
//         //         //     RequestUri = new Uri("https://graph.microsoft.com/v1.0/me/onenote/pages?select=title%2Clinks&search=odin"),
//         //         //     Headers =
//         //         //     {
//         //         //         { "Authorization", "Bearer EwB4A8l6BAAUkj1NuJYtTVha+Mogk+HEiPbQo04AAZZzTHgu7x9c5dTd90UJWS7/GZOMu5tnGZ6cDoTm/BuHOQnWoU109xOs9DvO++Ae2tSdK7ok5KeMyv7tW1apT2ijweSbGvN92glj+vUCsnt0lDueXvup4d9RdSgw9Zh6KH7e56FvK0MrlCNqQMMjizzYikk9OTgcGbkWK4Yd+3M+I2WY5bRtXiuLXukwHV8Mb2reQ/a6X47dW0PvbcvsShidLOpobyLnSb4DVs3srla7RyICrNqQYk1ybw1R0QIGo0EXy+od/AhiVBH4Rw8LdrqQ/G6yfTjSJixWERy5MVf24vxGExCb6yUhaK77jec9s9zuB+VtcRlbPtJbB1lHjYoDZgAACPFlTrt0HEfRSAIO+2rO5CrErAHIuhwWBxuuAhihMVQLXL2RTjtY60v80uKT/159ay5oVu5Lg5tgSI3rbYCb3BuE9T/Z5l80JhB2rxCrUR2D82XFrPkcrr9MlD05kOtmRVCdlCDzc1w5UjxNHz6vFmgvUGBuHVsRcZO58XpIuAMi1Fu8efqsW7cHFvALSuVupUPcMzrfU2TDxqGUCiI5yrEGGsPL25Gn6iEzzUYYQZupAIuoZYVIR/EwbGGnkilRA3z7O7KZ4Q+jFOoHGv1zQmItHiR2cqpMFWZcBH6Rlz7t/PQv6LasqxoiwZmGTd6A/rLQPf1dOqBNtbAzRSwa9aHh62ntoVdANyI24OYaq2P/v5bvRWymh0QqHC8/2dcitvbnT5wrKXZkfSpLV+GY4Rni6IkeqnD8aapBcTk+9zKKt9ckrjkDKnepmvhAi++rnDl/mdt6+1DQfZ5/bdaABVwjNhavh5wHn6Bgke/lGifJEx2hRfSoKWl887B5rPs3A0jcUaOk2EKf/atO1D326gVdha9cf3MS7QRf9atWXAAPGrR4/xupr48i1XjRQ22rLSlnRB/kGN9X8XlGcbf3LqHjE45sjPy2k0kHRiB7oixzAomSkqqzqk9kWe0yBITuSYahhZdysvZNqs8UYR/fGMiNEiWblJIj5SIHRHAxJCxLR43b5+L01+DTKzEG8L5SUO/vuMcyg1a/Az6kj8Cd8vuPAovwe1h+Xf3F8JOGuyI+h8MihlNzM0o0nevErxEzNuPo6xHuUuYt23RvSMHZD7MkbooC" },
//         //         //         { "Accept", "application/json" },
//         //         //         { "Cache-Control", "no-cache" },
//         //         //     },

//         //         // };
//         //         // using (var response = await client.SendAsync(request))
//         //         // {
//         //         //     response.EnsureSuccessStatusCode();
//         //         //     var body = await response.Content.ReadAsStringAsync();
//         //         //     Console.WriteLine(body);
//         //         // }            
//         //     }

//         //     public List<Result> Query(Query query)
//         //     {
//         //         var result = new Result
//         //         {
//         //             Title = "Search OneNote notes",
//         //             SubTitle = $"Query: {query.Search}",
//         //             Action = c =>
//         //             {
//         //                 context.API.ShowMsg("Title", "Subtitle");
//         //                 return true;
//         //             },
//         //             IcoPath = "Images/oneNoteLogo.png"
//         //         };

//         //         var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) => {
//         //             requestMessage
//         //                 .Headers
//         //                 .Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

//         //             return Task.CompletedTask;
//         //         }));

//         //         var queryOptions = new List<QueryOption>()
//         //         {
//         //             new QueryOption("select", "links,title"),
//         //             new QueryOption("search", "odin")
//         //         };

//         //         var pages = graphClient.Me.Onenote.Pages
//         //             .Request(queryOptions)
//         //             .GetAsync();
//         //         pages.Wait();
//         //         var result2 = new Result
//         //         {
//         //             Title = pages.Result[0].Title,
//         //             SubTitle = "LOL",
//         //         };
//         //         return new List<Result>() {result};
//         //     }

//         //     // public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
//         //     // {
//         //     //                 var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) => {
//         //     //         requestMessage
//         //     //             .Headers
//         //     //             .Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

//         //     //         return Task.CompletedTask;
//         //     //     }));
//         //     //     //GraphServiceClient graphClient = new GraphServiceClient(authProvider);

//         //     //     var queryOptions = new List<QueryOption>()
//         //     //     {
//         //     //         new QueryOption("select", "links,title"),
//         //     //         new QueryOption("search", "odin")
//         //     //     };

//         //     //     var pages = await graphClient.Me.Onenote.Pages
//         //     //         .Request(queryOptions)
//         //     //         .GetAsync();

//         //     //     Console.WriteLine(pages[0].Title);
//         //     // }
//         #endregion
//     }
// }