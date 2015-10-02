using System;
using RedditSharp;
using RedditSharp.Things;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

namespace ImgurHelperBot
{
    public class PostWriter
    {
        private BlockingCollection<PostData> buffer;

        public PostWriter(BlockingCollection<PostData> linkBuffer)
        {
            buffer = linkBuffer;
        }

        public void writePosts()
        {
            while(!buffer.IsAddingCompleted || buffer.Count > 0)
            {
                PostData pd;
                try
                {
                    pd = buffer.Take();
                    try
                    {
                        pd.parentPost.Comment(pd.comment);
                        Console.WriteLine("Wrote: " + pd.comment + ", in post: " + pd.parentPost.Permalink); 
                    }
                    catch (RedditSharp.RateLimitException ex)
                    {
                        Console.WriteLine(ex.Data);
                        Console.WriteLine("Rate limit hit!  We'll try again later");
                        //Since reddit sharp naturally limits us to the normal minimum assume we've hit the new account rate limit of 9 minutes and wait ~10 to make sure we've cleared it
                        System.Threading.Thread.Sleep(600000);
                        buffer.Add(pd);
                    }
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("Ran out of things to post! Shutting down");
                }
            }
        }
    }

    public class PostData
    {
        public string comment;
        public Post parentPost;

        public PostData(string comment, Post parentPost)
        {
            this.comment = comment;
            this.parentPost = parentPost;
        }
    }

	public enum imgurType { galleryImage, galleryAlbum, album, image };

	public class ImgurConnector
	{
        private string imgurClientID;

        public ImgurConnector(string clientID)
        {
            imgurClientID = clientID;
        }

		public void getProperties(JObject json)
		{
			foreach (JProperty property in json.Properties())
			{
				Console.WriteLine(property.Name + " - " + property.Value);
			}
		}

		public List<string> parseUrl(string url)
		{
			HttpWebRequest[] webRequests;
			imgurType curType = this.getType(url);
			List<string> urls = new List<string>();
			Console.WriteLine ("original url: " + url);

			//Don't worry about urls that are already direct links
			if (url.Contains (".jpg") || url.Contains (".png") || url.Contains (".gif")) 
			{
				return urls;
			}

			//Pick how to make the request based on the url, otherwise imgur will give us a 404 error
			webRequests = this.craftRequests(url,curType);
			foreach (HttpWebRequest webRequest in webRequests) 
			{
				Console.WriteLine ("request: " + webRequest.Address);

				string responseFromServer = this.performRequest (webRequest);

				if (responseFromServer != null) 
				{
					JObject json;
					//Handle case where we queried with the wrong data model (gallery album instead of image)
					if (!responseFromServer.Equals ("404")) 
					{
						json = JObject.Parse (responseFromServer);

						urls.AddRange(this.getImageLinks (json, curType));
					} else {
						curType = imgurType.galleryImage;
					}
					Console.WriteLine ("response:" + responseFromServer);
				} else {
					//web request failed
					Console.WriteLine ("Query failed 3");
					//log output for examination
					Console.WriteLine ("url: " + url);
				}
			}

			return urls;
		}

		public imgurType getType(string url)
		{
			//Gallery album type - https://api.imgur.com/models/gallery_album
			//Or may be gallery image type - https://api.imgur.com/models/gallery_image
			if (url.Contains ("gallery")) 
			{
				Console.WriteLine ("gallery");
				return imgurType.galleryAlbum;
			//Album type - https://api.imgur.com/models/album
			} else if(url.Contains("/a/"))
			{
				Console.WriteLine ("album");
				return imgurType.album;
			//Image type - https://api.imgur.com/models/image
			} else 
			{
				Console.WriteLine ("image");
				return imgurType.image;
			}
		}

