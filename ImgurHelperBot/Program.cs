using System;
using RedditSharp;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;

namespace ImgurHelperBot
{
	public enum imgurType { galleryImage, galleryAlbum, album, image };

	public class ImgurConnector
	{
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
				urls.Add(url);
				return urls;
			}

			//Pick how to make the request based on the url, otherwise imgur will give us a 404 error
			webRequests = this.craftRequest(url,curType);
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

		public HttpWebRequest[] craftRequest(string url, imgurType type)
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
			webRequest.Headers.Add("Authorization", "Client-ID b1949cd535a4805");
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

		//TODO rate limit posts to 10 minutes for new account
		//TODO queue up posts that violate rate limit
		//TODO add second thread for posting from queue
		//TODO check for direct links in main
		public static void Main (string[] args)
		{
			ImgurConnector myParser = new ImgurConnector ();

			string[] creds = File.ReadAllLines ("/Users/andrewtackett/Projects/ImgurHelperBotCreds.txt");
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
					List<string> urls;
					Console.WriteLine ("post.url:" + post.Url);
					Console.WriteLine ("url left part: " + post.Url.GetLeftPart (UriPartial.Path));
					urls = myParser.parseUrl (post.Url.GetLeftPart (UriPartial.Path));

					//Display gathered links
					Console.WriteLine ("Links:");
					foreach (string link in urls) {
						Console.WriteLine (link);
					}
					StringBuilder sb = new StringBuilder ();
					sb.Append ("Direct image links:");
					sb.AppendLine ();
					foreach (var link in urls) 
					{
						sb.Append (link);
						sb.AppendLine();
					}
					Console.WriteLine ("sb output:" + sb.ToString ());
					try
					{
						post.Comment (sb.ToString ());
					}catch(RedditSharp.RateLimitException ex) 
					{
						//Could we use delegates here and spawn a timer thread to kick off a post attempt later?
						Console.WriteLine (ex.Data);
						Console.WriteLine ("Rate limit hit!  Trying again in ~9 minutes");
						System.Threading.Thread.Sleep (600000);
						post.Comment (sb.ToString ());
					}
				} else 
				{
					Console.WriteLine ("not an imgur post");
				}
				limit++;
				if (limit > 25)
					break;
			}
		}
	}
}
