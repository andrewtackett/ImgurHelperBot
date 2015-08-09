using System;
using RedditSharp;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ImgurHelperBot
{
	public enum imgurType { galleryImage, galleryAlbum, album, image };

	public class ImgurConnector
	{
		public void getProperties(JObject json){
			foreach (JProperty property in json.Properties()){
				Console.WriteLine(property.Name + " - " + property.Value);
			}
		}

		public List<string> parseUrl(string url){
			HttpWebRequest[] webRequests;
			imgurType curType = this.getType(url);
			List<string> urls = new List<string>();

			//Don't worry about urls that are already direct links
			if (url.Contains (".jpg") || url.Contains (".png") || url.Contains (".gif")) {
				//TODO ignore
			}

			//Pick how to make the request based on the url, otherwise imgur will give us a 404 error
			webRequests = this.craftRequest(url,curType);

			//TODO redo for multiple
			//TODO redo failure for multiple
			foreach (HttpWebRequest webRequest in webRequests) {
				Console.WriteLine ("request: " + webRequest.Address);

				string responseFromServer = this.performRequest (webRequest);

				if (responseFromServer != null) {
					JObject json;
					//Handle case where we queried with the wrong data model (gallery album instead of image)
					if (!responseFromServer.Equals ("404")) {
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

		public imgurType getType(string url){
			//Gallery album type - https://api.imgur.com/models/gallery_album
			//Or may be gallery image type - https://api.imgur.com/models/gallery_image
			if (url.Contains ("gallery")) {
				Console.WriteLine ("gallery");
				return imgurType.galleryAlbum;
			//Album type - https://api.imgur.com/models/album
			} else if(url.Contains("/a/")){
				Console.WriteLine ("album");
				return imgurType.album;
			//Image type - https://api.imgur.com/models/image
			} else {
				Console.WriteLine ("image");
				return imgurType.image;
			}
		}

		public HttpWebRequest[] craftRequest(string url, imgurType type){
			string[] ids = getID (url,type);
			foreach (string id in ids) {
				Console.WriteLine ("id:" + id);
			}
			Console.WriteLine ("url: " + url);
			List<HttpWebRequest> webRequests = new List<HttpWebRequest>();// = (HttpWebRequest)WebRequest.Create("http://google.com");

			foreach (string id in ids) {
				switch (type) {
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

		public string[] getID(string url,imgurType type){
			string idString = url;
			int startIndex = 0;
			char[] delimiters = { '/', ',', '.', '#' };
			string[] tokens = new string[0];
			switch (type) {
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
					if (idString.IndexOf ("#") != -1) {
						length = idString.IndexOf ("#") - startIndex;
					} else {
						length = idString.Length - startIndex;
					}
					idString = idString.Substring (startIndex, length);
					Console.WriteLine ("Substring:" + idString);
					tokens = idString.Split(delimiters);
					break;
			}
			return tokens;
		}

		//Returns string containing response from server
		public string performRequest(HttpWebRequest webRequest){
			webRequest.Headers.Add("Authorization", "Client-ID b1949cd535a4805");
			try{
				Stream response = webRequest.GetResponse().GetResponseStream();
				StreamReader reader = new StreamReader(response);
				string responseFromServer = reader.ReadToEnd();

				reader.Close();
				response.Close();

				return responseFromServer;
			}
			catch (WebException ex){
				if (ex.Status == WebExceptionStatus.ProtocolError){
					var response = ex.Response as HttpWebResponse;
					if (response != null){
						Console.WriteLine("HTTP Status Code: " + (int)response.StatusCode);
						if ((int)response.StatusCode == 404) {
							return "404";
						}
					}
				}

				return null;
			}
		}

		public List<string> getImageLinks(JObject json,imgurType type){
			List<string> links = new List<string> ();
			switch (type) {
				case imgurType.galleryAlbum:
					bool success = bool.Parse (json ["success"].ToString ());
					Console.WriteLine ("success: " + success);
					//galleryAlbum type
					if (success) {
						foreach (JToken token in json["data"]["images"]) {
							links.Add (token ["link"].ToString ());
						}
					//galleryImage type, we'll need to do another request to extract the data
					//TODO redo
					} else {
						links.Add(json["data"]["link"].ToString());
						//links.Add("error");
						//links.Add("galleryImage");
					}
					break;
				case imgurType.galleryImage:
					links.Add(json["data"]["link"].ToString());
					break;
				case imgurType.album:
					foreach (JToken token in json["data"]["images"]) {
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

		//TODO: add code to handle retrying with gallery image if not gallery album
		public static void Main (string[] args)
		{
			ImgurConnector myParser = new ImgurConnector ();
			List<string> urls;

			//TODO: handle unusual urls: 
			/*entry point	content
			imgur.com/{image_id}	image
			imgur.com/{image_id}.extension	direct link to image (no html)
			imgur.com/a/{album_id}	album
			imgur.com/a/{album_id}#{image_id}	single image from an album
			imgur.com/gallery/{gallery_post_id}	gallery
			*/
			//string url = "https://imgur.com/a/QMmR2#0";

			//string url = "http://imgur.com/8UtzD83,6aG8utZ,RHymzpg,kmh2AUL,MOwVxNw,wbFYpJ1#5";


			//string url = "http://imgur.com/gallery/KmB7kFV/new?forcedesktop=1";


			//image
			string url = "http://imgur.com/cQH87QL";

			//nonworking
			//string url = "http://imgur.com/gallery/0ENTp";

			//gallery image 
			//string url = "http://imgur.com/gallery/A6DVKa3";

			//gallery album 
			//string url = "http://imgur.com/gallery/UdaKS";

			//album - working
			//string url = "http://imgur.com/a/G3oj0";

			urls = myParser.parseUrl(url);

			//Display gathered links
			Console.WriteLine ("Links:");
			foreach (string link in urls) {
				Console.WriteLine (link);
			}
		}
	}
}