		public HttpWebRequest[] craftRequests(string url, imgurType type)
		{
			string[] ids = getID (url,type);
			foreach (string id in ids) 
			{
				Console.WriteLine ("id:" + id);
			}
			Console.WriteLine ("url: " + url);
			List<HttpWebRequest> webRequests = new List<HttpWebRequest>();// = (HttpWebRequest)WebRequest.Create("http://google.com");

			foreach (string id in ids) 
			{
				switch (type) 
				{
					case imgurType.galleryAlbum:
						webRequests.Add ((HttpWebRequest)WebRequest.Create ("https://api.imgur.com/3/gallery/album/" + id));
						//Add in gallery image in case this isn't an album
						webRequests.Add ((HttpWebRequest)WebRequest.Create ("https://api.imgur.com/3/gallery/image/" + id));
						break;
					case imgurType.galleryImage:
						webRequests.Add((HttpWebRequest)WebRequest.Create ("https://api.imgur.com/3/gallery/image/" + id));
						break;
					case imgurType.album:
						webRequests.Add((HttpWebRequest)WebRequest.Create ("https://api.imgur.com/3/album/" + id));
						break;
					case imgurType.image:
						webRequests.Add((HttpWebRequest)WebRequest.Create ("https://api.imgur.com/3/image/" + id));
						break;
					default:
						webRequests.Add((HttpWebRequest)WebRequest.Create ("https://api.imgur.com/3/image/" + id));
						break;
				}
			}

			return webRequests.ToArray();
		}

		public string[] getID(string url,imgurType type)
		{
			string idString = url;
			int startIndex = 0;
			char[] delimiters = { '/', ',', '.', '#' };
			string[] tokens = new string[0];
            if (url.Equals(""))
                return tokens;
			switch (type) 
			{
				case imgurType.album:
					startIndex = idString.IndexOf ("/a/") + 3;
					idString = idString.Substring (startIndex);
					tokens = idString.Split (delimiters, idString.Length);
					Array.Resize (ref tokens, 1);
					break;
				case imgurType.galleryAlbum:
				case imgurType.galleryImage:
					startIndex = idString.IndexOf("gallery/") + 8;
					idString = idString.Substring (startIndex);
					tokens = idString.Split (delimiters, idString.Length);
					Array.Resize (ref tokens, 1);
					break;
				case imgurType.image:
					startIndex = idString.IndexOf (".com/") + 5;
					int length;
						//Trim off the image selector if present
					if (idString.IndexOf ("#") != -1)
						length = idString.IndexOf ("#") - startIndex;
					else
						length = idString.Length - startIndex;

					idString = idString.Substring (startIndex, length);
					Console.WriteLine ("Substring:" + idString);
					tokens = idString.Split(delimiters);
					break;
			}
			return tokens;
		}

		//Returns string containing response from server
		public string performRequest(HttpWebRequest webRequest)
		{
			webRequest.Headers.Add("Authorization", "Client-ID " + imgurClientID);
			try
			{
				Stream response = webRequest.GetResponse().GetResponseStream();
				StreamReader reader = new StreamReader(response);
				string responseFromServer = reader.ReadToEnd();

				reader.Close();
				response.Close();

				return responseFromServer;
			}
			catch (WebException ex)
			{
				if (ex.Status == WebExceptionStatus.ProtocolError)
				{
					var response = ex.Response as HttpWebResponse;
					if (response != null)
					{
						Console.WriteLine("HTTP Status Code: " + (int)response.StatusCode);
						if ((int)response.StatusCode == 404)
							return "404";
					}
				}

				return null;
			}
		}

		public List<string> getImageLinks(JObject json,imgurType type)
		{
			List<string> links = new List<string> ();
			switch (type) 
			{
				case imgurType.galleryAlbum:
					bool success = bool.Parse (json ["success"].ToString ());
					Console.WriteLine ("success: " + success);
					//galleryAlbum type
					if (success) 
					{
						foreach (JToken token in json["data"]["images"]) 
						{
							links.Add (token ["link"].ToString ());
						}
					//galleryImage type, try parsing 
					} else 
					{
						links.Add(json["data"]["link"].ToString());
					}
					break;
				case imgurType.galleryImage:
					links.Add(json["data"]["link"].ToString());
					break;
				case imgurType.album:
					foreach (JToken token in json["data"]["images"]) 
					{
						links.Add (token ["link"].ToString ());
					}
					break;
				case imgurType.image:
					links.Add((string)json["data"]["link"]);
					Console.WriteLine ("Image Link: " + links[0]);
					break;
				default:
					//shouldn't get here
					links.Add ("test");
					break;
			}
			return links;
		}

