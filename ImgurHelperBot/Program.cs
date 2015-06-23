using System;
using RedditSharp;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ImgurHelperBot
{
	enum imgurType { galleryAlbum, album, image };

	class MainClass
	{
		public void getProperties(JObject json){
			foreach (JProperty property in json.Properties()){
				Console.WriteLine(property.Name + " - " + property.Value);
			}
		}

		public imgurType getType(string url){
			//Gallery album type - https://api.imgur.com/models/gallery_album
			if (url.Contains ("gallery")) {
				Console.WriteLine ("gallery");
				return imgurType.image;
			//Album type - https://api.imgur.com/models/album
			} else if(url.Contains("/a/")){
				Console.WriteLine ("album");
				return imgurType.galleryAlbum;
			//Image type - https://api.imgur.com/models/image
			} else {
				Console.WriteLine ("image");
				return imgurType.image;
			}
		}

		public HttpWebRequest craftRequest(string url, imgurType type){
			string id = url.Substring(url.LastIndexOf("/") + 1);

			Console.WriteLine ("id: " + id + ", url: " + url);
			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("http://google.com");

			switch (type) {
				case imgurType.galleryAlbum:
					webRequest = (HttpWebRequest)WebRequest.Create("https://api.imgur.com/3/gallery/album/" + id);
					break;
				case imgurType.album:
					webRequest = (HttpWebRequest)WebRequest.Create("https://api.imgur.com/3/album/" + id);
					break;
				case imgurType.image:
					webRequest = (HttpWebRequest)WebRequest.Create("https://api.imgur.com/3/image/" + id);
					break;
			}

			return webRequest;
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
					}
				}

				return null;
			}
		}

		public string[] getImageLinks(JObject json,imgurType type){
			string[] links = {"test"};
			switch (type) {
			case imgurType.galleryAlbum:

				break;
			case imgurType.album:

				break;
			case imgurType.image:
				links[0] = (string)json["data"]["link"];
				Console.WriteLine ("Image Link: " + links[0]);
				break;
			default:
				//shouldn't get here
				break;
			}
			return links;
		}

		public static void Main (string[] args)
		{
			MainClass myParser = new MainClass ();
			Console.WriteLine ("Hello World!");
			//string url = "http://imgur.com/cQH87QL";

			//nonworking
			//string url = "http://imgur.com/gallery/0ENTp";
			string url = "http://imgur.com/gallery/A6DVKa3";
			//string url = "http://imgur.com/a/G3oj0";
			HttpWebRequest webRequest;
			imgurType curType = myParser.getType(url);
			string[] urls;

			//Don't worry about urls that are already direct links
			if (url.Contains (".jpg") || url.Contains (".png") || url.Contains (".gif")) {
				//ignore
			}

			//Pick how to make the request based on the url, otherwise imgur will give us a 404 error
			webRequest = myParser.craftRequest(url,curType);

			Console.WriteLine ("request: " + webRequest.Address);

			string responseFromServer = myParser.performRequest (webRequest);

			if (responseFromServer != null) {
				JObject json = JObject.Parse (responseFromServer);

				urls = myParser.getImageLinks (json, curType);

				Console.WriteLine (responseFromServer);

				//Display gathered links
				Console.WriteLine ("Links:");
				foreach (string link in urls) {
					Console.WriteLine (link);
				}
			} else {
				//web request failed
			}
		}
	}
}