        public string buildComment(List<string> urls)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Direct image links:");
            sb.AppendLine();
            foreach (var link in urls)
            {
                sb.Append(link);
                sb.AppendLine();
            }
            Console.WriteLine("sb output:" + sb.ToString());

            return sb.ToString();
        }

        public static string[] getCredentials()
        {
            string[] creds;
            try
            {
                creds = File.ReadAllLines("ImgurHelperBotCreds.txt");

                if (creds.Length != 3)
                {
                    Console.WriteLine("Incorrect number of lines in ImgurHelperBotCreds.txt");
                    Console.WriteLine("Number of lines: " + creds.Length);
                    System.Environment.Exit(0);
                }

                return creds;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Couldn't find ImgurHelperBotCreds.txt in the same directory as executable");
                System.Environment.Exit(0);
            }

            //Won't reach here, just exists to make the compiler happy
            return new string[0];
        }

		//TODO rate limit posts to 10 minutes for new account
		//TODO add cancellation/stop feature to shut down gracefully (need to do in consumer thread too)
        //TODO check to see if getID can be simplified since we now use GetLeftPart
		public static void Main (string[] args)
		{
            string[] creds = getCredentials();
            BlockingCollection<PostData> linksBuffer = new BlockingCollection<PostData>(10000);
            ImgurConnector myParser = new ImgurConnector(creds[2]);
            PostWriter pw = new PostWriter(linksBuffer);
            Thread consumerThread = new Thread(new ThreadStart(pw.writePosts));
            consumerThread.Start();

			var reddit = new Reddit();
			var user = reddit.LogIn(creds[0], creds[1]);
			var subreddit = reddit.GetSubreddit("/r/test");
			int limit = 0;
			foreach (var post in subreddit.New)
			{
				Console.WriteLine ("=====================================================");
				Console.WriteLine ("author: " + post.Author);
				Console.WriteLine ("title: " + post.Title);
				//Only look at urls from imgur
				if (post.Url.Host.Contains ("imgur")) {
					bool alreadyCommented = false;
					foreach (var comment in post.Comments) {
						//If we've already commented on this thread ignore it
						if (comment.Author == "imgurhelperbot") {
							Console.WriteLine ("Already commented on this post.  Comment:");
							Console.WriteLine (comment.Body);
							alreadyCommented = true;
							break;
						}
					}
					if (alreadyCommented)
						continue;

					//Console.WriteLine ("post.url:" + post.Url);
					//Console.WriteLine ("url left part: " + post.Url.GetLeftPart (UriPartial.Path));
					List<string> urls = myParser.parseUrl (post.Url.GetLeftPart (UriPartial.Path));

                    //Direct image link so we should skip it
                    if (urls.Count==0)
                    {
                        Console.WriteLine("Direct image link already: " + post.Url + ", skipping");
                        continue;
                    }

					//Display gathered links
					Console.WriteLine ("Links:");
					foreach (string link in urls) {
						Console.WriteLine (link);
					}

                    string outputComment = myParser.buildComment(urls);
                    PostData pd = new PostData(outputComment,post);
                    linksBuffer.Add(pd);
				} else 
				{
					Console.WriteLine ("not an imgur post");
				}
				limit++;
				if (limit > 25)
					break;
			}

            linksBuffer.CompleteAdding();
            consumerThread.Join();
            Console.WriteLine("Please press enter to exit...");
            Console.Read();
		}
	}
}
